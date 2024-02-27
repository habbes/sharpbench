using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;

namespace Sharpbench;

class JobRunner
{
    ConcurrentBag<Job> queue = new();
    JobsTracker tracker;
    ConcurrentDictionary<WebSocket, WebSocket> realTimeClients = new();
    public JobRunner(JobsTracker tracker)
    {
        tracker.OnNewJob(this.HandleOnNewJob);
        this.tracker = tracker;
    }

    public void HandleOnNewJob(Job job)
    {
        Console.WriteLine($"Queued job {job.Id}");
        queue.Add(job);
    }

    public void RunJobs()
    {
        Console.WriteLine("Running jobs...");
        Task.Run(() => RunBackgroundJobs()); // TODO: proper background job handling
    }

    public Task RealTimeSyncWithClient(WebSocket client)
    {
        realTimeClients.TryAdd(client, client);
        while (client.State == WebSocketState.Open)
        {
            Thread.Sleep(1000);
        }

        realTimeClients.Remove(client, out _);
        return Task.CompletedTask;
    }

    private async Task BroadcastLogMessage(LogMessage message)
    {
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, message);
        stream.Position = 0;
        byte[] bytes = stream.ToArray();
        var data = new ArraySegment<byte>(bytes, 0, bytes.Length);
        // TODO: broadcast for simplicity, but should send messages to the right clients
        await BroadCastMessage(data);
    }

    private async Task BroadCastMessage(ArraySegment<byte> message)
    {
        foreach (var kvp in realTimeClients)
        {
            var client = kvp.Value;
            await client.SendAsync(message, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    private async Task RunBackgroundJobs()
    {
        while (true)
        {
            if (queue.IsEmpty)
            {
                continue;
            }

            if (queue.TryTake(out Job? job))
            {
                try {
                    await RunJob(job);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error occurred while processing job {job}: {e.Message}");
                }
            }
        }
    }

    private async Task RunJob(Job job)
    {
        this.tracker.ReportJobStarted(job.Id);
        

        var tempFolderName = Path.GetRandomFileName();;
        var tempProjectDir = Path.Combine(Path.GetTempPath(), tempFolderName);
        Directory.CreateDirectory(tempProjectDir);

        // copy files from the project template to the temp project dir
        var cwd = new DirectoryInfo(Directory.GetCurrentDirectory());
        Console.WriteLine($"cwd: {cwd}");
        var projectTemplateDir = Path.Combine(Directory.GetCurrentDirectory(), "project-template");
        foreach (var file in Directory.GetFiles(projectTemplateDir))
        {
            Console.WriteLine($"file {file}");
            var destFile = Path.Combine(tempProjectDir, Path.GetFileName(file));
            Console.WriteLine($"Copying '{file} to '{destFile}");
            File.Copy(file, destFile, overwrite: true);
        }

        var userBenchmarkClass = Path.Combine(tempProjectDir, "Benhmarks.cs");
        Console.WriteLine($"Writing user code in {userBenchmarkClass}");
        File.WriteAllText(userBenchmarkClass, job.Code);


        Console.WriteLine($"Running job {job.Id}");

        
        int exitCode = await ExecuteBenchmarkProject(job.Id, tempProjectDir);
        Console.WriteLine("Execution complete");
        // ensure project and result files were generated correctly
        foreach (var entry in Directory.GetFileSystemEntries(tempProjectDir))
        {
            Console.WriteLine($"user project file: { entry }");
        }

        Console.WriteLine($"Deleting folder '{ tempProjectDir }'");
        Directory.Delete(tempProjectDir, recursive: true);
        Console.WriteLine($"Directory deleted? {!Directory.Exists(tempProjectDir)}");
        Console.WriteLine($"Exit code {exitCode}");
    }

    private async Task<int> ExecuteBenchmarkProject(int jobId, string projectDir)
    {
        Console.WriteLine("Starting benchmark run");
        Console.WriteLine("Restoring project...");
        int restoreExitCode = await RunRestore(jobId, projectDir);
        if (restoreExitCode != 0)
        {
            Console.WriteLine("Restore failed");
            return restoreExitCode;
        }

        Console.WriteLine("Running project...");
        int runExitCode = await RunBuildAndRun(jobId, projectDir);
        if (runExitCode != 0)
        {
            Console.WriteLine("Run failed");
        }

        return runExitCode;
    }

    private Task<int> RunRestore(int jobId, string projectDir)
    {
        // TODO instead of ignoring failed sources, we could specify a Nuget config
        // file that specifies the sources to target
        return RunDotnetStep(jobId, projectDir, "restore -s https://api.nuget.org/v3/index.json --ignore-failed-sources");
    }

    private Task<int> RunBuildAndRun(int jobId, string projectDir)
    {
        // We should run this after restore has completed;
        // TODO: I set inProcess option as hack to avoid building the benchmarked code separately
        // cause that leads to Nuget restore issues on my machine because of the credentials issue
        // I should disable this once I containerize these background jobs
        return RunDotnetStep(jobId, projectDir, "run -c Release --no-restore -- --filter=* --inProcess");
    }

    private async Task<int> RunDotnetStep(int jobId, string projectDir, string args)
    {
        try 
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = projectDir,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = args,
                
            };
            
            Console.WriteLine("Started process");
            // TODO should stream the outputs to the client instead
            // Console.WriteLine("stdout: {0}", await process.StandardOutput.ReadToEndAsync());
            // Console.WriteLine("stderr: {0}", await process.StandardError.ReadToEndAsync());

            process.OutputDataReceived += async void (sender, args) => 
            {
                Console.ResetColor();
                Console.WriteLine(args.Data);
                if (args.Data == null)
                {
                    return;
                }

                await BroadcastLogMessage(new LogMessage(jobId, "stdout", args.Data));
            };

            process.ErrorDataReceived += async void (sender, args) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(args.Data);
                if (args.Data == null)
                {
                    return;
                }

                await BroadcastLogMessage(new LogMessage(jobId, "stderr", args.Data));
            };

            bool started = process.Start();
            if (!started)
            {
                throw new Exception("Failed to start process");
            }

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            await process.WaitForExitAsync();
            int exitCode = process.ExitCode;
            return exitCode;
        }
        finally
        {
            Console.ResetColor();
        }
    }
}

record LogMessage(int JobId, string LogSource, string Message);
