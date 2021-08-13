# Plugins

Plugins enhance the feature set of omnikeeper, adding for example new REST endpoints, online inbound adapters or compute layers.

There are two types of plugins:
-  Plugins for extending the backend/core, written in .Net and distributed as nupkg packages
- Plugins for extending the technical frontend, written in JavaScript and distributed as npm packages

The reason for the split between frontend and backend is mainly technical. It's hard to include and distribute frontend code inside of backend packages and vice versa.
Because of this split, a single omnikeeper enhancement is often spread into two plugins, one for the backend/core, one for the technical frontend.

## Backend plugins

Backend plugins are .Net libraries that are packaged into nupkg files. To load a backend plugin into omnikeeper, the nupkg file must be put into the `okplugins` folder under the application's root folder. During startup, omnikeeper automatically detects the file, unpacks and loads it.

## Frontend plugins
Frontend plugins are simply npm modules. Currently, there is no way to load a frontend plugin during runtime. To load a frontend plugin into omnikeeper, it needs to be added to the frontend project using `npm install [plugin-name]`. This is normally done during the build process (see [[build and packaging|build-and-packaging]]). In addition, all possible frontend plugins have a hard-coded check for them inside the technical frontend code that is required to load them. This is all unfortunate and makes plugin handling difficult for the technical frontend. Improvements that make the process more dynamic and easier are under-way though.

## Checking plugins

To check which plugins are loaded and their versions in a running omnikeeper instance, visit `/manage/version` in the technical frontend.

## Developing plugins

For more detailed insights into how plugins for omnikeeper can be developed, have a look at the code, in particular the already existing plugins, such as the Generic JSON Ingest plugin. Its code for the backend part [[is located here|https://github.com/max-bytes/omnikeeper/tree/master/backend/OKPluginGenericJSONIngest]], it's code for the frontend part [[is located here|https://github.com/max-bytes/omnikeeper/tree/master/frontend/okplugin-generic-json-ingest]]