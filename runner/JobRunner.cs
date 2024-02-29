using Sharpbench.Core;
using System.Diagnostics;
using System.Text.Json;

namespace Sharpbench.Runner;

internal class JobRunner
{
    IJobRepository jobs;
    IJobMessageStream messages;
    ILogger<Worker> logger;
    string id;

    public JobRunner(string jobId, ILogger<Worker> logger, IJobRepository jobs, IJobMessageStream messageStream)
    {
        this.jobs = jobs;
        this.messages = messageStream;
        this.id = jobId;
        this.logger = logger;
    }

    public async Task RunJob()
    {
        var job = await this.jobs.GetJob(this.id);
        await this.RunJob(job);
    }

    private async Task RunJob(Job job)
    {
        await this.jobs.ReportJobStarted(job.Id);


        var tempFolderName = Path.GetRandomFileName(); ;
        var tempProjectDir = Path.Combine(Path.GetTempPath(), tempFolderName);
        Directory.CreateDirectory(tempProjectDir);

        // copy files from the project template to the temp project dir
        var cwd = new DirectoryInfo(Directory.GetCurrentDirectory());
        this.logger.LogInformation($"cwd: {cwd}");
        var projectTemplateDir = Path.Combine(Directory.GetCurrentDirectory(), "project-template");
        foreach (var file in Directory.GetFiles(projectTemplateDir))
        {
            this.logger.LogInformation($"file {file}");
            var destFile = Path.Combine(tempProjectDir, Path.GetFileName(file));
            this.logger.LogInformation($"Copying '{file} to '{destFile}");
            File.Copy(file, destFile, overwrite: true);
        }

        var userBenchmarkClass = Path.Combine(tempProjectDir, "Benhmarks.cs");
        this.logger.LogInformation($"Writing user code in {userBenchmarkClass}");
        File.WriteAllText(userBenchmarkClass, job.Code);


        this.logger.LogInformation($"Running job {job.Id}");


        int exitCode = await ExecuteBenchmarkProject(job.Id, tempProjectDir);
        this.logger.LogInformation("Execution complete");
        if (exitCode != 0)
        {
            // report error
            this.logger.LogInformation($"Execution failed with exit code {exitCode}");
            Directory.Delete(tempProjectDir, recursive: true);
            var failedJob =  await this.jobs.ReportJobError(job.Id, exitCode);
            await BroadcastStatusMessage(new JobCompleteMessage("jobComplete", job.Id, failedJob));

            this.logger.LogInformation($"Deleting folder '{tempProjectDir}'");
            Directory.Delete(tempProjectDir, recursive: true);
            return;
        }

        // success

        var benchmarkResultsDir = Path.Combine(tempProjectDir, "BenchmarkDotNet.Artifacts", "results");
        var ghMdPath = Directory.GetFiles(benchmarkResultsDir).FirstOrDefault(f => f.EndsWith(".md"));
        if (ghMdPath == null)
        {
            throw new Exception("Could not find markdown report");
        }

        var mdReport = File.ReadAllText(ghMdPath);
        var successJob = await this.jobs.ReportJobSuccess(job.Id, mdReport);
        await BroadcastStatusMessage(new JobCompleteMessage("jobComplete", job.Id, successJob));

        // ensure project and result files were generated correctly
        foreach (var entry in Directory.GetFileSystemEntries(tempProjectDir))
        {
            this.logger.LogInformation($"user project file: {entry}");
        }

        // TODO: clean up should be in a finally block
        this.logger.LogInformation($"Deleting folder '{tempProjectDir}'");
        Directory.Delete(tempProjectDir, recursive: true);
    }

    private async Task<int> ExecuteBenchmarkProject(string jobId, string projectDir)
    {
        this.logger.LogInformation("Starting benchmark run");
        this.logger.LogInformation("Restoring project...");
        (int exitCode, string container) = await CreateContainer(jobId, projectDir);
        if (exitCode != 0)
        {
            this.logger.LogInformation("Create container failed");
            return exitCode;
        }

        exitCode = await StartContainer(jobId, projectDir, container);
        if (exitCode != 0)
        {
            this.logger.LogInformation("Failed to start container");
            return exitCode;
        }

        exitCode = await StreamContainerLogs(jobId, projectDir, container);
        if (exitCode != 0)
        {
            this.logger.LogInformation("Failed to stream container logs");
            return exitCode;
        }

        exitCode = await RemoveContainer(jobId, projectDir, container);
        if (exitCode != 0)
        {
            this.logger.LogInformation("Failed to remove container");
            return exitCode;
        }

        return exitCode;
        // int restoreExitCode = await RunRestore(jobId, projectDir);
        // if (restoreExitCode != 0)
        // {
        //     this.logger.LogInformation("Restore failed");
        //     return restoreExitCode;
        // }

        // this.logger.LogInformation("Running project...");
        // int runExitCode = await RunBuildAndRun(jobId, projectDir);
        // if (runExitCode != 0)
        // {
        //     this.logger.LogInformation("Run failed");
        // }

        // return runExitCode;
    }

    private Task<int> RunRestore(string jobId, string projectDir)
    {
        // TODO instead of ignoring failed sources, we could specify a Nuget config
        // file that specifies the sources to target
        return RunDotnetStep(jobId, projectDir, "restore -s https://api.nuget.org/v3/index.json --ignore-failed-sources");
    }

    private Task<int> RunBuildAndRun(string jobId, string projectDir)
    {
        // We should run this after restore has completed;
        // TODO: I set inProcess option as hack to avoid building the benchmarked code separately
        // cause that leads to Nuget restore issues on my machine because of the credentials issue
        // I should disable this once I containerize these background jobs
        return RunDotnetStep(jobId, projectDir, "run -c Release --no-restore -- --filter=* --inProcess");
    }

    private async Task<(int exitCode, string container)> CreateContainer(string jobId, string projectDir)
    {
        var image = "habbes/sharpbench-runner";
        string container = Path.GetRandomFileName().Split('.')[0];
        int exitCode = await RunDockerStep(
            jobId,
            projectDir,
            $"create -v {projectDir}:/src --name {container} {image}"
        );

        return (exitCode, container);
    }

    private Task<int> StartContainer(string jobId, string projectDir, string containerName)
    {
        return RunDockerStep(
            jobId,
            projectDir,
            $"start {containerName}"
        );
    }

    private Task<int> StreamContainerLogs(string jobId, string projectDir, string containerName)
    {
        return RunDockerStep(
            jobId,
            projectDir,
            $"logs --follow --details {containerName}"
        );
    }

    private Task<int> RemoveContainer(string jobId, string projectDir, string containerName)
    {
        return RunDockerStep(
            jobId,
            projectDir,
            $"rm {containerName}"
        );
    }

    private async Task<int> RunDockerStep(string jobId, string projectDir, string args)
    {
        try
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo("docker")
            {
                WorkingDirectory = projectDir,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = args
            };
            this.logger.LogInformation("Started process");
            // TODO should stream the outputs to the client instead
            // this.logger.LogInformation("stdout: {0}", await process.StandardOutput.ReadToEndAsync());
            // this.logger.LogInformation("stderr: {0}", await process.StandardError.ReadToEndAsync());

            process.OutputDataReceived += async void (sender, args) =>
            {
                Console.ResetColor();
                this.logger.LogInformation(args.Data);
                if (args.Data == null)
                {
                    return;
                }

                await BroadcastLogMessage(new LogMessage("log", jobId, "stdout", args.Data));
            };

            process.ErrorDataReceived += async void (sender, args) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                this.logger.LogInformation(args.Data);
                if (args.Data == null)
                {
                    return;
                }

                await BroadcastLogMessage(new LogMessage("log", jobId, "stderr", args.Data));
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

    private async Task<int> RunDotnetStep(string jobId, string projectDir, string args)
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

            this.logger.LogInformation("Started process");
            // TODO should stream the outputs to the client instead
            // this.logger.LogInformation("stdout: {0}", await process.StandardOutput.ReadToEndAsync());
            // this.logger.LogInformation("stderr: {0}", await process.StandardError.ReadToEndAsync());

            process.OutputDataReceived += async void (sender, args) =>
            {
                Console.ResetColor();
                this.logger.LogInformation(args.Data);
                if (args.Data == null)
                {
                    return;
                }

                await BroadcastLogMessage(new LogMessage("log", jobId, "stdout", args.Data));
            };

            process.ErrorDataReceived += async void (sender, args) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                this.logger.LogInformation(args.Data);
                if (args.Data == null)
                {
                    return;
                }

                await BroadcastLogMessage(new LogMessage("log", jobId, "stderr", args.Data));
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

    private Task BroadcastLogMessage(LogMessage message) => this.BroadcastMessage(message.JobId, JobMessageType.Log, message);

    private Task BroadcastStatusMessage(JobCompleteMessage message) => this.BroadcastMessage(message.JobId, JobMessageType.Status, message);

    private async Task BroadcastMessage<T>(string jobId, JobMessageType type, T message)
    {
        // TODO:
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, message);
        stream.Position = 0;
        byte[] data = stream.ToArray();

        await BroadcastRawMessage(new JobMessage(jobId, type, data));
    }

    private Task BroadcastRawMessage(JobMessage message) => this.messages.PublishMessage(message);

}

record LogMessage(string Type, string JobId, string LogSource, string Message);
record JobCompleteMessage(string Type, string JobId, Job Job);
