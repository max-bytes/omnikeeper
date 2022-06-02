import React, { useState, useMemo, useEffect } from "react";
import './App.css';
import Explorer from './components/cis/Explorer';
import Diffing from './components/diffing/Diffing';
import 'antd/dist/antd.min.css';
import Keycloak from 'keycloak-js'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faExchangeAlt, faPlus, faSearch, faWrench, faTh, faList, faLayerGroup, faPlayCircle, faCog } from '@fortawesome/free-solid-svg-icons';
import {PrivateRoute} from './components/PrivateRoute'
import LoginPage from './components/LoginPage'
import AddNewCI from './components/cis/AddNewCI'
import SearchCIAdvanced from './components/search/SearchCIAdvanced'
import ChangesetList from "components/changesets/ChangesetList";
import Changeset from "components/changesets/Changeset";
import GridView from './components/gridView/GridView'
import Manage from './components/manage/Manage'
import UserBar from './components/UserBar';
import Page from './components/Page';
import { Redirect, Route, Switch, BrowserRouter, Link  } from 'react-router-dom'
import ApolloWrapper from './components/ApolloWrapper';
import env from "@beam-australia/react-env";
import { ReactKeycloakProvider } from '@react-keycloak/web'
import { Menu, Layout, Button, Drawer } from 'antd';
import { ExplorerLayers } from "components/ExplorerLayers";
import { LayerSettingsContext } from "utils/layers";
import Trait from "components/traits/Trait";
import Breadcrumbs from "utils/Breadcrumbs";
import GraphQLPlayground from "components/GraphQLPlayground";
import Dashboard from "components/dashboard/Dashboard";
import useFrontendPluginsManager from "utils/useFrontendPluginsManager";
import SubMenu from "antd/lib/menu/SubMenu";
import { useLocalStorage } from 'utils/useLocalStorage';
const { Header, Content } = Layout;

const keycloak = new Keycloak({
  "url": `${env("KEYCLOAK_URL")}`,
  "realm": env("KEYCLOAK_REALM"),
  "ssl-required": "none",
  "clientId": env("KEYCLOAK_CLIENT_ID"),
  "public-client": true,
  "verify-token-audience": true,
  "use-resource-role-mappings": true,
  "confidential-port": 0,
  "enable-cors": true
})

const keycloakProviderInitOptions = {
  onLoad: 'check-sso'
}

function LayerSettingsContextProvider(props) {
  // get layerSettings from storage, initialize state with it
  const [layerSettingsInStorage, setLayerSettingsInStorage] = useLocalStorage('layerSettings', null);
  const [layerSettings, setLayerSettings] = useState(layerSettingsInStorage);
  const memoizedLayerSettings = useMemo(
    () => ({ layerSettings, setLayerSettings }), 
    [layerSettings]
  );
  useEffect(() => setLayerSettingsInStorage(layerSettings), [setLayerSettingsInStorage, layerSettings]);
  return <LayerSettingsContext.Provider value={memoizedLayerSettings}>{props.children}</LayerSettingsContext.Provider>;
}

function App() {

  const BR = () => {
    const [layerDrawerVisible, setLayerDrawerVisible] = useState(false);

    const frontendPluginsManager = useFrontendPluginsManager();
    const frontendPlugins = frontendPluginsManager.allFrontendPlugins;
    
    return <BrowserRouter basename={env("BASE_NAME")} forceRefresh={false}>
      <LayerSettingsContextProvider>
        <Layout style={{height: '100vh', backgroundColor: 'unset'}}>
            <Header style={{ position: 'fixed', zIndex: 10, width: '100%', top: '0px', 
              borderBottom: "solid 1px #e8e8e8", boxShadow: "0 0 30px #f3f1f1", backgroundColor: 'white' }}>
              <div style={{float: 'left', width: '200px'}}>
                  <Link to="/">
                      <img
                          src={process.env.PUBLIC_URL + '/omnikeeper_logo_v1.0.png'}
                          alt="omnikeeper logo" 
                          className="logo"
                          style={{ height: "38px", margin: "4px 8px" }}
                      />
                  </Link>
              </div>
              <div style={{float: 'left', paddingLeft: '20px'}}>
                <Button onClick={e => setLayerDrawerVisible(true)} 
                  icon={<FontAwesomeIcon icon={faLayerGroup} style={{ marginRight: "0.5rem" }} />}
                  size="large">Layers</Button>
              </div>
              <div style={{float: 'right'}}>
                <UserBar />
              </div>
              <Route
                render={({ location }) =>  (
                  <Menu mode="horizontal" defaultSelectedKeys={location.pathname.split("/")[1]} style={{justifyContent: 'flex-end'}}>
                    <Menu.Item key="explorer"><Link to="/explorer"><FontAwesomeIcon icon={faSearch} style={{ marginRight: "0.5rem" }}/> Explore CIs</Link></Menu.Item>
                    <Menu.Item key="manage"><Link to="/manage"><FontAwesomeIcon icon={faCog} style={{ marginRight: "0.5rem" }}/> Manage</Link></Menu.Item>
                    <Menu.Item key="changesets"><Link to="/changesets"><FontAwesomeIcon icon={faList} style={{ marginRight: "0.5rem" }}/> Changesets</Link></Menu.Item>
                    <Menu.Item key="createCI"><Link to="/createCI"><FontAwesomeIcon icon={faPlus} style={{ marginRight: "0.5rem" }}/> Create New CI</Link></Menu.Item>
                    <SubMenu key="tools" title={<span><FontAwesomeIcon icon={faWrench} style={{ marginRight: "0.5rem" }}/>Tools</span>}>
                      <Menu.Item key="diffing"><Link to="/diffing"><FontAwesomeIcon icon={faExchangeAlt} style={{ marginRight: "0.5rem" }}/> Diffing</Link></Menu.Item>
                      <Menu.Item key="grid-view"><Link to="/grid-view"><FontAwesomeIcon icon={faTh} style={{ marginRight: "0.5rem" }}/> Grid View</Link></Menu.Item>
                      <Menu.Item key="graphql-playground"><Link to="/graphql-playground"><FontAwesomeIcon icon={faPlayCircle} style={{ marginRight: "0.5rem" }}/> GraphQL Playground</Link></Menu.Item>
                      {
                        frontendPlugins.flatMap(plugin => {
                            if (plugin.components.menuComponents)
                            {
                              const items = plugin.components.menuComponents.map(mc =>
                                <Menu.Item key={mc.url}><Link to={`${mc.url}`}><FontAwesomeIcon icon={mc.icon} style={{ marginRight: "0.5rem" }}/> {mc.title}</Link></Menu.Item>
                              ); 
                              return items;
                            }
                            else
                                return [];
                        })
                      }
                    </SubMenu>
                    
                  </Menu>
                )}
              />
            </Header>
              
            <Content className="site-layout" style={{ padding: '0 50px', marginTop: 64 }}>
              <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
                <Breadcrumbs style={{marginTop: '10px', marginBottom: '10px'}} />
                <Switch>
                  <Page path="/login" title="Login">
                    <LoginPage />
                  </Page>
                  <PrivateRoute path="/graphql-playground" title="GraphQL Playground">
                    <GraphQLPlayground />
                  </PrivateRoute>
                  <PrivateRoute path="/diffing" title="Diffing">
                    <Diffing />
                  </PrivateRoute>
                  <PrivateRoute path="/createCI" title="Create CI">
                    <AddNewCI />
                  </PrivateRoute>
                  <PrivateRoute path="/explorer/:ciid" title="View CI">
                    <Explorer />
                  </PrivateRoute>
                  <PrivateRoute path="/explorer" title="Explore CIs">
                    <SearchCIAdvanced />
                  </PrivateRoute>
                  <PrivateRoute path="/changesets/:changesetID" title="View Changeset">
                    <Changeset />
                  </PrivateRoute>
                  <PrivateRoute path="/changesets" title="Changesets">
                    <ChangesetList />
                  </PrivateRoute>
                  <PrivateRoute path="/grid-view" title="Grid-View">
                    <GridView/>
                  </PrivateRoute>
                  <PrivateRoute path="/manage" title="Manage">
                    <Manage/>
                  </PrivateRoute>
                  <PrivateRoute path="/traits/:traitID" title="View Trait">
                    <Trait />
                  </PrivateRoute>

                  {
                    frontendPlugins.flatMap(plugin => {
                      if (plugin.components.menuComponents)
                      {
                        const items = plugin.components.menuComponents.map(mc => {
                            const Component = mc.component;
                            return <PrivateRoute key={mc.url} path={mc.url} title={mc.title}>
                              <Component />
                            </PrivateRoute>
                          }
                        ); 
                        return items;
                      }
                      else
                        return [];
                    })
                  }

                  <PrivateRoute path="/">
                    <Dashboard />
                  </PrivateRoute>

                  <Route path="*">
                    <Redirect to="/" />
                  </Route>
                </Switch>
              </div>

              <Drawer 
                width={500}
                title="Layers"
                placement={"left"}
                closable={true}
                onClose={() => setLayerDrawerVisible(false)}
                visible={layerDrawerVisible}
                key="explorerLayerDrawer"
              >
                <ExplorerLayers />
              </Drawer>
            </Content>
          </Layout>
        </LayerSettingsContextProvider>
      </BrowserRouter>
  }
  

  const tokenSetter = (token) => {
    localStorage.setItem('token', token.token);
  }

  return (
    <ReactKeycloakProvider authClient={keycloak} initOptions={keycloakProviderInitOptions} autoRefreshToken={true}
      onTokens={tokenSetter}  LoadingComponent={<>Loading...</>}>
        <ApolloWrapper component={BR} />
    </ReactKeycloakProvider>
  );
}

export default App;
