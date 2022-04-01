import env from "@beam-australia/react-env";

export default function useFrontendPluginsManager() {
    // loads all frontend plugins, specified in 'REACT_APP_PLUGINS_FRONTEND' and creates an array with plugin-Objects
    // plugin-Objects consists of different attributes, like name, title or the component itself
    const allFrontendPlugins = (() => {
        const frontendPluginsStringArray = env('PLUGINS_FRONTEND').split(" ");
        return frontendPluginsStringArray.flatMap(s => {
            // parse env variable REACT_APP_PLUGINS_FRONTEND
            const split = s.match(/(.*)@(.*)/);
            if (split.length !== 3) {
                console.error("Cannot parse frontend plugin slug \"" + s + "\"");
                return [];
            }
            const wantedPluginName = split[1];
            // const wantedPluginVersion = split[2];

            // HACK: "It is not possible to use a fully dynamic import statement, such as import(foo).
            // Because foo could potentially be any path to any file in your system or project."
            // https://webpack.js.org/api/module-methods/#dynamic-expressions-in-import
            // WORKAROUND: use this ugly hardcoded switch-statement
            // TODO: find a solution
            let plugin;
            switch (wantedPluginName) {
                // Try to 'require' frontend-plugins. -> No error-handling needed, if 'require' fails: It's okay, that some plugins cannot be found.
                case "@max-bytes/okplugin-generic-json-ingest":
                    // try { plugin = require("../../okplugin-generic-json-ingest"); } catch(e) { return []; } // FOR DEVELOPMENT ONLY !! // TODO: don't use in prod!
                    try { 
                        plugin = require("@max-bytes/okplugin-generic-json-ingest"); 
                    } catch(e) {
                        console.error("Could not load plugin @max-bytes/okplugin-generic-json-ingest");
                        return []; 
                    } 
                    break;
                case "@max-bytes/okplugin-visualization":
                    try { 
                        plugin = require("@max-bytes/okplugin-visualization"); 
                    } catch(e) {
                        console.error("Could not load plugin @max-bytes/okplugin-visualization");
                        return []; 
                    } 
                    break;
                default:
                    throw new Error("Cannot find module '" + wantedPluginName + "'"); // All available frontend-plugins should be listed in this switch. If not, throw error.
            }

            const PluginComponents = plugin.default(); // thows and error, if plugin doesn't have a default export

            return {
                name: plugin.name,
                title: plugin.title,
                version: plugin.version,
                description: plugin.description,
                components: PluginComponents
            }
        });
    })();

    return {
        allFrontendPlugins: allFrontendPlugins
    };
}