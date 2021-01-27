import React from 'react';
import './App.css';
import Explorer from './components/Explorer';
import Diffing from './components/diffing/Diffing';
import 'semantic-ui-css/semantic.min.css'
import 'antd/dist/antd.css';
import Keycloak from 'keycloak-js'
import { Icon } from 'semantic-ui-react'
import {PrivateRoute} from './components/PrivateRoute'
import LoginPage from './components/LoginPage'
import AddNewCI from './components/AddNewCI'
import SearchCIAdvanced from './components/search/SearchCIAdvanced'
import GridView from './components/gridView/GridView'
import Manage from './components/manage/Manage'
import UserBar from './components/UserBar';
import { Redirect, Route, Switch, BrowserRouter, Link  } from 'react-router-dom'
import ApolloWrapper from './components/ApolloWrapper';
import env from "@beam-australia/react-env";
import ManagePredicates from './components/manage/ManagePredicates';
import ManageBaseConfiguration from './components/manage/ManageBaseConfiguration';
import ManageLayers from './components/manage/ManageLayers';
import ManageOIAContexts from './components/manage/ManageOIAContexts';
import ManageODataAPIContexts from './components/manage/ManageODataAPIContexts';
import ManageTraits from './components/manage/ManageTraits';
import ManageCache from './components/manage/ManageCache';
import ManageCurrentUser from './components/manage/ManageCurrentUser';
import ShowLogs from './components/manage/ShowLogs';
import ShowVersion from './components/manage/ShowVersion';
import { ReactKeycloakProvider } from '@react-keycloak/web'
import LayerOperations from 'components/manage/LayerOperations';
import { Menu } from 'antd';

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

const keycloakProviderInitOptions = {
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
        <nav style={{
                    borderBottom: "solid 1px #e8e8e8",
                    overflow: "hidden",
                    boxShadow: "0 0 30px #f3f1f1",
                }}>
            <div style={{ width: "200px", float: "left" }}>
                <Link to="/">
                    <img
                        src={process.env.PUBLIC_URL + '/omnikeeper_logo_v1.0.png'}
                        alt="omnikeeper logo" 
                        className="logo"
                        style={{ height: "38px", margin: "4px 8px" }}
                    />
                </Link>
            </div>
            <div style={{ width: "calc(100% - 200px)", float: "left" }}>
            <Route
                render={({ location, history }) =>  (
                        <Menu mode="horizontal" defaultSelectedKeys={location.pathname.split("/")[1]} style={{ position: "relative", display: "flex", justifyContent: "flex-end", borderBottom: "none" }}>
                            <Menu.Item key="manage"><Link to="/manage"><Icon name="wrench" /> Manage</Link></Menu.Item>
                            <Menu.Item key="createCI"><Link to="/createCI"><Icon name="plus" /> Create New CI</Link></Menu.Item>
                            <Menu.Item key="explorer"><Link to="/explorer"><Icon name="search" /> Search CI</Link></Menu.Item>
                            <Menu.Item key="diffing"><Link to="/diffing"><Icon name="exchange" /> Diffing</Link></Menu.Item>
                            <Menu.Item key="grid-view" style={{ marginRight: "60px" }}><Link to="/grid-view"><Icon name="grid layout" /> Grid View</Link></Menu.Item>
                            <Menu.Divider/>
                            <UserBar disabled={true} style={{ cursor: "unset" }} />
                        </Menu>
                    )}
                />
            </div>
        </nav>
           
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
                <SearchCIAdvanced />
              </PrivateRoute>
              <PrivateRoute exact path="/grid-view">
                <Redirect to="/grid-view/explorer" />
              </PrivateRoute>
              <PrivateRoute path="/grid-view">
                <GridView />
              </PrivateRoute>
              
              <PrivateRoute path="/manage/baseconfiguration">
                <ManageBaseConfiguration />
              </PrivateRoute>
              <PrivateRoute path="/manage/predicates">
                <ManagePredicates />
              </PrivateRoute>
              <PrivateRoute path="/manage/layers/operations/:layerID">
                <LayerOperations />
              </PrivateRoute>
              <PrivateRoute path="/manage/layers">
                <ManageLayers />
              </PrivateRoute>
              <PrivateRoute path="/manage/oiacontexts">
                <ManageOIAContexts />
              </PrivateRoute>
              <PrivateRoute path="/manage/odataapicontexts">
                <ManageODataAPIContexts />
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
              <PrivateRoute path="/manage/version">
                <ShowVersion />
              </PrivateRoute>
              <PrivateRoute path="/manage/logs">
                <ShowLogs />
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
  

  const tokenSetter = (token) => {
    localStorage.setItem('token', token.token);
  }

  return (
    <ReactKeycloakProvider authClient={keycloak} initOptions={keycloakProviderInitOptions} 
      onTokens={tokenSetter}  LoadingComponent={<>Loading...</>}>
      <div style={{height: '100%'}}>
        <ApolloWrapper component={BR} />
      </div>
    </ReactKeycloakProvider>
  );
}

export default App;
