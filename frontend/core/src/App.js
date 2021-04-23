import React from "react";
import './App.css';
import Explorer from './components/Explorer';
import Diffing from './components/diffing/Diffing';
import 'antd/dist/antd.css';
import Keycloak from 'keycloak-js'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faExchangeAlt, faPlus, faSearch, faWrench, faChevronLeft } from '@fortawesome/free-solid-svg-icons';
import {PrivateRoute} from './components/PrivateRoute'
import LoginPage from './components/LoginPage'
import AddNewCI from './components/AddNewCI'
import SearchCIAdvanced from './components/search/SearchCIAdvanced'
import Manage from './components/manage/Manage'
import UserBar from './components/UserBar';
import { Redirect, Route, Switch, BrowserRouter, Link } from 'react-router-dom'
import ApolloWrapper from './components/ApolloWrapper';
import env from "@beam-australia/react-env";
import { ReactKeycloakProvider } from '@react-keycloak/web'
import { Menu } from 'antd';
import useFrontendPluginsManager from "utils/useFrontendPluginsManager";

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
  const { data: frontendPluginsManager, loading: frontendPluginsmanagerLoading, error: frontendPluginsmanagerError } = useFrontendPluginsManager();
  const frontendPlugins = frontendPluginsManager?.getAllFrontendPlugins();

  if (frontendPluginsmanagerError) return "Error:" + frontendPluginsmanagerError;

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
                render={({ location, }) =>  (
                        <Menu mode="horizontal" defaultSelectedKeys={location.pathname.split("/")[1]} style={{ position: "relative", display: "flex", justifyContent: "flex-end", borderBottom: "none" }}>
                            <Menu.Item key="manage"><Link to="/manage"><FontAwesomeIcon icon={faWrench} style={{ marginRight: "0.5rem" }}/> Manage</Link></Menu.Item>
                            <Menu.Item key="createCI"><Link to="/createCI"><FontAwesomeIcon icon={faPlus} style={{ marginRight: "0.5rem" }}/> Create New CI</Link></Menu.Item>
                            <Menu.Item key="explorer"><Link to="/explorer"><FontAwesomeIcon icon={faSearch} style={{ marginRight: "0.5rem" }}/> Search CI</Link></Menu.Item>
                            <Menu.Item key="diffing"><Link to="/diffing"><FontAwesomeIcon icon={faExchangeAlt} style={{ marginRight: "0.5rem" }}/> Diffing</Link></Menu.Item>                            
                            {
                                frontendPluginsmanagerLoading? <Menu.Item key="loading">Loading...</Menu.Item> :
                                frontendPlugins.map(plugin => {
                                    // only create Link for plugins containing component 'mainMenuComponent'
                                    if (plugin.components.mainMenuComponent)
                                        return <Menu.Item key={plugin.name} style={{ marginRight: "60px" }}><Link to={`/${plugin.name}`}>{plugin.icon} Grid View</Link></Menu.Item>;
                                    else
                                        return null;
                                })
                            }
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
              {
                    // 'pluginLoading'-PrivateRoute:
                    // Shows "Loading..." until frontend-plugin was loaded (if frontend-plugin doesn't exist after loading -> Shows Main-component)
                    // Without it, it would show the Main-component until frontend-plugin was loaded - then it would render the frontend-plugin-component (looks linke jumping around, not wanted!)
                    frontendPluginsmanagerLoading? <PrivateRoute path="*" key="pluginLoading">Loading...</PrivateRoute> :
                    frontendPlugins?.map(plugin => (
                            plugin.components.mainMenuComponent && // only create PrivateRoute for plugins containing component 'mainMenuComponent'
                                <PrivateRoute path={`/${plugin.name}`} key={plugin.name}>
                                    <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
                                        <div style={{ padding: "10px" }}>
                                            <h2 style={{ marginBottom: 0 }}>{plugin.title}</h2>
                                            <p>{plugin.description}</p>
                                            <div style={{marginBottom: '10px'}}><Link to={"/"}><FontAwesomeIcon icon={faChevronLeft} /> Back</Link></div>
                                        </div>
                                        {plugin.components.mainMenuComponent()}
                                    </div>
                                </PrivateRoute>
                        )
                    )
              }
              <PrivateRoute path="/manage">
                <Manage/>
              </PrivateRoute>

              {
                    !frontendPluginsmanagerLoading &&
                    <Route path="*">
                        <Redirect to="/explorer" />
                    </Route>
              }
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
