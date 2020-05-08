import React from 'react';
import './App.css';
import Explorer from './components/Explorer';
import 'bootstrap/dist/css/bootstrap.min.css';
import 'semantic-ui-css/semantic.min.css'
import Keycloak from 'keycloak-js'
import { Menu, Icon } from 'semantic-ui-react'
import { KeycloakProvider } from '@react-keycloak/web'
import {PrivateRoute} from './components/PrivateRoute'
import LoginPage from './components/LoginPage'
import AddNewCI from './components/AddNewCI'
import SearchCI from './components/SearchCI'
import Manage from './components/manage/Manage'
import UserBar from './components/UserBar';
import { Redirect, Route, Switch, BrowserRouter, Link  } from 'react-router-dom'
import ApolloWrapper from './components/ApolloWrapper';
import env from "@beam-australia/react-env";
import ManagePredicates from './components/manage/ManagePredicates';
import ManageLayers from './components/manage/ManageLayers';
import ManageCITypes from './components/manage/ManageCITypes';
import ManageTraits from './components/manage/ManageTraits';
import { useKeycloak } from '@react-keycloak/web'
import { useEffect } from 'react';

  // TODO: move?
function KeycloakTokenSetter() {
  const [ keycloak ] = useKeycloak();
  useEffect(() => {
      localStorage.setItem('token', keycloak.token);
  }, [keycloak.token]);
  return null;
}

const keycloak = new Keycloak({
  "realm": env("KEYCLOAK_REALM"),
  "url": env("KEYCLOAK_URL"),
  "ssl-required": "none",
  "resource": env("KEYCLOAK_RESOURCE"),
  "clientId": env("KEYCLOAK_CLIENT_ID"),
  "public-client": true,
  "verify-token-audience": true,
  "use-resource-role-mappings": true,
  "confidential-port": 0,
  "enable-cors": true
})

const keycloakProviderInitConfig = {
  // workaround, disabling of checking iframe cookie, because its a cross-site one, and chrome stopped accepting them
  // when they don't have SameSite=None set... and keycloak doesn't send a proper cookie yet: 
  // https://issues.redhat.com/browse/KEYCLOAK-12125
  "checkLoginIframe": false,
  onLoad: 'check-sso',
  promiseType: 'native'
}
//'/landscape/registry'
function App() {
  return (
    <KeycloakProvider keycloak={keycloak} initConfig={keycloakProviderInitConfig}>
      <div style={{height: '100%'}}>
        <KeycloakTokenSetter />
        <BrowserRouter basename={env("BASE_NAME")} forceRefresh={false}>
          <Menu fixed='top' inverted style={{display: 'flex', justifyContent: 'space-between'}}>
            <div>
              <Menu.Item style={{fontSize:'1.2em'}}>Landscape Registry</Menu.Item>
            </div>
            <div style={{flexGrow: 1}}></div>
            <div style={{display:'flex'}}>
              <Route path="*">
                <Menu.Item><Link to="/manage"><Icon name="wrench" /> Manage</Link></Menu.Item>
                <Menu.Item><Link to="/createCI">Create New CI</Link></Menu.Item>
                <Menu.Item><Link to="/explorer">Search CI</Link></Menu.Item>
              </Route>
              <UserBar />
            </div>
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
              
              <PrivateRoute path="/manage/predicates">
                <ApolloWrapper component={ManagePredicates} />
              </PrivateRoute>
              <PrivateRoute path="/manage/layers">
                <ApolloWrapper component={ManageLayers} />
              </PrivateRoute>
              <PrivateRoute path="/manage/citypes">
                <ApolloWrapper component={ManageCITypes} />
              </PrivateRoute>
              <PrivateRoute path="/manage/traits">
                <ApolloWrapper component={ManageTraits} />
              </PrivateRoute>
              <PrivateRoute path="/manage">
                <ApolloWrapper component={Manage} />
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
