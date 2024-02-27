using System.Collections.Concurrent;
using System.Diagnostics;

namespace Sharpbench;

class JobRunner
{
    ConcurrentBag<Job> queue = new();
    JobsTracker tracker;
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

        
        int exitCode = await ExecuteBenchmarkProject(tempProjectDir);
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

    private async Task<int> ExecuteBenchmarkProject(string projectDir)
    {
        Console.WriteLine("Starting benchmark run");
        Console.WriteLine("Restoring project...");
        int restoreExitCode = await RunRestore(projectDir);
        if (restoreExitCode != 0)
        {
            Console.WriteLine("Restore failed");
            return restoreExitCode;
        }

        Console.WriteLine("Running project...");
        int runExitCode = await RunBuildAndRun(projectDir);
        if (runExitCode != 0)
        {
            Console.WriteLine("Run failed");
        }

        return runExitCode;
    }

    private Task<int> RunRestore(string projectDir)
    {
        // TODO instead of ignoring failed sources, we could specify a Nuget config
        // file that specifies the sources to target
        return RunDotnetStep(projectDir, "restore -s https://api.nuget.org/v3/index.json --ignore-failed-sources");
    }

    private Task<int> RunBuildAndRun(string projectDir)
    {
        // We should run this after restore has completed;
        // TODO: I set inProcess option as hack to avoid building the benchmarked code separately
        // cause that leads to Nuget restore issues on my machine because of the credentials issue
        // I should disable this once I containerize these background jobs
        return RunDotnetStep(projectDir, "run -c Release --no-restore -- --filter=* --inProcess");
    }

    private async Task<int> RunDotnetStep(string projectDir, string args)
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

            process.OutputDataReceived += (sender, args) => 
            {
                Console.ResetColor();
                Console.WriteLine(args.Data);
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(args.Data);
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

