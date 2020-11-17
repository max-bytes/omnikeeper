import React from "react";
import { Menu } from "antd";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faPlus, faSearch, faWrench } from "@fortawesome/free-solid-svg-icons";
import {PrivateRoute} from './../PrivateRoute'
import { Redirect, Route, Switch, BrowserRouter, Link } from 'react-router-dom'
import env from "@beam-australia/react-env";
import AddNewContext from "./AddNewContext";
import GridViewExplorer from "./GridViewExplorer";
import Context from "./Context";
import ManageContexts from "./ManageContexts";

function GridView(props) {

    // TODO: menu: set defaultSelectedKeys based on selected route

    return (
        <BrowserRouter basename={env("BASE_NAME") + "grid-view/"} forceRefresh={false}>
            <div style={{display: 'flex', flexDirection: 'column', height: '100%', paddingTop: "15px"}}>
                <Route path="*">
                    <Menu mode="horizontal" style={{display: 'flex', justifyContent: 'center', margin: "auto"}}>
                        <Menu.Item key="createNewContext" ><Link to="/create-context"><FontAwesomeIcon icon={faPlus} style={{marginRight: "10px"}}/>Create New Context</Link></Menu.Item>
                        <Menu.Item key="searchContext" ><Link to="/explorer"><FontAwesomeIcon icon={faSearch} style={{marginRight: "10px"}}/>Search Context</Link></Menu.Item>
                        <Menu.Item key="manageContexts" ><Link to="/manage-contexts"><FontAwesomeIcon icon={faWrench} style={{marginRight: "10px"}}/>Manage Contexts</Link></Menu.Item>
                    </Menu>
                </Route>
                <Switch>
                    <PrivateRoute path="/explorer/:contextName">
                        <Context />
                    </PrivateRoute>
                    <PrivateRoute path="/manage-contexts/:contextName">
                        <Context />
                    </PrivateRoute>
                    <PrivateRoute path="/create-context">
                        <AddNewContext />
                    </PrivateRoute>
                    <PrivateRoute path="/explorer">
                        <GridViewExplorer />
                    </PrivateRoute>
                    <PrivateRoute path="/manage-contexts">
                        <ManageContexts />
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