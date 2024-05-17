# 2024-05-17 Abuse prevention

I want to announce Sharpbench to the public. It's possible that some people in the wild might want to try it out. The fact that the Sharpbench runs arbitrary user code, it might also attract malicious actors.

Before talking publicly about Sharpbench, I want to put some measures to prevent some common cases of abuse:

- prevent networking to avoid people using Sharpbench to issue DDoS attacks, send Spam, mine crypto, etc.
- limit memory to avoid people allocating so much memory that it causes the host VM to crash
- limit runtime to avoid people blocking the queue by running e.g. an infinite loop

Since the benchmarks are executed in a container that's not privileged, this provides some level of security. I should conduct more research about Docker to know what attack vectors are possible and whether there are mechanisms can be exploited to escape out of the sanbox.

I can prevent networking and limit memory when starting the docker container. As for the runtime, I can keep a timer in the benchmark runner that will terminate the process of it takes longer than some maximum threshold.

At the moment, the container uses `dotnet run` to run the benchmarks. This runs `dotnet restore` by default, which needs Internet access to install dependencies from NuGet. So disabling networking on the container will cause builds to fail. To address this, I'm considering the following options:

- The image should have a local copy of the dependencies including BenchmarkDotNet
- Create two containers: the first will run `dotnet restore` on the mounted volume that contains the project source. This container will have networking enabled. The second container will run the project with no networking.
- Create two container. The first will run `dotnet restore` on a project template and "cache" the output. The cached output will be re-used when mounting the project source for each new job to the runner container.

While the first and last option attempt to avoid the repeated cost of restoring projects when they all currently use the same dependencies, I think I'll go with option 2 because it's easier to implement and easier for operations.

Since right now I only have one VM, which only run one job at a time, limiting the runtime is crucial to prevent one job from hogging the machine and blocking the queue indefinitely. However, even with enforced timeouts, it's possible for a malicious attacker to block the queue sending jobs repeatedly before anyone else does. This could be mitigated by having some sort of throttling, requiring authentication, etc. but I don't plan on addressing this issue for now. Let's see how it plays out. I think the most important thing is that networking is disabled and that the user does not escape the docker sandbox. The other issues would affect service availability, which is less of a concern at the moment. I will work on improving availability and resilience over time (and security too).
