package main

import (
	"context"
	"fmt"
	"os"

	"golang.org/x/oauth2"
	okclient "www.mhx.at/gitlab/landscape/omnikeeper-client-go.git"
)

func main() {
	apiVersion := "1"

	username := "omnikeeper-client-library-test"
	password := "omnikeeper-client-library-test"
	serverURL := "https://acme.omnikeeper-dev.bymhx.at/backend"

	oauth2cfg := &oauth2.Config{
		ClientID: "landscape-omnikeeper",
		Endpoint: oauth2.Endpoint{
			AuthURL:  "https://auth-dev.mhx.at/auth/realms/acme/protocol/openid-connect/auth",
			TokenURL: "https://auth-dev.mhx.at/auth/realms/acme/protocol/openid-connect/token",
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
	// response from `GetAllCIIDs`: []string
	fmt.Fprintf(os.Stdout, "Response from `CIApi.GetAllCIIDs`: %v\n", resp)
}

func exitOnError(err error) {
	if err != nil {
		fmt.Fprintf(os.Stderr, "error: %v\n", err)
		os.Exit(1)
	}
}
