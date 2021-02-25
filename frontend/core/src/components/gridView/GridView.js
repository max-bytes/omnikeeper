import React from "react";
import { Menu } from "antd";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faPlus, faSearch } from "@fortawesome/free-solid-svg-icons";
import {PrivateRoute} from './../PrivateRoute'
import { Redirect, Route, Switch, BrowserRouter, Link } from 'react-router-dom'
import env from "@beam-australia/react-env";
import AddNewContext from "./AddNewContext";
import GridViewExplorer from "./GridViewExplorer";
import Context from "./Context";

function GridView(props) {
    // TODO: menu: set defaultSelectedKeys based on selected route

    return (
        <BrowserRouter basename={env("BASE_NAME") + "grid-view/"} forceRefresh={false}>
            <div style={{display: 'flex', flexDirection: 'column', height: '100%', paddingTop: "15px"}}>
                <Route
                    render={({ location, }) =>  (
                        <Menu mode="horizontal" defaultSelectedKeys={location.pathname.split("/")[1]} style={{display: 'flex', justifyContent: 'center', margin: "auto"}}>
                            <Menu.Item key="explorer" ><Link to="/explorer"><FontAwesomeIcon icon={faSearch} style={{marginRight: "10px"}}/>Contexts</Link></Menu.Item>
                            <Menu.Item key="create-context" ><Link to="/create-context"><FontAwesomeIcon icon={faPlus} style={{marginRight: "10px"}}/>Create New Context</Link></Menu.Item>
                        </Menu>
                        )}
                    />
                <Switch>
                    <PrivateRoute path="/explorer/:contextName">
                        <Context {...props} />
                    </PrivateRoute>
                    <PrivateRoute path="/edit-context/:contextName">
                        <AddNewContext {...props} editMode />
                    </PrivateRoute>
                    <PrivateRoute path="/create-context">
                        <AddNewContext {...props} />
                    </PrivateRoute>
                    <PrivateRoute path="/explorer">
                        <GridViewExplorer {...props} />
                    </PrivateRoute>

                    <PrivateRoute path="*">
                        <Redirect to="/explorer" />
                    </PrivateRoute>
                </Switch>
            </div>
        </BrowserRouter>
    );
}

export default GridView;