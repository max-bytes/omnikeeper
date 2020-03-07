import React from 'react'
import Keycloak from 'keycloak-js'
import { KeycloakProvider } from '@react-keycloak/web'

import { AppRouter } from './routes'

const keycloak = new Keycloak({
  "realm": "landscape",
  "auth-server-url": "http://localhost:8080/auth/",
  "ssl-required": "none",
  "resource": "landscape",
  "public-client": true,
  "verify-token-audience": true,
  "use-resource-role-mappings": true,
  "confidential-port": 0
})


const keycloakProviderInitConfig = {
  onLoad: 'check-sso'
}

class App extends React.PureComponent {
  onKeycloakEvent = (event, error) => {
    console.log('onKeycloakEvent', event, error)
  }

  onKeycloakTokens = tokens => {
    console.log('onKeycloakTokens', tokens)
  }

  render() {
    return (
      <KeycloakProvider
        keycloak={keycloak}
        initConfig={keycloakProviderInitConfig}
        onEvent={this.onKeycloakEvent}
        onTokens={this.onKeycloakTokens}
      >
        <AppRouter />
      </KeycloakProvider>
    )
  }
}

export default App