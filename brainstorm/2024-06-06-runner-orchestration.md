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

## Orchestration goals

- Flexibility:
  - Allow user to choose target .NET version
  - Allow user to choose target architecture (x86, arm)
  - Allow user to choose target OS (at least Windows and Linux)
  - Manage pool of runners and queue job to the runner that meets the requirements
- Observability:
  - Find out real-time state of machines (running a job, idle, offline, version of benchmark runner, etc.)
  - Find out when runners have crash and why
  - Be able to recover automatically from failed machines
  - Be able to tell how many jobs are queued, how many are running at any given point in time
  - Track failure rate, find patterns in of failed jobs
  - Track regions where jobs come from?
- Cost management:
  - Automatically shut down runners when idle and starting them when job available
- Run multiple jobs (from different users) concurrently. The actual number will depend on budget. Ideally we'd want to maximize the number of concurrent jobs we can run for a given budget without considerably impacting reliability of benchmark results.
- Ease of operation:
  - Relatively easy to add and remove vms to the pool
  - Keep track of capabilities of each VM (OS, arch, supported .NET version)
  - Remotely restart failed servers, or even re-install software
  - Update runners to the latest version of sharpbench (automatically)
  - Install OS updates without having to manually ssh into servers
- Resilience
  - TODO

## Architecture and implementation

To think about implementation, let me relax the requirements, start simple and build up from there.

### Level 1: Cost-effective concurrent jobs

For a start, I can settle with supporting concurrent jobs without breaking the bank (i.e. the free $150 monthly Azure Credits I get from my company). At this level I don't care about flexibility: it's okay if only one OS, .NET version, architecture etc. are supported. At this level I also don't want to worry about ops. If I limit myself to a handful of VMs, I can still manage them by the occasional ssh, and Azure portal.

Assuming I'll only run nore more than one benchmark in a VM at a time, the solution here is to have multiple VMs. Each VM will have the sharpbench runner installed and will listen to the same queue. This way the server doesn't change. It will keep sending jobs to the same queue. Any idle runner can dequeue the next available job from the queue.

To keep the costs manageable, I should shut down VMs when they're idle and there are no jobs available. When jobs are available, I should turn some of the VMs back on. Turning a VM on when a job arrives lead to a cold-start issues where the first job(s) that arrive when all machines are off will stay in the queue for longer. If jobs are sparse, then most jobs will experience cold-start and that will negatively affect the overall user experience and perception of the service. I'll look into the cold-start problem later.

I'll need a system that:
- knows about all the available runner VMS
- can tell when VMs are idle and when they're busy
- can turn off VMs
- can boot VMs
- can effectively decide when to turn off/on a particular VM

Here are my initial thoughts.

**Tracking VM state**:

- Create a service that will keep track of all VM state. Let's refer to it as WorkerManager
- Each runner sends a signal to the worker manager when it's state changes:
  - before it starts executing a job
  - after it finishes executing a job
- WorkerManager uses state signals from runners to keep track of the current state of each runner
- For the sake of resilience, runner should require an acknowledgement from WorkerManager before commit to the new state (i.e. should wait for ack before exeuting the job or going back to the idle loop)
  - this adds a delay to starting jobs (which should be minimal in healthy cases), but it helps ensure that the WorkerManager has a consistent view of the runner's current state. If the message does not get delivered to the WorkerManager, it might assume the runner is idle when it's executing a job and shut it down in the middle of a job, or it might assume it's busy when it's idle and keep it on indefinitely, increasing costs.
  - it's possible that the state change signal was delivered to the WorkerManager, but the acknowledgement fails to reach or get processed by the runner. In this case, the WorkerManager knows that the runner is busy (or idle) but the runner is not aware that the WorkerManager knows. How do we solve this?
     - The runner could retry sending the message N times until it processes the acknowledgement. This means the WorkerManager receiving the same state message should be idempotent.
     - If the runner fails to process an acknowledgement after N tries, it should not proceed. It should intentionall enter an unhealthy state and wait for the system to recover (TODO: think about what this actually looks like)
- Each runner sends a heartbeat periodically to the WorkerManager
  - If a WorkerManager doesn't receive a heartbeat from a given runner after a given threshold, it could consider the VM down (or unhealthy)





