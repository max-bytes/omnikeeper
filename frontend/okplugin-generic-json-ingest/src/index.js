import React from "react";
import { Menu } from "antd";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faPlus, faSearch } from "@fortawesome/free-solid-svg-icons";
import "antd/dist/antd.css";
import {PrivateRoute} from 'components/PrivateRoute';
import { Redirect, Route, Switch, BrowserRouter, Link } from 'react-router-dom'
import env from "@beam-australia/react-env";
import { name as pluginName, version as pluginVersion, description as pluginDescription } from './package.json';
import AddNewContext from "./AddNewContext";
import Explorer from "./Explorer";

const pluginTitle = "Generic JSON Ingest";
const apiVersion = 1;

export default function OKPluginGenericJSONIngest(props) {
    const ManageComponent = () =>  (
        <BrowserRouter basename={env("BASE_NAME") + "manage/okplugin-generic-json-ingest"} forceRefresh={false}> {/* TODO: get rid of hardcoded manage in path */}
            <div style={{display: 'flex', flexDirection: 'column', height: '100%' }}>
                <Route
                    render={({ location, }) =>  (
                        <Menu mode="horizontal" defaultSelectedKeys={location.pathname.split("/")[1]} style={{display: 'flex', justifyContent: 'center', margin: "auto"}}>
                            <Menu.Item key="explorer" ><Link to="/explorer"><FontAwesomeIcon icon={faSearch} style={{marginRight: "10px"}}/>Contexts</Link></Menu.Item>
                            <Menu.Item key="create-context" ><Link to="/create-context"><FontAwesomeIcon icon={faPlus} style={{marginRight: "10px"}}/>Create New Context</Link></Menu.Item>
                        </Menu>
                        )}
                    />
                <Switch>
                    <PrivateRoute path="/edit-context/:contextName">
                        <AddNewContext {...props} apiVersion={apiVersion} editMode />
                    </PrivateRoute>
                    <PrivateRoute path="/create-context">
                        <AddNewContext {...props} apiVersion={apiVersion} />
                    </PrivateRoute>
                    <PrivateRoute path="/explorer">
                        <Explorer {...props} apiVersion={apiVersion} />
                    </PrivateRoute>
                    <PrivateRoute path="*">
                        <Redirect to="/explorer" />
                    </PrivateRoute>
                </Switch>
            </div>
        </BrowserRouter>
    );

    return {
        manageComponent: ManageComponent,
    }
}

export const name = pluginName;
export const title = pluginTitle;
export const version = pluginVersion;
export const description = pluginDescription;