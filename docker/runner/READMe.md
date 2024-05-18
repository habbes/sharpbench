# `habbes/sharpbench-runner` docker image

This docker image is used to run benchmarks submitted to Sharpbench.

To build the image:

```
docker build . -t habbes/sharpbench-runner:<version>
```

The ideal is to build the image for both ARM64 and x64 so the runner can run on different
platforms without having to change the docker image used in the code:

```
docker build . --platform linux/amd64 -t habbes/sharpbench-runner:<version>
```

```
docker build . --platform linux/arm64 -t habbes/sharpbench-runner:<version>
```

The image is also available on docker hub: https://hub.docker.com/repository/docker/habbes/sharpbench-runner

It's pushed using

```
docker push habbes/sharpbench-runner:<tag>
``

or

```
docker push habbes/sharpbench-runner --all-tags
```