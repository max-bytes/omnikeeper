import React from 'react';
import { Switch, Link, useRouteMatch, useLocation, Redirect } from 'react-router-dom';
import { PrivateRoute } from 'components/PrivateRoute';
import ManagePredicates from 'components/manage/ManagePredicates';
import ManageBaseConfiguration from 'components/manage/ManageBaseConfiguration';
import ManageLayers from 'components/manage/ManageLayers';
import ManageOIAContexts from 'components/manage/ManageOIAContexts';
import ManageODataAPIContexts from 'components/manage/ManageODataAPIContexts';
import ManageTraits from 'components/manage/ManageTraits';
import ManageAuthRoles from 'components/manage/ManageAuthRoles';
import ManageCurrentUser from 'components/manage/ManageCurrentUser';
import ShowLogs from 'components/manage/ShowLogs';
import ShowVersion from 'components/manage/ShowVersion';
import LayerOperations from 'components/manage/LayerOperations';
import useFrontendPluginsManager from "utils/useFrontendPluginsManager";
import ManageGenerators from './ManageGenerators';
import ManageCLConfigs from './ManageCLConfigs';
import ManageRestartApplication from './ManageRestartApplication';
import UsageStats from './UsageStats';

export default function Manage(props) {
    let { path, url } = useRouteMatch();
    const { pathname } = useLocation();

    const frontendPluginsManager = useFrontendPluginsManager();
    const frontendPlugins = frontendPluginsManager.allFrontendPlugins;

    return (
        <Switch>
            <Redirect from="/:url*(/+)" to={pathname.slice(0, -1)} /> {/* Removes trailing slashes */}
            <PrivateRoute path={`${path}/base-configuration`} title="Manage Base Configuration">
                <ManageBaseConfiguration />
            </PrivateRoute>
            <PrivateRoute path={`${path}/predicates`} title="Manage Predicates">
                <ManagePredicates />
            </PrivateRoute>
            <PrivateRoute path={`${path}/layers/operations/:layerID`} title="Layer Operations">
                <LayerOperations />
            </PrivateRoute>
            <PrivateRoute path={`${path}/layers`} title="Manage Layers">
                <ManageLayers />
            </PrivateRoute>
            <PrivateRoute path={`${path}/oiacontexts`} title="Manage OIA Contexts">
                <ManageOIAContexts />
            </PrivateRoute>
            <PrivateRoute path={`${path}/odataapicontexts`} title="Manage OData API Contexts">
                <ManageODataAPIContexts />
            </PrivateRoute>
            <PrivateRoute path={`${path}/traits`} title="Manage Traits">
                <ManageTraits />
            </PrivateRoute>
            <PrivateRoute path={`${path}/generators`} title="Manage Generators">
                <ManageGenerators />
            </PrivateRoute>
            <PrivateRoute path={`${path}/auth-roles`} title="Manage Auth-Roles">
                <ManageAuthRoles />
            </PrivateRoute>
            <PrivateRoute path={`${path}/cl-configs`} title="Manage Compute-Layer Configurations">
                <ManageCLConfigs />
            </PrivateRoute>
            <PrivateRoute path={`${path}/current-user`} title="Debug Current User">
                <ManageCurrentUser />
            </PrivateRoute>
            <PrivateRoute path={`${path}/version`} title="Show Version">
                <ShowVersion/>
            </PrivateRoute>
            <PrivateRoute path={`${path}/logs`} title="Show Logs">
                <ShowLogs />
            </PrivateRoute>
            <PrivateRoute path={`${path}/usage-stats`} title="Show Usage Stats">
                <UsageStats />
            </PrivateRoute>
            <PrivateRoute path={`${path}/restart-application`} title="Restart Application">
                <ManageRestartApplication />
            </PrivateRoute>
            {
                // 'pluginLoading'-PrivateRoute:
                // Shows "Loading..." until frontend-plugin was loaded (if frontend-plugin doesn't exist after loading -> Shows Manage-main-component)
                // Without it, it would show the Manage-main-component until frontend-plugin was loaded - then it would render the frontend-plugin-component (looks linke jumping around, not wanted!)
                frontendPlugins?.map(plugin => {
                    if (!plugin.components.manageComponent) // only create PrivateRoute for plugins containing component 'manageComponent'
                        return null;
                    const Component = plugin.components.manageComponent;
                    return <PrivateRoute path={`${path}/${plugin.name}`} key={plugin.name}>
                        <div style={{ padding: "10px" }}>
                            <h2 style={{ marginBottom: 0 }}>{plugin.title}</h2>
                            <p>{plugin.description}</p>
                        </div>
                        <Component />
                    </PrivateRoute>;
                }
                )
            }

            <PrivateRoute path={path} title="Manage">
                <div><h2>Management</h2>
                    <h3>Core Management</h3>
                    <ul>
                        <li><Link to={`${url}/base-configuration`}>Base Configuration</Link></li>
                        <li><Link to={`${url}/layers`}>Layers</Link></li>
                        <li><Link to={`${url}/oiacontexts`}>Online Inbound Layer Contexts</Link></li>
                        <li><Link to={`${url}/odataapicontexts`}>OData API Contexts</Link></li>
                        <li><Link to={`${url}/restart-application`}>Restart Application</Link></li>
                    </ul>

                    <h3>Data-Config Management</h3>
                    <ul>
                        <li><Link to={`${url}/predicates`}>Predicates</Link></li>
                        <li><Link to={`${url}/traits`}>Traits</Link></li>
                        <li><Link to={`${url}/auth-roles`}>Auth Roles</Link></li>
                        <li><Link to={`${url}/generators`}>Generators</Link></li>
                        <li><Link to={`${url}/cl-configs`}>Compute Layer Configurations</Link></li>
                    </ul>

                    <h3>Debug</h3>
                    <ul>
                        <li><Link to={`${url}/version`}>Version</Link></li>
                        <li><Link to={`${url}/current-user`}>Current User Data</Link></li>
                        <li><Link to={`${url}/logs`}>Logs</Link></li>
                    </ul>

                    <h3>Stats</h3>
                    <ul>
                        <li><Link to={`${url}/usage-stats`}>Usage Stats</Link></li>
                    </ul>

                    <h3>Plugin Management</h3>
                    <ul>
                    {
                        frontendPlugins.map(plugin => {
                            // only create Link for plugins containing component 'manageComponent'
                            if (plugin.components.manageComponent)
                                return <li key={plugin.name}><Link to={`${url}/${plugin.name}`}>{plugin.title}</Link></li>;
                            else
                                return null;
                        })
                    }
                    </ul>
                </div>
            </PrivateRoute>
        </Switch>
    )
}
