import React from 'react';
import { Switch, Link, useRouteMatch, useLocation, Redirect } from 'react-router-dom';
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
    let { path, url } = useRouteMatch();
    const { pathname } = useLocation();

    const { data: frontendPluginsManager, loading: frontendPluginsmanagerLoading, error: frontendPluginsmanagerError } = useFrontendPluginsManager();
    const frontendPlugins = frontendPluginsManager?.getAllFrontendPlugins();

    if (frontendPluginsmanagerError) return "Error:" + frontendPluginsmanagerError;

    return (
        <div style={{display: 'flex', flexDirection: 'column', height: '100%', paddingTop: "15px"}}>
            <Switch>
                <Redirect from="/:url*(/+)" to={pathname.slice(0, -1)} /> {/* Removes trailing slashes */}
                <PrivateRoute path={`${path}/baseconfiguration`}>
                    <ManageBaseConfiguration />
                </PrivateRoute>
                <PrivateRoute path={`${path}/predicates`}>
                    <ManagePredicates />
                </PrivateRoute>
                <PrivateRoute path={`${path}/layers/operations/:layerID`}>
                    <LayerOperations />
                </PrivateRoute>
                <PrivateRoute path={`${path}/layers`}>
                    <ManageLayers />
                </PrivateRoute>
                <PrivateRoute path={`${path}/oiacontexts`}>
                    <ManageOIAContexts />
                </PrivateRoute>
                <PrivateRoute path={`${path}/odataapicontexts`}>
                    <ManageODataAPIContexts />
                </PrivateRoute>
                <PrivateRoute path={`${path}/traits`}>
                    <ManageTraits />
                </PrivateRoute>
                <PrivateRoute path={`${path}/cache`}>
                    <ManageCache />
                </PrivateRoute>
                <PrivateRoute path={`${path}/current-user`}>
                    <ManageCurrentUser />
                </PrivateRoute>
                <PrivateRoute path={`${path}/version`}>
                    <ShowVersion/>
                </PrivateRoute>
                <PrivateRoute path={`${path}/logs`}>
                    <ShowLogs />
                </PrivateRoute>
                {
                    // 'pluginLoading'-PrivateRoute:
                    // Shows "Loading..." until frontend-plugin was loaded (if frontend-plugin doesn't exist after loading -> Shows Manage-main-component)
                    // Without it, it would show the Manage-main-component until frontend-plugin was loaded - then it would render the frontend-plugin-component (looks linke jumping around, not wanted!)
                    frontendPluginsmanagerLoading? <PrivateRoute path="*" key="pluginLoading">Loading...</PrivateRoute> :
                    frontendPlugins?.map(plugin => (
                            plugin.components.manageComponent && // only create PrivateRoute for plugins containing component 'manageComponent'
                                <PrivateRoute path={`${path}/${plugin.path}`} key={plugin.name}>
                                    <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
                                        <div style={{ padding: "10px" }}>
                                            <h2 style={{ marginBottom: 0 }}>{plugin.title}</h2>
                                            <p>{plugin.description}</p>
                                            <div style={{marginBottom: '10px'}}><Link to={path}><FontAwesomeIcon icon={faChevronLeft} /> Back</Link></div>
                                        </div>
                                        {plugin.components.manageComponent()}
                                    </div>
                                </PrivateRoute>
                        )
                    )
                }

                <PrivateRoute path={path}>
                    <div style={{ padding: '10px' }}><h2>Management</h2>
                        <h3>Core Management</h3>
                        <ul>
                            <li><Link to={`${url}/baseConfiguration`}>Base Configuration</Link></li>
                            <li><Link to={`${url}/predicates`}>Predicates</Link></li>
                            <li><Link to={`${url}/layers`}>Layers</Link></li>
                            <li><Link to={`${url}/traits`}>Traits</Link></li>
                            <li><Link to={`${url}/oiacontexts`}>Online Inbound Layer Contexts</Link></li>
                            <li><Link to={`${url}/odataapicontexts`}>OData API Contexts</Link></li>
                            </ul>

                            <h3>Debug</h3>
                            <ul>
                            <li><Link to={`${url}/cache`}>Cache</Link></li>
                            <li><Link to={`${url}/version`}>Version</Link></li>
                            <li><Link to={`${url}/current-user`}>Current User Data</Link></li>
                            <li><Link to={`${url}/logs`}>Logs</Link></li>
                            </ul>

                            <h3>Plugin Management</h3>
                        <ul>
                        {
                            frontendPluginsmanagerLoading? "Loading..." :
                            frontendPlugins.map(plugin => {
                                // only create Link for plugins containing component 'manageComponent'
                                if (plugin.components.manageComponent)
                                    return <li key={plugin.name}><Link to={`${url}/${plugin.path}`}>{plugin.title}</Link></li>;
                                else
                                    return null;
                            })
                        }
                        </ul>
                    </div>
                </PrivateRoute>
            </Switch>
        </div>
    )
}
