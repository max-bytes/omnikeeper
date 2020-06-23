import React from 'react';
import './App.css';
import Explorer from './components/Explorer';
import Diffing from './components/diffing/Diffing';
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
import ManageCache from './components/manage/ManageCache';
import ManageCurrentUser from './components/manage/ManageCurrentUser';
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
  // promiseType: 'native'
}

function App() {

  const BR = () => {
    return <BrowserRouter basename={env("BASE_NAME")} forceRefresh={false}>
          <Menu fixed='top' inverted style={{display: 'flex', justifyContent: 'space-between'}}>
            <div>
              <Menu.Item style={{fontSize:'1.2em'}}>Landscape metakeeper</Menu.Item>
            </div>
            <div style={{flexGrow: 1}}></div>
            <div style={{display:'flex'}}>
              <Route path="*">
                <Menu.Item><Link to="/manage"><Icon name="wrench" /> Manage</Link></Menu.Item>
                <Menu.Item><Link to="/createCI"><Icon name="plus" /> Create New CI</Link></Menu.Item>
                <Menu.Item><Link to="/explorer"><Icon name="search" /> Search CI</Link></Menu.Item>
                <Menu.Item><Link to="/diffing"><Icon name="exchange" /> Diffing</Link></Menu.Item>
              </Route>
              <UserBar />
            </div>
          </Menu>
          <div style={{height: '100%', paddingTop: '50px'}}> {/* HACK: because we are not 100% using semantic UI, move the main content down manually*/}
            <Switch>
              <Route path="/login">
                <LoginPage />
              </Route>
              <PrivateRoute path="/explorer/:ciid">
                <Explorer />
              </PrivateRoute>
              <PrivateRoute path="/diffing">
                <Diffing />
              </PrivateRoute>
              <PrivateRoute path="/createCI">
                <AddNewCI />
              </PrivateRoute>
              <PrivateRoute path="/explorer">
                <SearchCI />
              </PrivateRoute>
              
              <PrivateRoute path="/manage/predicates">
                <ManagePredicates />
              </PrivateRoute>
              <PrivateRoute path="/manage/layers">
                <ManageLayers />
              </PrivateRoute>
              <PrivateRoute path="/manage/citypes">
                <ManageCITypes />
              </PrivateRoute>
              <PrivateRoute path="/manage/traits">
                <ManageTraits />
              </PrivateRoute>
              <PrivateRoute path="/manage/cache">
                <ManageCache />
              </PrivateRoute>
              <PrivateRoute path="/manage/current-user">
                <ManageCurrentUser />
              </PrivateRoute>
              <PrivateRoute path="/manage">
                <Manage />
              </PrivateRoute>

              <Route path="*">
                <Redirect to="/explorer" />
              </Route>
            </Switch>
          </div>
        </BrowserRouter>
  }
  

  return (
    <KeycloakProvider keycloak={keycloak} initConfig={keycloakProviderInitConfig} LoadingComponent={<>Loading...</>}>
      <div style={{height: '100%'}}>
        <KeycloakTokenSetter />
        <ApolloWrapper component={BR} />
      </div>
    </KeycloakProvider>
  );
}

export default App;
