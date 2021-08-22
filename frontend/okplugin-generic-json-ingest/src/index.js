import React from "react";
import { Menu } from "antd";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faPlus, faSearch } from "@fortawesome/free-solid-svg-icons";
import "antd/dist/antd.css";
import {PrivateRoute} from 'components/PrivateRoute';
import { Redirect, Route, Switch, Link, useRouteMatch, useLocation } from 'react-router-dom'
import { name as pluginName, version as pluginVersion, description as pluginDescription } from './package.json';
import AddNewContext from "./AddNewContext";
import Explorer from "./Explorer";
import useSwaggerClient from "utils/useSwaggerClient";

const pluginTitle = "Generic JSON Ingest";
const apiVersion = 1;

export default function OKPluginGenericJSONIngest(props) {
    let { path, url } = useRouteMatch();
    const { pathname } = useLocation();

    const { data: swaggerClient, loading, error } = useSwaggerClient();

    if (error) return "Error:" + error;
    if (loading) return "Loading...";

    const ManageComponent = () =>  (
        <div style={{display: 'flex', flexDirection: 'column', height: '100%' }}>
            <Route
                render={({ location, }) => {
                    const locPath = location.pathname.split("/");
                    const locPathLast = locPath[locPath.length-1];
                    const selectedKey = locPathLast === "create-context" ? "create-context" : "explorer";
                    return  (
                        <Menu mode="horizontal" selectedKeys={selectedKey} style={{display: 'flex', justifyContent: 'center', margin: "auto"}}>
                            <Menu.Item key="explorer" ><Link to={`${url}/${pluginName}/explorer`}><FontAwesomeIcon icon={faSearch} style={{marginRight: "10px"}}/>Contexts</Link></Menu.Item>
                            <Menu.Item key="create-context" ><Link to={`${url}/${pluginName}/create-context`}><FontAwesomeIcon icon={faPlus} style={{marginRight: "10px"}}/>Create New Context</Link></Menu.Item>
                        </Menu>
                    )
                }}
            />
            <Switch>
                <Redirect from="/:url*(/+)" to={pathname.slice(0, -1)} /> {/* Removes trailing slashes */}
                <PrivateRoute path={`${path}/${pluginName}/edit-context/:contextID`}>
                    <AddNewContext swaggerClient={swaggerClient} apiVersion={apiVersion} editMode />
                </PrivateRoute>
                <PrivateRoute path={`${path}/${pluginName}/create-context`}>
                    <AddNewContext swaggerClient={swaggerClient} apiVersion={apiVersion} />
                </PrivateRoute>
                <PrivateRoute path={`${path}/${pluginName}/explorer`}>
                    <Explorer swaggerClient={swaggerClient} apiVersion={apiVersion} />
                </PrivateRoute>

                <PrivateRoute path={path}>
                <Redirect to={`${path}/${pluginName}/explorer`} />
            </PrivateRoute>
            </Switch>
        </div>
    );

    return {
        manageComponent: ManageComponent,
    }
}

export const name = pluginName;
export const title = pluginTitle;
export const version = pluginVersion;
export const description = pluginDescription;