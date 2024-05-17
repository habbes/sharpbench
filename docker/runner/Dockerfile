FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src

ENTRYPOINT ["dotnet"]

CMD ["run", "--no-restore", "-c", "Release", "--", "--filter=*"]