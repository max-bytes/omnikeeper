import React from "react";
import { Menu } from "antd";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faPlus, faSearch } from "@fortawesome/free-solid-svg-icons";
import {PrivateRoute} from 'components/PrivateRoute'
import { Redirect, Route, Switch, Link, useRouteMatch, useLocation } from 'react-router-dom'
import AddNewContext from "./AddNewContext";
import GridViewExplorer from "./GridViewExplorer";
import Context from "./Context";
import useSwaggerClient from "utils/useSwaggerClient";

const apiVersion = 1;

function GridView(props) {
    let { path, url } = useRouteMatch();
    const { pathname } = useLocation();

    const { data: swaggerClient, loading, error } = useSwaggerClient();

    if (error) return "Error:" + error;
    if (loading) return "Loading...";

    return (
        <div style={{display: 'flex', flexDirection: 'column', height: '100%', paddingTop: "15px"}}>
            <Route
                render={({ location, }) => {
                    const locPath = location.pathname.split("/")[2]?.toString();
                    const selectedKey = locPath === "create-context" ? "create-context" : "explorer";
                    return  (
                        <Menu mode="horizontal" selectedKeys={selectedKey} style={{display: 'flex', justifyContent: 'center', margin: "auto"}}>
                            <Menu.Item key="explorer" ><Link to={`${url}/explorer`}><FontAwesomeIcon icon={faSearch} style={{marginRight: "10px"}}/>Contexts</Link></Menu.Item>
                            <Menu.Item key="create-context" ><Link to={`${url}/create-context`}><FontAwesomeIcon icon={faPlus} style={{marginRight: "10px"}}/>Create New Context</Link></Menu.Item>
                        </Menu>
                    )
                }}
            />
            <Switch>
                <Redirect from="/:url*(/+)" to={pathname.slice(0, -1)} /> {/* Removes trailing slashes */}
                <PrivateRoute path={`${path}/explorer/:contextName`}>
                    <Context swaggerClient={swaggerClient} apiVersion={apiVersion} />
                </PrivateRoute>
                <PrivateRoute path={`${path}/edit-context/:contextName`}>
                    <AddNewContext swaggerClient={swaggerClient} apiVersion={apiVersion} editMode />
                </PrivateRoute>
                <PrivateRoute path={`${path}/create-context`}>
                    <AddNewContext swaggerClient={swaggerClient} apiVersion={apiVersion} />
                </PrivateRoute>
                <PrivateRoute path={`${path}/explorer`}>
                    <GridViewExplorer swaggerClient={swaggerClient} apiVersion={apiVersion} />
                </PrivateRoute>

                <PrivateRoute path={path}>
                    <Redirect to={`${path}/explorer`} />
                </PrivateRoute>
            </Switch>
        </div>
    );
}

export default GridView;