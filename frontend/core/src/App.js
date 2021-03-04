import React,{ useState, useEffect, useCallback } from "react";
import './App.css';
import Explorer from './components/Explorer';
import Diffing from './components/diffing/Diffing';
import 'antd/dist/antd.css';
import Keycloak from 'keycloak-js'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faExchangeAlt, faPlus, faSearch, faWrench, faTh } from '@fortawesome/free-solid-svg-icons';
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
import { ReactKeycloakProvider } from '@react-keycloak/web'
import { Menu } from 'antd';
import SwaggerClient from "swagger-client";

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

    const swaggerDefUrl = `${env('BACKEND_URL')}/../swagger/v1/swagger.json`; // HACK: BACKEND_URL contains /graphql suffix, remove!
    const [swaggerClient, setSwaggerClient] = useState(null);

    // get swagger JSON
    // NOTE: we use a useEffect to (re)load the client itself
    // to make the client usable for others, the getSwaggerClient() callback is used
    // the reason this callback exists is for setting the correct, updated access token
    useEffect(() => { 
        try {
            const token = localStorage.getItem('token');
            new SwaggerClient(swaggerDefUrl, {
                authorizations: {
                    oauth2: { token: { access_token: token } },
                }
            }).then(d => {
                setSwaggerClient(d);
            });
        } catch(e) {
            console.error(e);
        }
    }, [swaggerDefUrl]);
    const getSwaggerClient = useCallback(() => {
        // update token before returning the client
        swaggerClient.authorizations.oauth2.token.access_token = localStorage.getItem('token');
        return swaggerClient;
    }, [swaggerClient]);

    if (!swaggerClient) return "Loading...";

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
                            <Menu.Item key="manage"><Link to="/manage"><FontAwesomeIcon icon={faWrench} style={{ marginRight: "0.5rem" }}/> Manage</Link></Menu.Item>
                            <Menu.Item key="createCI"><Link to="/createCI"><FontAwesomeIcon icon={faPlus} style={{ marginRight: "0.5rem" }}/> Create New CI</Link></Menu.Item>
                            <Menu.Item key="explorer"><Link to="/explorer"><FontAwesomeIcon icon={faSearch} style={{ marginRight: "0.5rem" }}/> Search CI</Link></Menu.Item>
                            <Menu.Item key="diffing"><Link to="/diffing"><FontAwesomeIcon icon={faExchangeAlt} style={{ marginRight: "0.5rem" }}/> Diffing</Link></Menu.Item>
                            <Menu.Item key="grid-view" style={{ marginRight: "60px" }}><Link to="/grid-view"><FontAwesomeIcon icon={faTh} style={{ marginRight: "0.5rem" }}/> Grid View</Link></Menu.Item>
                            <Menu.Divider/>
                            <UserBar disabled={true} style={{ cursor: "unset" }} />
                        </Menu>
                    )}
                />
            </div>
        </nav>
           
          <div style={{ height: 'calc(100% - 48px)' }}>
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
                <GridView swaggerClient = {getSwaggerClient} />
              </PrivateRoute>
              <PrivateRoute path="/manage">
                <Manage swaggerClient = {getSwaggerClient} />
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
