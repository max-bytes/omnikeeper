import React from "react";
import { Menu } from "antd";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faPlus, faSearch } from "@fortawesome/free-solid-svg-icons";
import "antd/dist/antd.min.css";
import {PrivateRoute} from 'components/PrivateRoute';
import { Redirect, Route, Switch, Link, useRouteMatch, useLocation } from 'react-router-dom'
import { name as pluginName, version as pluginVersion, description as pluginDescription } from './package.json';
import AddNewContext from "./AddNewContext";
import Explorer from "./Explorer";
import useSwaggerClient from "utils/useSwaggerClient";

const pluginTitle = "Generic JSON Ingest";
const apiVersion = 1;

function ManageComponent(props) {
    let { path, url } = useRouteMatch();
    const { pathname } = useLocation();

    const { data: swaggerClient, loading, error } = useSwaggerClient();

    if (error) return "Error:" + error;
    if (loading) return "Loading...";

    return <>
        <Route
            render={({ location, }) => {
                const locPath = location.pathname.split("/");
                const locPathLast = locPath[locPath.length-1];
                const selectedKey = locPathLast === "create-context" ? "create-context" : "explorer";
                const items = [
                    { key: "explorer", label: <Link to={`${url}/explorer`}><FontAwesomeIcon icon={faSearch} style={{marginRight: "10px"}}/>Contexts</Link> },
                    { key: "create-context", label: <Link to={`${url}/create-context`}><FontAwesomeIcon icon={faPlus} style={{marginRight: "10px"}}/>Create New Context</Link> },
                ];
                return <Menu mode="horizontal" selectedKeys={selectedKey} style={{display: 'flex', justifyContent: 'center', margin: "auto"}} items={items} />;
            }}
        />
        <Switch>
            <Redirect from="/:url*(/+)" to={pathname.slice(0, -1)} /> {/* Removes trailing slashes */}
            <PrivateRoute path={`${path}/edit-context/:contextID`}>
                <AddNewContext swaggerClient={swaggerClient} apiVersion={apiVersion} editMode />
            </PrivateRoute>
            <PrivateRoute path={`${path}/create-context`}>
                <AddNewContext swaggerClient={swaggerClient} apiVersion={apiVersion} />
            </PrivateRoute>
            <PrivateRoute path={`${path}/explorer`}>
                <Explorer swaggerClient={swaggerClient} apiVersion={apiVersion} />
            </PrivateRoute>

            <PrivateRoute path={path}>
                <Redirect to={`${path}/explorer`} />
            </PrivateRoute>
        </Switch>
    </>
}

export default function OKPluginGenericJSONIngest() {
    return {
        manageComponent: (props) => <ManageComponent {...props} />,
    }
}

export const name = pluginName;
export const title = pluginTitle;
export const version = pluginVersion;
export const description = pluginDescription;