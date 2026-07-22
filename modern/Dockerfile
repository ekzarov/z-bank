FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props BankOfZ.slnx ./
COPY backend ./backend
RUN dotnet restore BankOfZ.slnx
RUN dotnet publish backend/src/BankOfZ.Api/BankOfZ.Api.csproj -c Release -o /out/api --no-restore
RUN dotnet publish backend/tools/BankOfZ.Setup/BankOfZ.Setup.csproj -c Release -o /out/setup --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /out/api ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BankOfZ.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS setup
WORKDIR /app
COPY --from=build /out/setup ./
ENTRYPOINT ["dotnet", "BankOfZ.Setup.dll"]
