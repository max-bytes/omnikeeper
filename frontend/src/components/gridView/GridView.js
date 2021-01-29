import React, { useState, useEffect, useCallback } from "react";
import { Menu } from "antd";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faPlus, faSearch } from "@fortawesome/free-solid-svg-icons";
import {PrivateRoute} from './../PrivateRoute'
import { Redirect, Route, Switch, BrowserRouter, Link } from 'react-router-dom'
import env from "@beam-australia/react-env";
import AddNewContext from "./AddNewContext";
import GridViewExplorer from "./GridViewExplorer";
import Context from "./Context";
import SwaggerClient from "swagger-client";
import FeedbackMsg from "./FeedbackMsg";

function GridView(props) {

    const swaggerDefUrl = `${env('BACKEND_URL')}/../swagger/v1/swagger.json`; // HACK: BACKEND_URL contains /graphql suffix, remove!
    const apiVersion = 1;

    const [swaggerMsg, setSwaggerMsg] = useState("");
    const [swaggerErrorJson, setSwaggerErrorJson] = useState(false);
    const [swaggerClient, setSwaggerClient] = useState(null);

    // get swagger JSON
    // NOTE: we use a useEffect to (re)load the client itself
    // to make the client usable for others, the getSwaggerClient() callback is used
    // the reason this callback exists is for setting the correct, updated access token
    useEffect(() => { 
        try {
            const token = localStorage.getItem('token');
            new SwaggerClient(swaggerDefUrl, {
                authorizations: {
                    oauth2: { token: { access_token: token } },
                }
            }).then(d => {
                setSwaggerClient(d);
            });
        } catch(e) {
            setSwaggerErrorJson(JSON.stringify(e.response, null, 2));
            setSwaggerMsg(e.toString());//e.statusCode + ": " + e.response.statusText + " " + e.response.url);
        }
    }, [swaggerDefUrl]);
    const getSwaggerClient = useCallback(() => {
        // update token before returning the client
        swaggerClient.authorizations.oauth2.token.access_token = localStorage.getItem('token');
        return swaggerClient;
    }, [swaggerClient]);

    // TODO: menu: set defaultSelectedKeys based on selected route

    return (
        <BrowserRouter basename={env("BASE_NAME") + "grid-view/"} forceRefresh={false}>
            <div style={{display: 'flex', flexDirection: 'column', height: '100%', paddingTop: "15px"}}>
                <Route
                    render={({ location, history }) =>  (
                        <Menu mode="horizontal" defaultSelectedKeys={location.pathname.split("/")[1]} style={{display: 'flex', justifyContent: 'center', margin: "auto"}}>
                            <Menu.Item key="explorer" ><Link to="/explorer"><FontAwesomeIcon icon={faSearch} style={{marginRight: "10px"}}/>Contexts</Link></Menu.Item>
                            <Menu.Item key="create-context" ><Link to="/create-context"><FontAwesomeIcon icon={faPlus} style={{marginRight: "10px"}}/>Create New Context</Link></Menu.Item>
                        </Menu>
                        )}
                    />
                {!swaggerErrorJson && swaggerClient ? (
                    <Switch>
                        <PrivateRoute path="/explorer/:contextName">
                            <Context swaggerClient={getSwaggerClient} apiVersion={apiVersion} />
                        </PrivateRoute>
                        <PrivateRoute path="/edit-context/:contextName">
                            <AddNewContext swaggerClient={getSwaggerClient} apiVersion={apiVersion} editMode />
                        </PrivateRoute>
                        <PrivateRoute path="/create-context">
                            <AddNewContext swaggerClient={getSwaggerClient} apiVersion={apiVersion} />
                        </PrivateRoute>
                        <PrivateRoute path="/explorer">
                            <GridViewExplorer swaggerClient={getSwaggerClient} apiVersion={apiVersion} />
                        </PrivateRoute>

                        <PrivateRoute path="*">
                            <Redirect to="/explorer" />
                        </PrivateRoute>
                    </Switch>) : 
                    <div style={{ height: "100%" }}>
                        {swaggerMsg && <FeedbackMsg alertProps={{message: swaggerMsg, type: swaggerErrorJson ? "error": "success", showIcon: true, banner: true}} swaggerErrorJson={swaggerErrorJson} />}
                    </div>
                }
            </div>
        </BrowserRouter>
    );
}

export default GridView;