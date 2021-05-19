# Example client library for omnikeeper in Go

## Running example 
```bash
go run main.go
```
Should return a list of CIIDs from the acme-dev omnikeeper instance.

## Using client library in other projects

### get generated go client library
```bash
GOPRIVATE=www.mhx.at go get -u www.mhx.at/gitlab/landscape/omnikeeper-client-go.git
```

### Usage of client library
see main.go

## Authentication
Authentication is done via username + password. Make sure chosen user exists in keycloak. Make sure `oauth2cfg` is set appropriately.