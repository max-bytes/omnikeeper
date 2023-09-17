package main

import (
	"context"
	"fmt"
	"os"

	"golang.org/x/oauth2"
	okclient "github.com/max-bytes/omnikeeper-client-go"
)

func main() {
	apiVersion := "1"

	username := "username"
	password := "password"
	serverURL := "https://example.com/backend"

	oauth2cfg := &oauth2.Config{
		ClientID: "landscape-omnikeeper",
		Endpoint: oauth2.Endpoint{
			TokenURL: "https://example.com/auth/realms/acme/protocol/openid-connect/token",
		},
	}

	ctx := context.Background()
	token, err := oauth2cfg.PasswordCredentialsToken(ctx, username, password)
	exitOnError(err)

	configuration := okclient.NewConfiguration()
	configuration.Servers[0].URL = serverURL
	api_client := okclient.NewAPIClient(configuration)

	tokenSource := oauth2cfg.TokenSource(ctx, token)
	auth := context.WithValue(ctx, okclient.ContextOAuth2, tokenSource)

	resp, r, err := api_client.CIApi.GetAllCIIDs(auth, apiVersion).Execute()
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error when calling `CIApi.GetAllCIIDs``: %v\n", err)
		fmt.Fprintf(os.Stderr, "Full HTTP response: %v\n", r)
	}
	fmt.Fprintf(os.Stdout, "Response from `CIApi.GetAllCIIDs`: %v\n", resp)
}

func exitOnError(err error) {
	if err != nil {
		fmt.Fprintf(os.Stderr, "error: %v\n", err)
		os.Exit(1)
	}
}
