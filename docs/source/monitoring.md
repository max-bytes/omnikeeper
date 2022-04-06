# Monitoring

An omnikeeper instance can be monitored via HTTP REST API. The omnikeeper backend exposes an endpoint at `/health`, which can be queried (via HTTP GET) for the omnikeeper's overall health. The main status indicator is the HTTP status code:
* 200 OK: Healthy
* 503 Service Unavailable: Unhealthy

The endpoint's response is a JSON object with more details.