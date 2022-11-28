#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
RUN apk add --no-cache tzdata # needed so .Net can properly query timezones, see https://www.stevejgordon.co.uk/timezonenotfoundexception-in-alpine-based-docker-images

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
ARG version
WORKDIR /src
COPY ["Omnikeeper/Omnikeeper.csproj", "Omnikeeper/"]
COPY ["Omnikeeper.Base/Omnikeeper.Base.csproj", "Omnikeeper.Base/"]
RUN dotnet restore "Omnikeeper/Omnikeeper.csproj" /p:Version="${version}"
COPY . .

WORKDIR "/src/Omnikeeper"
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
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

HEALTHCHECK CMD wget --spider http://127.0.0.1/health || exit
