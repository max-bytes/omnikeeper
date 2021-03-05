// TODO:
// For now this function only exports an array frontendPlugins
// Functionality will increase with future Use Cases.

export default function getFrontendPlugins(swaggerClient) {
    // HACK: "It is not possible to use a fully dynamic import statement, such as import(foo).
    // Because foo could potentially be any path to any file in your system or project."
    // https://webpack.js.org/api/module-methods/#dynamic-expressions-in-import
    // TODO: find a solution

    const frontendPlugins = (() => {
        const frontendPluginsStringArray = process.env.REACT_APP_PLUGINS_FRONTEND.split(" ");

        return frontendPluginsStringArray.flatMap(s => {
            try{
                // parse process.env.REACT_APP_PLUGINS_FRONTEND
                const wantedPluginName = s.split("@")[0];
                const wantedPluginVersion = s.split("@")[1];

                let plugin;
                switch (wantedPluginName) {
                    case "okplugin-generic-json-ingest":
                        // plugin = require("local_plugins_for_dev/okplugin-generic-json-ingest"); // FOR DEVELOPMENT ONLY !! // TODO: don't use in prod!
                        plugin = require("okplugin-generic-json-ingest");
                        break;
                    default:
                        throw new Error("Cannot find module '" + wantedPluginName + "'"); // All available frontend-plugins should be listed in this switch. If not, throw error.
                }

                // create props
                const pluginProps={
                    wantedPluginVersion: wantedPluginVersion,
                    swaggerClient: swaggerClient,
                }
                const PluginComponent = plugin.default(pluginProps); // thows and error, if plugin doesn't have a default()

                return {
                    name: plugin.name,
                    title: plugin.title,
                    version: plugin.version,
                    description: plugin.description,
                    component: PluginComponent
                }

            } catch(e) {
                console.error(e);
                return [];
            }
        });
    })();

    return frontendPlugins;
}
