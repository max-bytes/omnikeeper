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

WORKDIR /src
COPY ["TestPlugin/TestPlugin.csproj", "TestPlugin/"]
RUN dotnet restore "TestPlugin/TestPlugin.csproj"
WORKDIR "/src/TestPlugin"
RUN dotnet build "TestPlugin.csproj" -c Release -o /app/build


FROM build AS publish
WORKDIR "/src/LandscapeRegistry"
RUN dotnet publish "LandscapeRegistry.csproj" -c Release -o /app/publish
WORKDIR "/src/TestPlugin"
RUN dotnet publish "TestPlugin.csproj" -c Release -o /app/publish/plugins

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LandscapeRegistry.dll"]