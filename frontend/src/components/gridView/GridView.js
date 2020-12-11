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
    const [swaggerJson, setSwaggerJson] = useState(null);

    // get swagger JSON
    const getSwaggerJson = useCallback(async () => {
        try {
            const token = localStorage.getItem('token');
            const swaggerJson = await new SwaggerClient(swaggerDefUrl, {
                authorizations: {
                    oauth2: { token: { access_token: token } },
                }
            });
            setSwaggerJson(swaggerJson);
        } catch(e) {
            setSwaggerErrorJson(JSON.stringify(e.response, null, 2));
            setSwaggerMsg(e.toString());//e.statusCode + ": " + e.response.statusText + " " + e.response.url);
        }
    }, [swaggerDefUrl])

    useEffect(() => {getSwaggerJson();}, [getSwaggerJson]);

    // TODO: menu: set defaultSelectedKeys based on selected route

    return (
        <BrowserRouter basename={env("BASE_NAME") + "grid-view/"} forceRefresh={false}>
            <div style={{display: 'flex', flexDirection: 'column', height: '100%', paddingTop: "15px"}}>
                <Route path="*">
                    <Menu mode="horizontal" style={{display: 'flex', justifyContent: 'center', margin: "auto"}}>
                    <Menu.Item key="contexts" ><Link to="/explorer"><FontAwesomeIcon icon={faSearch} style={{marginRight: "10px"}}/>Contexts</Link></Menu.Item>
                        <Menu.Item key="createNewContext" ><Link to="/create-context"><FontAwesomeIcon icon={faPlus} style={{marginRight: "10px"}}/>Create New Context</Link></Menu.Item>
                    </Menu>
                </Route>
                { !swaggerErrorJson && swaggerJson ? (
                    <Switch>
                        <PrivateRoute path="/explorer/:contextName">
                            <Context swaggerJson={swaggerJson} apiVersion={apiVersion} />
                        </PrivateRoute>
                        <PrivateRoute path="/edit-context/:contextName">
                            <AddNewContext swaggerJson={swaggerJson} apiVersion={apiVersion} editMode />
                        </PrivateRoute>
                        <PrivateRoute path="/create-context">
                            <AddNewContext swaggerJson={swaggerJson} apiVersion={apiVersion} />
                        </PrivateRoute>
                        <PrivateRoute path="/explorer">
                            <GridViewExplorer swaggerJson={swaggerJson} apiVersion={apiVersion} />
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