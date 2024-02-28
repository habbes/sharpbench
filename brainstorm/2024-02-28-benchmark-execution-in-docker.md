# 2024-02-28 Packaging benchmarks to run in Docker

I've implemented a basic end-to-end benchmarking playground that runs on my machine. Here's the high-level-flow:

- The user types the code in the web app's editor and clicks Run
- The web app sends the code to the server via an API endpoint
- The web app also establishes a websocket connection with the server where it will receive real time updates about the benchmark job (e.d. stdout logs)
- The server starts a background runner to handle the benchmark
- The runner copies the `project-template` folder into a temporary directory. This temporary directory will be the benchmark project's root.
  - The `project-template` is folder in the server that is used as a baseline to build benchmark project. It contains:
    - A `.csproj` file with configure with `net8.0` and with `BenchmarkDotNet` added as a depedency
    - A `Program.cs` file that runs benchmarks in the assembly using `BenchmarkSwitcher.Run`
- Create a new `Benchmarks.cs` file in the project dir and copy the user's code in that file.
- The runner starts a new process that executes `dotnet restore` against the the temporary project root.
- After the restore process is complete. The runner starts a new process that executes `dotnet run -c Release` against the project root
- The last process runs the benchmark.
- For each of the processes, the runner streams the standard error and output messages to the client via the previously established websocket connection
- When the benchmark completes, the runner copies the contents of the markdown report and streams sends it to the client as well
- If an error occurs, the runner also sends the client a corresponding message

One issue I faced with this is that it's using the `dotnet` on my machine to run the benchmark project. The machine I was running it on has some NuGet sources that require authentication. This caused `dotnet restore` to fail. This is why I ran `dotnet restore` and `dotnet run --no-restore` as separate processes. In the `dotnet restore` command, I also specified the NuGet source explicitly and instructed it to ignore errors when retrieving certain sources.

The previous issue is good demonstration that running the benchmarks on the host machine's dotnet is not that portable and issues or conflicts can arise due to specific configuration on the user's machine. That's just one of the motivation of using Docker. The other motivation is that with using Docker I can achieve better process isolation and better security by sandboxing the benchmark process (which is running arbitrary user code) to prevent it from having free access to host's resources. I can also do things like restrict the amount CPU or RAM the docker container has available, easily terminate it after some timeout period, etc.

## Docker strategies

**Resources:**

- [Tutorial: Containerize a .NET app](https://learn.microsoft.com/en-us/dotnet/core/docker/build-container)
- [How to run BenchmarkDotNet in a Docker container](https://wojciechnagorski.com/2019/12/how-to-run-benchmarkdotnet-in-a-docker-container/)

I have a few strategies in mind with regards to which docker images to create and a few trade-offs to consider. One important thing to keep in mind is that the program to run is not known in advance since it depends on the user's code that's submitted dynamically. So we can't just package everything in a docker image ahead of time and run container from that. We need something that can allow us to build the code each time we want to run a benchmark.

### Single docker image to build and run benchmarks

This approach consists in building a single docker image based on .NET's SDK image (`FROM mcr.microsoft.com/dotnet/sdk:8.0`). Microsoft publishes official images for the sdk and runtime. The runtime images are mostly used for running production containers. They are much smaller and only contain what's required to run apps. The sdk containers are mostly used in development. It contains tools for both building and running .NET apps. I created images from both sdk and runtime base images locally for testing and the size difference was huge. The runtime-based image was 217MB and the sdk-based image was 1.2GB.

For this use case we use the SDK image because we want to build the user's code. BenchmarkDotNet also needs build tools because it builds and runs a separate process for the benchmarked code. I think we can relax the requirement of BenchmarkDotNet requiring sdk tools by using the in-process mode in which case BenchmarkDotNet will not create a separate process for the benchmarked code. However this approach makes the benchmark susceptible to side effects in the code.

We'll build an image that restores, builds and runs code from some local directory like `/app`. Then we we run the container we'll mount a volume to that project directory. The volume will map to a host directory that contains the benchmark project including the user's code.

The downside with this approach is the size of the sdk image. This may impact the cost of running the services. For example, Azure's P1 disks are 4GB and cost $0.60 per month. But I haven't tested to find out whether that will be large enough to store the OS and image comfortably. An upside of this approach is that I don't need to install dotnet on the host system.

Maybe it's possible to create a custom image that contains the dotnet sdk that's more minimal than the official image, but I don't want to get into that kind of optimizaton right now.

### SDK on host and docker image to run benchmarks

This approach consists of install the dotnet SDK directly on the host system and using docker to run the benchmarks. So the restore and publish steps would be performed on the host system directly and then this would be mounted to a docker container that's based on a dotnet runtime image. This is based on the assumption that install dotnet sdk on host takes up fewer resources compared to installing a dotnet sdk docker image. However since BenchmarkDotNet requires some dotnet sdk tools to build the benchmark process, this approach is only vialble if we restrict to the benchmarks to run in in-process mode. Another downside of this approach is that it makes the build process less portable because it relies on things being already installed and properly configured on the host machine. We also have to ensure that the sdk version on the machine is compatible with the dotnet runtime version in the container.

Actually now that I think of it, many of the approaches I had in mind are not viable if we want to support the default out-of-process of execution of BenchmarkDotNet. I think I'll just stick to the first approach for now (single docker image based on dotnet sdk and mounting the project at runtime) and consider optimizations in future.

### Concerns

When running BenchmarkDotNet in docker I see the following warning

```sh
Failed to set up high priority (Permission denied). In order to run benchmarks with high priority, make sure you have the right permissions
```

So I'll probably have to worry about security and permission configurations for the container.

