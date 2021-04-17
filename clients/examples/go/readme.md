# Example client library for omnikeeper in Go

## get generated go client library
```bash
GOPRIVATE=www.mhx.at go get -u www.mhx.at/gitlab/landscape/omnikeeper-client-go.git
```

# Run example
```bash
go run main.go
```
Should return a list of CIIDs from the acme-dev omnikeeper instance.

## Usage of client library
see main.go

# Authentication
Authentication is done via username + password. Make sure chosen user exists in keycloak. Make sure `oauth2cfg` is set appropriately.