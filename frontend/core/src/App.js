import React, { useState } from "react";
import './App.css';
import Explorer from './components/cis/Explorer';
import Diffing from './components/diffing/Diffing';
import 'antd/dist/antd.min.css';
import Keycloak from 'keycloak-js'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faExchangeAlt, faPlus, faSearch, faWrench, faTh, faList, faLayerGroup, faPlayCircle } from '@fortawesome/free-solid-svg-icons';
import {PrivateRoute} from './components/PrivateRoute'
import LoginPage from './components/LoginPage'
import AddNewCI from './components/cis/AddNewCI'
import SearchCIAdvanced from './components/search/SearchCIAdvanced'
import ChangesetList from "components/changesets/ChangesetList";
import Changeset from "components/changesets/Changeset";
import GridView from './components/gridView/GridView'
import Manage from './components/manage/Manage'
import UserBar from './components/UserBar';
import { Redirect, Route, Switch, BrowserRouter, Link  } from 'react-router-dom'
import ApolloWrapper from './components/ApolloWrapper';
import env from "@beam-australia/react-env";
import { ReactKeycloakProvider } from '@react-keycloak/web'
import { Menu, Layout, Button, Drawer } from 'antd';
import ExplorerLayers from "components/ExplorerLayers";
import Trait from "components/traits/Trait";
import Breadcrumbs from "utils/Breadcrumbs";
import GraphQLPlayground from "components/GraphQLPlayground";
import Dashboard from "components/dashboard/Dashboard";
const { Header, Content } = Layout;

const keycloak = new Keycloak({
  "url": `${env("KEYCLOAK_URL")}/auth`,
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
  // workaround, disabling of checking iframe cookie, because its a cross-site one, and chrome stopped accepting them
  // when they don't have SameSite=None set... and keycloak doesn't send a proper cookie yet: 
  // https://issues.redhat.com/browse/KEYCLOAK-12125
  "checkLoginIframe": false,
  onLoad: 'check-sso',
  // promiseType: 'native'
}

function App() {

  const BR = () => {
    const [layerDrawerVisible, setLayerDrawerVisible] = useState(false);
  
    return <BrowserRouter basename={env("BASE_NAME")} forceRefresh={false}>
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
                  <Menu.Item key="manage"><Link to="/manage"><FontAwesomeIcon icon={faWrench} style={{ marginRight: "0.5rem" }}/> Manage</Link></Menu.Item>
                  <Menu.Item key="changesets"><Link to="/changesets"><FontAwesomeIcon icon={faList} style={{ marginRight: "0.5rem" }}/> Changesets</Link></Menu.Item>
                  <Menu.Item key="diffing"><Link to="/diffing"><FontAwesomeIcon icon={faExchangeAlt} style={{ marginRight: "0.5rem" }}/> Diffing</Link></Menu.Item>
                  <Menu.Item key="grid-view"><Link to="/grid-view"><FontAwesomeIcon icon={faTh} style={{ marginRight: "0.5rem" }}/> Grid View</Link></Menu.Item>
                  <Menu.Item key="createCI"><Link to="/createCI"><FontAwesomeIcon icon={faPlus} style={{ marginRight: "0.5rem" }}/> Create New CI</Link></Menu.Item>
                  <Menu.Item key="graphql-playground"><Link to="/graphql-playground"><FontAwesomeIcon icon={faPlayCircle} style={{ marginRight: "0.5rem" }}/> GraphQL Playground</Link></Menu.Item>
                </Menu>
              )}
            />
          </Header>
            
          <Content className="site-layout" style={{ padding: '0 50px', marginTop: 64 }}>
            <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
              <Breadcrumbs style={{marginTop: '10px', marginBottom: '10px'}} />
              <Switch>
                <Route path="/login">
                  <LoginPage />
                </Route>
                <PrivateRoute path="/graphql-playground">
                  <GraphQLPlayground />
                </PrivateRoute>
                <PrivateRoute path="/diffing">
                  <Diffing />
                </PrivateRoute>
                <PrivateRoute path="/createCI">
                  <AddNewCI />
                </PrivateRoute>
                <PrivateRoute path="/explorer/:ciid">
                  <Explorer />
                </PrivateRoute>
                <PrivateRoute path="/explorer">
                  <SearchCIAdvanced />
                </PrivateRoute>
                <PrivateRoute path="/changesets/:changesetID">
                  <Changeset />
                </PrivateRoute>
                <PrivateRoute path="/changesets">
                  <ChangesetList />
                </PrivateRoute>
                <PrivateRoute path="/grid-view">
                  <GridView/>
                </PrivateRoute>
                <PrivateRoute path="/manage">
                  <Manage/>
                </PrivateRoute>
                <PrivateRoute path="/traits/:traitID">
                  <Trait />
                </PrivateRoute>
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
      </BrowserRouter>
  }
  

  const tokenSetter = (token) => {
    localStorage.setItem('token', token.token);
  }

  return (
    <ReactKeycloakProvider authClient={keycloak} initOptions={keycloakProviderInitOptions} 
      onTokens={tokenSetter}  LoadingComponent={<>Loading...</>}>
        <ApolloWrapper component={BR} />
    </ReactKeycloakProvider>
  );
}

export default App;
