// TODO:
// For now this function only exports an Object with all frontendPlugins with { pluginName, pluginVersion, PluginComponent }
// Functionality will increase with future Use Cases.

export default function getFrontendPlugins(props) {
    const swaggerClient = props.swaggerClient;

    // HACK: "It is not possible to use a fully dynamic import statement, such as import(foo).
    // Because foo could potentially be any path to any file in your system or project."
    // https://webpack.js.org/api/module-methods/#dynamic-expressions-in-import
    // TODO: find a solution

    const frontendPlugins = (() => {
        const frontendPluginsStringArray = process.env.REACT_APP_PLUGINS_FRONTEND.split(" ");

        return frontendPluginsStringArray.flatMap(s => {
            try{
                // parse process.env.REACT_APP_PLUGINS_FRONTEND
                const pluginName = s.split("@")[0];
                const wantedPluginVersion = s.split("@")[1];

                let plugin;
                switch (pluginName) {
                    case "okplugin-plugintest1":
                        // plugin = require("local_plugins_for_dev/okplugin-plugintest1"); // FOR DEVELOPMENT ONLY !! // TODO: don't use in prod!
                        plugin = require("okplugin-plugintest1");
                        break;
                    default:
                        throw new Error("Cannot find module '" + pluginName + "'"); // All available frontend-plugins should be listed in this switch. If not, throw error.
                }

                const pluginVersion = plugin.version;

                // create props
                const pluginProps={
                    wantedPluginVersion: wantedPluginVersion,
                    swaggerClient: swaggerClient,
                }
                const PluginComponent = plugin.default(pluginProps); // thows and error, if plugin doesn't have a default()

                return {
                    pluginName: pluginName,
                    pluginVersion: pluginVersion,
                    PluginComponent: PluginComponent
                }

            } catch(e) {
                console.error(e);
                return [];
            }
        });
    })();

    return frontendPlugins;
}
