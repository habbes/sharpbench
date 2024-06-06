# 2024-06-06 Benchmark runner orchestration

Currently Sharpbench has a single benchmark runner. It's a single VM on Azure with SKU `Standard D2s v3 (2 vcpus, 8 GiB memory)`, running Ubuntu 22.04. It runs a single instance of the `runner` process. The `runner` is currently designed to execute only one job at a time. If we have multiple jobs submitted concurrently, one will be picked, the rest will be queued. Lack of concurrent jobs is not a good user experience if I want multiple people to use it. However, running multiple jobs on the same server might make results more reliable (I should run experiments to understand the impact concurrent benchmarks have on results, also the impact of running benchmarks in docker vs directly on VM also the impact of running benchmarks on VM vs directly on a phyiscal machine).

Here's a summary of problems with the current setup:

- Only 1 VM -> Only job can run at a time -> long queue times when there are concurrent users.
- The VM is always on so that we can accept jobs at any time. Keeping the VM on throughout is expensive because Azure bills per hour.
- No monitoring: I don't know when runner crashes. I only realize the runner is down when I run a benchmark and it remains in the queue for a long time.
- No observability: I don't know how many jobs are running or queued at any given point in time.
- If the runner crashes for some reason, I have to manually ssh into the VM to restart it.
- No flexibility in configuring the environment the benchmark runs on:
  - I can only run benchmarks on Ubuntu 22.04 since that's the only VM I have.
  - I can only run benchmarks on the architecture supported by this VM (x86)
  - I can only run benchmarks on a specific version of .NET 8.

