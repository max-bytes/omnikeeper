import useSwaggerClient from "utils/useSwaggerClient";
import env from "@beam-australia/react-env";

export default function useFrontendPluginsManager() {
    const { data: swaggerClient, loading: swaggerClientLoading, error: swaggerClientError } = useSwaggerClient();

    try {
        // loads all frontend plugins, specified in 'REACT_APP_PLUGINS_FRONTEND' and creates an array with plugin-Objects
        // plugin-Objects consists of different attributes, like name, title or the component itself
        const allFrontendPlugins = (() => {
            const frontendPluginsStringArray = env('PLUGINS_FRONTEND').split(" ");
            return frontendPluginsStringArray.flatMap(s => {
                // parse env variable REACT_APP_PLUGINS_FRONTEND
                const split = s.match(/(.*)@(.*)/);
                if (split.length !== 3) {
                    console.log("Cannot parse frontend plugin slug \"" + s + "\"");
                    return [];
                }
                const wantedPluginName = split[1];
                const wantedPluginVersion = split[2];

                // HACK: "It is not possible to use a fully dynamic import statement, such as import(foo).
                // Because foo could potentially be any path to any file in your system or project."
                // https://webpack.js.org/api/module-methods/#dynamic-expressions-in-import
                // WORKAROUND: use this ugly hardcoded switch-statement
                // TODO: find a solution
                let plugin;
                switch (wantedPluginName) {
                    // Try to 'require' frontend-plugins. -> No error-handling needed, if 'require' fails: It's okay, that some plugins cannot be found.
                    case "@maximiliancsuk/okplugin-generic-json-ingest":
                        // try { plugin = require("local_plugins_for_dev/@maximiliancsuk/okplugin-generic-json-ingest"); } catch(e) { return []; } // FOR DEVELOPMENT ONLY !! // TODO: don't use in prod!
                        try { plugin = require("@maximiliancsuk/okplugin-generic-json-ingest"); } catch(e) { return []; } 
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
                    version: plugin.version,
                    description: plugin.description,
                    components: PluginComponents
                }
            });
        })();

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