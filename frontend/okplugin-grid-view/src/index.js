import React from "react";
import { Menu } from "antd";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faTh, faPlus, faSearch } from "@fortawesome/free-solid-svg-icons";
import {PrivateRoute} from 'components/PrivateRoute'
import { Redirect, Route, Switch, Link, } from 'react-router-dom'
import { name as pluginName, version as pluginVersion, description as pluginDescription } from './package.json';
import AddNewContext from "./AddNewContext";
import GridViewExplorer from "./GridViewExplorer";
import Context from "./Context";
import useSwaggerClient from "utils/useSwaggerClient";

const apiVersion = 1;
const pluginPath = "grid-view"

export const name = pluginName;
export const title = "Grid View";
export const version = pluginVersion;
export const description = pluginDescription;
export const path = pluginPath;
export const icon = <FontAwesomeIcon icon={faTh} style={{ marginRight: "0.5rem" }}/>;

export default function OKPluginGridView(props) {
    const { data: swaggerClient, loading, error } = useSwaggerClient();

    if (error) return "Error:" + error;
    if (loading) return "Loading...";

    // INFO: Main Menu Components do not need attributes 'path', 'url' and 'pathname', since they are placed in root
    const MainMenuComponent = () => (
        <div style={{display: 'flex', flexDirection: 'column', height: '100%' }}>
            <Route
                render={({ location, }) => {
                    const locPath = location.pathname.split("/");
                    const locPathLast = locPath[locPath.length-1];
                    const selectedKey = locPathLast === "create-context" ? "create-context" : "explorer";
                    return  (
                        <Menu mode="horizontal" selectedKeys={selectedKey} style={{display: 'flex', justifyContent: 'center', margin: "auto"}}>
                            <Menu.Item key="explorer" ><Link to={`/${pluginPath}/explorer`}><FontAwesomeIcon icon={faSearch} style={{marginRight: "10px"}}/>Contexts</Link></Menu.Item>
                            <Menu.Item key="create-context" ><Link to={`/${pluginPath}/create-context`}><FontAwesomeIcon icon={faPlus} style={{marginRight: "10px"}}/>Create New Context</Link></Menu.Item>
                        </Menu>
                    )
                }}
            />
            <Switch>
                {/* TODO: find way to get rid of trailing slashes - could cause problems */}
                <PrivateRoute path={`/${pluginPath}/explorer/:contextName`}>
                    <Context swaggerClient={swaggerClient} apiVersion={apiVersion} />
                </PrivateRoute>
                <PrivateRoute path={`/${pluginPath}/edit-context/:contextName`}>
                    <AddNewContext swaggerClient={swaggerClient} apiVersion={apiVersion} editMode />
                </PrivateRoute>
                <PrivateRoute path={`/${pluginPath}/create-context`}>
                    <AddNewContext swaggerClient={swaggerClient} apiVersion={apiVersion} />
                </PrivateRoute>
                <PrivateRoute path={`/${pluginPath}/explorer`}>
                    <GridViewExplorer swaggerClient={swaggerClient} apiVersion={apiVersion} />
                </PrivateRoute>

                <PrivateRoute path={"/"}>
                    <Redirect to={`/${pluginPath}/explorer`} />
                </PrivateRoute>
            </Switch>
        </div>
    )

    return {
        mainMenuComponent: MainMenuComponent,
    }
}
