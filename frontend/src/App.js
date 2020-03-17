import React from 'react';
import './App.css';
import Explorer from './components/Explorer';
import 'bootstrap/dist/css/bootstrap.min.css';
import 'semantic-ui-css/semantic.min.css'
import Keycloak from 'keycloak-js'
import { Menu } from 'semantic-ui-react'
import { KeycloakProvider } from '@react-keycloak/web'
import {PrivateRoute} from './components/PrivateRoute'
import LoginPage from './components/LoginPage'
import AddNewCI from './components/AddNewCI'
import SearchCI from './components/SearchCI'
import UserBar from './components/UserBar';
import { Redirect, Route, Switch, BrowserRouter, Link  } from 'react-router-dom'
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
      <div style={{height: '100%'}}>
        <BrowserRouter basename="/" forceRefresh={false}>
          <Menu fixed='top' inverted style={{display: 'flex', justifyContent: 'flex-end'}}>
            <Route path="*">
              <Menu.Item><Link to="/createCI">Create New CI</Link></Menu.Item>
              <Menu.Item><Link to="/explorer">Search CI</Link></Menu.Item>
            </Route>
            <UserBar />
          </Menu>
          <div style={{height: '100%', paddingTop: '50px'}}> {/* HACK: because we are not 100% using semantic UI, move the main content down manually*/}
            <Switch>
              <Route path="/login">
                <LoginPage></LoginPage>
              </Route>
              <PrivateRoute path="/explorer/:ciid">
                <ApolloWrapper component={Explorer} />
              </PrivateRoute>
              <PrivateRoute path="/createCI">
                <ApolloWrapper component={AddNewCI} />
              </PrivateRoute>
              <PrivateRoute path="/explorer">
                <ApolloWrapper component={SearchCI} />
              </PrivateRoute>
              <Route path="*">
                <Redirect to="/explorer" />
              </Route>
            </Switch>
          </div>
        </BrowserRouter>
      </div>
    </KeycloakProvider>
  );
}

export default App;
