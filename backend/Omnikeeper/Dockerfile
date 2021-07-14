#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
ARG version
WORKDIR /src
COPY ["Omnikeeper/Omnikeeper.csproj", "Omnikeeper/"]
COPY ["Omnikeeper.Base/Omnikeeper.Base.csproj", "Omnikeeper.Base/"]
RUN dotnet restore "Omnikeeper/Omnikeeper.csproj" /p:Version="${version}"
COPY . .

WORKDIR "/src/Omnikeeper"
# login to private nuget repository
ARG nugetRepoUser
ARG nugetRepoPassword
RUN dotnet nuget update source "Github nuget Repository" --username "${nugetRepoUser}" --password "${nugetRepoPassword}" --store-password-in-clear-text
RUN dotnet build "Omnikeeper.csproj" /p:Version="${version}" -c Release -o /app/build

WORKDIR /src
COPY ["DBMigrate/DBMigrate.csproj", "DBMigrate/"]
RUN dotnet restore "DBMigrate/DBMigrate.csproj" /p:Version="${version}"
WORKDIR "/src/DBMigrate"
RUN dotnet build "DBMigrate.csproj" /p:Version="${version}" -c Release -o /app/build

FROM build AS publish
ARG version
WORKDIR "/src/Omnikeeper"
RUN dotnet publish "Omnikeeper.csproj" /p:Version="${version}" -c Release -o /app/publish
WORKDIR "/src/DBMigrate"
RUN dotnet publish "DBMigrate.csproj" /p:Version="${version}" -c Release -o /app/publish/DBMigrate

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "Omnikeeper.dll"]
RUN chmod +x entrypoint.sh
ENTRYPOINT ["/app/entrypoint.sh"]