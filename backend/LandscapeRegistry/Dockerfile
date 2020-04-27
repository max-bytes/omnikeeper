#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["LandscapeRegistry/LandscapeRegistry.csproj", "LandscapeRegistry/"]
COPY ["Base/Landscape.Base.csproj", "Base/"]
RUN dotnet restore "LandscapeRegistry/LandscapeRegistry.csproj"
COPY . .
WORKDIR "/src/LandscapeRegistry"
RUN dotnet build "LandscapeRegistry.csproj" -c Release -o /app/build

#WORKDIR /src
#COPY ["MonitoringPlugin/MonitoringPlugin.csproj", "MonitoringPlugin/"]
#RUN dotnet restore "MonitoringPlugin/MonitoringPlugin.csproj"
#WORKDIR "/src/MonitoringPlugin"
#RUN dotnet build "MonitoringPlugin.csproj" -c Release -o /app/build

WORKDIR /src
COPY ["DBMigrate/DBMigrate.csproj", "DBMigrate/"]
RUN dotnet restore "DBMigrate/DBMigrate.csproj"
WORKDIR "/src/DBMigrate"
RUN dotnet build "DBMigrate.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR "/src/LandscapeRegistry"
RUN dotnet publish "LandscapeRegistry.csproj" -c Release -o /app/publish
#WORKDIR "/src/MonitoringPlugin"
#RUN dotnet publish "MonitoringPlugin.csproj" -c Release -o /app/publish/plugins
WORKDIR "/src/DBMigrate"
RUN dotnet publish "DBMigrate.csproj" -c Release -o /app/publish/DBMigrate

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LandscapeRegistry.dll"]