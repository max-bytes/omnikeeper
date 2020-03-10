import React from 'react';
import './App.css';
import Explorer from './components/Explorer';
import 'bootstrap/dist/css/bootstrap.min.css';
import 'semantic-ui-css/semantic.min.css'
import Keycloak from 'keycloak-js'
import { KeycloakProvider } from '@react-keycloak/web'
import {PrivateRoute} from './components/PrivateRoute'
import LoginPage from './components/LoginPage'
import { Redirect, Route, Switch, BrowserRouter  } from 'react-router-dom'
import ApolloWrapper from './components/ApolloWrapper';

const keycloak = new Keycloak({
  "realm": "landscape",
  "url": "http://localhost:8080/auth/",
  "ssl-required": "none",
  "resource": "landscape",
  "clientId": "landscape",
  "public-client": true,
  "verify-token-audience": true,
  "use-resource-role-mappings": true,
  "confidential-port": 0,
  "enable-cors": true
})

const keycloakProviderInitConfig = {
  // onLoad: 'check-sso'
}

function App() {
  return (
    <KeycloakProvider keycloak={keycloak} initConfig={keycloakProviderInitConfig}>
      <BrowserRouter basename="/" forceRefresh={false}>
        <Switch>
          <Route path="/login">
            <LoginPage></LoginPage>
          </Route>
          <PrivateRoute path="/explorer/:ciid">
            <ApolloWrapper component={Explorer} />
          </PrivateRoute>
          <Route path="*">
            <Redirect to="/explorer/H76597978" />
          </Route>
        </Switch>
      </BrowserRouter>
    </KeycloakProvider>
  );
}

export default App;
