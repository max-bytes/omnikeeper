import useSwaggerClient from "utils/useSwaggerClient";

export default function useFrontendPluginsManager() {
    const { data: swaggerClient, loading: swaggerClientLoading, error: swaggerClientError } = useSwaggerClient();

    try {
        // loads all frontend plugins, specified in 'REACT_APP_PLUGINS_FRONTEND' and creates an array with plugin-Objects
        // plugin-Objects consists of different attributes, like name, title or the component itself
        const frontendPluginsStringArray = process.env.REACT_APP_PLUGINS_FRONTEND.split(" ");
        const allFrontendPlugins = frontendPluginsStringArray.flatMap(s => {
            // parse process.env.REACT_APP_PLUGINS_FRONTEND
            const wantedPluginName = s.split("@")[0];
            const wantedPluginVersion = s.split("@")[1];

            // HACK: "It is not possible to use a fully dynamic import statement, such as import(foo).
            // Because foo could potentially be any path to any file in your system or project."
            // https://webpack.js.org/api/module-methods/#dynamic-expressions-in-import
            // WORKAROUND: use this ugly hardcoded switch-statement
            // TODO: find a solution
            let plugin;
            switch (wantedPluginName) {
                // Try to 'require' frontend-plugins. -> No error-handling needed, if 'require' fails: It's okay, that some plugins cannot be found.
                case "okplugin-generic-json-ingest":
                    // try { plugin = require("local_plugins_for_dev/okplugin-generic-json-ingest"); } catch(e) { return []; } // FOR DEVELOPMENT ONLY !! // TODO: don't use in prod!
                    try { plugin = require("okplugin-generic-json-ingest"); } catch(e) { return []; }
                    break;
                case "okplugin-grid-view":
                    // try { plugin = require("local_plugins_for_dev/okplugin-grid-view"); } catch(e) { return []; } // FOR DEVELOPMENT ONLY !! // TODO: don't use in prod!
                    try { plugin = require("okplugin-grid-view"); } catch(e) { return []; }
                    break;
                default:
                    throw new Error("Cannot find module '" + wantedPluginName + "'"); // All available frontend-plugins should be listed in this switch. If not, throw error.
            }

            // create props
            const pluginProps={
                wantedPluginVersion: wantedPluginVersion,
                swaggerClient: swaggerClient,
            }
            const PluginComponents = plugin.default(pluginProps); // thows and error, if plugin doesn't have a default export

            return {
                name: plugin.name,
                title: plugin.title,
                path: plugin.path,
                icon: plugin.icon ? plugin.icon : "", // only frontend-plugins with a 'MainMenuComponent' must export 'icon'
                version: plugin.version,
                description: plugin.description,
                components: PluginComponents
            }
        });

        const pluginObjects = {
            // returns an Array with all plugin-Objects
            getAllFrontendPlugins() {
                return allFrontendPlugins;
            },
        }

        // 'loading' and 'error' get passed through from useSwaggerClient()
        return { data: pluginObjects, loading: swaggerClientLoading, error: swaggerClientError };

    } catch(e) {
        return { data: null, loading: false, error: e };
    }
}