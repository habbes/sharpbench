﻿using Sharpbench.Core;
using System.Diagnostics;
using System.Text.Json;

namespace Sharpbench.Runner;

internal class JobRunner
{
    IJobRepository jobs;
    IJobMessageStream messages;
    ILogger logger;
    string id;

    public JobRunner(string jobId, IJobRepository jobs, IJobMessageStream messageStream, ILogger logger)
    {
        this.jobs = jobs;
        this.messages = messageStream;
        this.id = jobId;
        this.logger = logger;
    }

    public async Task RunJob()
    {
        try
        {
            var job = await this.jobs.GetJob(this.id);
            await this.RunJob(job);
        }
        catch (Exception ex)
        {
            this.logger.LogError($"Error occurred while running job: {this.id}: {ex.Message}");
            this.logger.LogError(ex.StackTrace);
        }
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

        var userBenchmarkClass = Path.Combine(tempProjectDir, "Benchmarks.cs");
        this.logger.LogInformation($"Writing user code in {userBenchmarkClass}");
        File.WriteAllText(userBenchmarkClass, job.Code);


        this.logger.LogInformation($"Running job {job.Id}");


        CancellationTokenSource cancellation = new();
        cancellation.CancelAfter(TimeSpan.FromMinutes(8));
        
        // TODO: use bool + error details to track failure instead of exit codes
        int exitCode = 0;

        exitCode = await BuildBenchmarkProject(job.Id, tempProjectDir, cancellation.Token);
        this.logger.LogInformation("Build complete");
        if (exitCode == 0)
        {
            exitCode = await ExecuteBenchmarkProject(job.Id, tempProjectDir, cancellation.Token);
            this.logger.LogInformation("Execution complete");
        }

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

        try
        {
            var benchmarkResultsDir = Path.Combine(tempProjectDir, "BenchmarkDotNet.Artifacts", "results");
            var ghMdPath = Directory.GetFiles(benchmarkResultsDir).FirstOrDefault(f => f.EndsWith(".md"));
            if (ghMdPath == null)
            {
                throw new Exception("Could not find markdown report");
            }

            var mdReport = File.ReadAllText(ghMdPath);
            var successJob = await this.jobs.ReportJobSuccess(job.Id, mdReport);
            await BroadcastStatusMessage(new JobCompleteMessage("jobComplete", job.Id, successJob));
        }
        catch (Exception ex)
        {
            // exit code was 0, but could not find result. Build error probably occurred.
            this.logger.LogError($"Failed to report job succes: {ex.Message}");
            var failedJob = await this.jobs.ReportJobError(job.Id, -1);
            await BroadcastStatusMessage(new JobCompleteMessage("jobComplete", job.Id, failedJob));
        }

        // ensure project and result files were generated correctly
        foreach (var entry in Directory.GetFileSystemEntries(tempProjectDir))
        {
            this.logger.LogInformation($"user project file: {entry}");
        }

        // TODO: clean up should be in a finally block
        this.logger.LogInformation($"Deleting folder '{tempProjectDir}'");
        try
        {
            Directory.Delete(tempProjectDir, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
    }

    private async Task<int> BuildBenchmarkProject(string jobId, string projectDir, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Restoring benchmark project...");
        
        (int exitCode, string container) = await CreateBuildContainer(jobId, projectDir);
        if (exitCode != 0)
        {
            this.logger.LogInformation("Create build container failed");
            return exitCode;
        }

        exitCode = await RunContainer(jobId, projectDir, container, cancellationToken);
        return exitCode;
    }

    private async Task<int> ExecuteBenchmarkProject(string jobId, string projectDir, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Starting benchmark run");

        (int exitCode, string container) = await CreateContainer(jobId, projectDir);
        if (exitCode != 0)
        {
            this.logger.LogInformation("Create container failed");
            return exitCode;
        }

        exitCode = await RunContainer(jobId, projectDir, container, cancellationToken);
        return exitCode;
    }

    private async Task<int> RunContainer(string jobId, string projectDir, string container, CancellationToken cancellationToken)
    {
        int exitCode = await StartContainer(jobId, projectDir, container);
        bool failed = false;
        if (exitCode != 0)
        {
            this.logger.LogInformation("Failed to start build container");
            failed = true;
        }

        try
        {
            exitCode = await StreamContainerLogs(jobId, projectDir, container, cancellationToken);
            if (exitCode != 0)
            {
                this.logger.LogInformation("Failed to stream build container logs");
                failed = true;
            }
        }
        catch (OperationCanceledException)
        {
            this.logger.LogInformation("Job timed out and cancelled.");
            await BroadcastLogMessage(new LogMessage("log", jobId, "stderr", "The job exceeded the maximum allowed execution time."));
            failed = true;
        }

        int cleanupExitCode = await RemoveContainer(jobId, projectDir, container);
        if (cleanupExitCode != 0)
        {
            this.logger.LogInformation("Failed to remove build container");
        }

        return failed ? -1 : 0;
    }

    private async Task<(int exitCode, string container)> CreateBuildContainer(string jobId, string projectDir)
    {
        await this.BroadcastLogMessage(new LogMessage("log", jobId, "stdout", "Building project..."));
        var image = "habbes/sharpbench-runner:1.0";
        string container = Path.GetRandomFileName().Split('.')[0];
        int exitCode = await RunDockerStep(
            jobId,
            projectDir,
            $"create --memory 1GB -v {projectDir}:/src --name {container} {image} build -c Release"
        );

        return (exitCode, container);
    }

    private async Task<(int exitCode, string container)> CreateContainer(string jobId, string projectDir)
    {
        await this.BroadcastLogMessage(new LogMessage("log", jobId, "stdout", "Running benchmarks..."));
        var image = "habbes/sharpbench-runner";
        string container = Path.GetRandomFileName().Split('.')[0];
        int exitCode = await RunDockerStep(
            jobId,
            projectDir,
            $"create --network none --memory 1G -v {projectDir}:/src --name {container} {image}"
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

    private Task<int> StreamContainerLogs(
        string jobId,
        string projectDir,
        string containerName,
        CancellationToken cancellationToken
    )
    {
        return RunDockerStep(
            jobId,
            projectDir,
            $"logs --follow --details {containerName}",
            streamLogs: true,
            cancellationToken: cancellationToken
        );
    }

    private Task<int> RemoveContainer(string jobId, string projectDir, string containerName)
    {
        return RunDockerStep(
            jobId,
            projectDir,
            $"rm -f {containerName}"
        );
    }

    private async Task<int> RunDockerStep(
        string jobId,
        string projectDir,
        string args,
        bool streamLogs = false,
        CancellationToken cancellationToken = default
    )
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
            this.logger.LogInformation($"Started docker process {args}");
            // TODO should stream the outputs to the client instead

            if (streamLogs)
            {
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
            }

            bool started = process.Start();
            if (!started)
            {
                throw new Exception("Failed to start process");
            }

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            await process.WaitForExitAsync(cancellationToken);
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
