import React from 'react';
import { Switch, BrowserRouter, Link } from 'react-router-dom';
import env from "@beam-australia/react-env";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faChevronLeft } from '@fortawesome/free-solid-svg-icons';
import { PrivateRoute } from 'components/PrivateRoute';
import ManagePredicates from 'components/manage/ManagePredicates';
import ManageBaseConfiguration from 'components/manage/ManageBaseConfiguration';
import ManageLayers from 'components/manage/ManageLayers';
import ManageOIAContexts from 'components/manage/ManageOIAContexts';
import ManageODataAPIContexts from 'components/manage/ManageODataAPIContexts';
import ManageTraits from 'components/manage/ManageTraits';
import ManageCache from 'components/manage/ManageCache';
import ManageCurrentUser from 'components/manage/ManageCurrentUser';
import ShowLogs from 'components/manage/ShowLogs';
import ShowVersion from 'components/manage/ShowVersion';
import LayerOperations from 'components/manage/LayerOperations';
import useFrontendPluginsManager from "utils/useFrontendPluginsManager";

export default function Manage(props) {
    const { data: frontendPluginsManager, loading: frontendPluginsmanagerLoading, error: frontendPluginsmanagerError } = useFrontendPluginsManager();
    const frontendPlugins = frontendPluginsManager?.getAllFrontendPlugins();

    if (frontendPluginsmanagerError) return "Error:" + frontendPluginsmanagerError;

    return (
        <BrowserRouter basename={env("BASE_NAME") + "manage/"} forceRefresh={false}>
            <div style={{display: 'flex', flexDirection: 'column', height: '100%', paddingTop: "15px"}}>
                <Switch>
                    <PrivateRoute path="/baseconfiguration">
                        <ManageBaseConfiguration />
                    </PrivateRoute>
                    <PrivateRoute path="/predicates">
                        <ManagePredicates />
                    </PrivateRoute>
                    <PrivateRoute path="/layers/operations/:layerID">
                        <LayerOperations />
                    </PrivateRoute>
                    <PrivateRoute path="/layers">
                        <ManageLayers />
                    </PrivateRoute>
                    <PrivateRoute path="/oiacontexts">
                        <ManageOIAContexts />
                    </PrivateRoute>
                    <PrivateRoute path="/odataapicontexts">
                        <ManageODataAPIContexts />
                    </PrivateRoute>
                    <PrivateRoute path="/traits">
                        <ManageTraits />
                    </PrivateRoute>
                    <PrivateRoute path="/cache">
                        <ManageCache />
                    </PrivateRoute>
                    <PrivateRoute path="/current-user">
                        <ManageCurrentUser />
                    </PrivateRoute>
                    <PrivateRoute path="/version">
                        <ShowVersion/>
                    </PrivateRoute>
                    <PrivateRoute path="/logs">
                        <ShowLogs />
                    </PrivateRoute>
                    {
                        // 'pluginLoading'-PrivateRoute:
                        // Shows "Loading..." until frontend-plugin was loaded (if frontend-plugin doesn't exist after loading -> Shows Manage-main-component)
                        // Without it, it would show the Manage-main-component until frontend-plugin was loaded - then it would render the frontend-plugin-component (looks linke jumping around, not wanted!)
                        frontendPluginsmanagerLoading? <PrivateRoute path={"/:pluginName"} key="pluginLoading">Loading...</PrivateRoute> :
                        frontendPlugins?.map(plugin => (
                                plugin.components.manageComponent && // only create PrivateRoute for plugins containing component 'manageComponent'
                                    <PrivateRoute path={"/" + plugin.name} key={plugin.name}>
                                        <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
                                            <div style={{ padding: "10px" }}>
                                                <h2 style={{ marginBottom: 0 }}>{plugin.title}</h2>
                                                <p>{plugin.description}</p>
                                                <div style={{marginBottom: '10px'}}><Link to=""><FontAwesomeIcon icon={faChevronLeft} /> Back</Link></div>
                                            </div>
                                            {plugin.components.manageComponent()}
                                        </div>
                                    </PrivateRoute>
                            )
                        )
                    }

                    <PrivateRoute path="*">
                        <div style={{ padding: '10px' }}><h2>Management</h2>
                            <h3>Core Management</h3>
                            <ul>
                            <li><Link to="/baseConfiguration">Base Configuration</Link></li>
                            <li><Link to="/predicates">Predicates</Link></li>
                            <li><Link to="/layers">Layers</Link></li>
                            <li><Link to="/traits">Traits</Link></li>
                            <li><Link to="/oiacontexts">Online Inbound Layer Contexts</Link></li>
                            <li><Link to="/odataapicontexts">OData API Contexts</Link></li>
                            </ul>

                            <h3>Debug</h3>
                            <ul>
                            <li><Link to="/cache">Cache</Link></li>
                            <li><Link to="/version">Version</Link></li>
                            <li><Link to="/current-user">Current User Data</Link></li>
                            <li><Link to="/logs">Logs</Link></li>
                            </ul>

                            <h3>Plugin Management</h3>
                            <ul>  
                            {
                                frontendPluginsmanagerLoading? "Loading..." :
                                frontendPlugins.map(plugin => {
                                    // only create Link for plugins containing component 'manageComponent'
                                    if (plugin.components.manageComponent)
                                        return <li key={plugin.name}><Link to={"/" + plugin.name}>{plugin.title}</Link></li>;
                                    else
                                        return null;
                                })
                            }
                            </ul>
                        </div>
                    </PrivateRoute>
                </Switch>
            </div>
        </BrowserRouter>
    )
}
