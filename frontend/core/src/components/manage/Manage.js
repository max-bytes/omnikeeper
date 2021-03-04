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

export default function Manage(props) {
    const swaggerClient = props.swaggerClient;

    // ########## FRONTEND-PLUGINS CODE #########
    // TODO: outsource

    // HACK: "It is not possible to use a fully dynamic import statement, such as import(foo).
    // Because foo could potentially be any path to any file in your system or project."
    // https://webpack.js.org/api/module-methods/#dynamic-expressions-in-import
    // TODO: find a solution

    const availableFrontenedPlugins = [];
  
    const frontendPlugins = (() => {
        const frontendPluginsStringArray = process.env.REACT_APP_PLUGINS_FRONTEND.split(" ");

        return frontendPluginsStringArray.map(s => {
            try{
                // parse process.env.REACT_APP_PLUGINS_FRONTEND
                const pluginName = s.split("@")[0];
                const wantedPluginVersion = s.split("@")[1];

                let plugin;
                switch (pluginName) {
                    case "okplugin-plugintest1":
                        // plugin = require("./local_plugins_for_dev/okplugin-plugintest1"); // FOR DEVELOPMENT ONLY !! // TODO: don't use in prod!
                        plugin = require("okplugin-plugintest1");
                        break;
                    default:
                        return null;
                }

                const pluginVersion = plugin.version;
                availableFrontenedPlugins.push({pluginName: pluginName, pluginVersion: pluginVersion}); // add to availableFrontenedPlugins

                // create props
                const pluginProps={
                    wantedPluginVersion: wantedPluginVersion,
                    swaggerClient: swaggerClient,
                }
                const PluginComponent = plugin.default(pluginProps); // thows and error, if plugin doesn't have a default()

                return (
                <PrivateRoute path={"/" + pluginName} key={pluginName}>
                    <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
                    <h2>{pluginName}</h2>
                    <div style={{marginBottom: '10px'}}><Link to=""><FontAwesomeIcon icon={faChevronLeft} /> Back</Link></div>
                    <PluginComponent/>
                    </div>
                </PrivateRoute>
                );

            } catch(e) {
                return null;
            }
        });
    })();

    // #######################################

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
                        <ShowVersion availableFrontenedPlugins={availableFrontenedPlugins} />
                    </PrivateRoute>
                    <PrivateRoute path="/logs">
                        <ShowLogs />
                    </PrivateRoute>
                    {frontendPlugins}
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
                                availableFrontenedPlugins?.map(plugin => {
                                    return <li key={plugin.pluginName}><Link to={"/" + plugin.pluginName}>{plugin.pluginName}</Link></li>;
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
