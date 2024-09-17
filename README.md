# Sharpbench

A playground for quick throwaway C# microbenchmarks.

## Setting up for development

### Docker

Ensure you have docker installed and docker engine running on your machine.

[Download and install Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Redis

Redis is used to queue jobs and store job results.

Redis needs to be installed and running before you launch the server.

[Download and install Redis](https://redis.io/docs/latest/operate/oss_and_stack/install/install-redis/)

Alternatively, you can run a redis server with docker without having to manually install it:

```cli
docker run -d --name sharpbench-redis -p 6379:6379  redis/redis-stack-server:latest
```

### Run the server

Make sure you have [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download) installed on your machine.

The server project is located in the `server` directory. It exposes the REST API that receives benchmark jobs
from the user and sends them to the queue. It also sends real time benchmark logs and results back to the user.

```cli
cd server
```

```
dotnet run
```

### Run the benchmark runner

The benchmark runner picks a job from the queue, builds and runs the project using docker.

```cli
cd runner
```

```cli
dotnet run
```

Note: Use `ctrl+c` to terminate the runner. This takes a few moments to take effect. This is an issue on the backlog.

### Run the frontend web app

Make sure you have [Node.js](https://nodejs.org) installed. I recommend [nvm-windows](https://github.com/coreybutler/nvm-windows) or [nvm](https://github.com/nvm-sh/nvm) to install and manage Node.js versions.

The frontend is the web-based user interface. It's located in the `webapp` folder.

```cli
cd webapp
```

Install dependencies

```cli
npm install
```

```cli
npm run dev
```

TODO: Use .NET Aspire to make it easier to run the project.