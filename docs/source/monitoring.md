# Monitoring

An omnikeeper instance can be monitored via HTTP REST API. The omnikeeper backend exposes an endpoint at `/health`, which can be queried (via HTTP GET) for the omnikeeper's overall health. The main status indicator is the HTTP status code:
* 200 OK: Healthy
* 503 Service Unavailable: Unhealthy

The endpoint's response is a JSON object with more details.

omnikeeper health checks are implemented using ASP.NET Core Health Checks. See https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-6.0 for more information.