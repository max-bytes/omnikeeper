import React from 'react';
import { useQuery } from '@apollo/client';
import { queries } from '../../graphql/queries_manage'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import useFrontendPluginsManager from "utils/useFrontendPluginsManager";
import env from "@beam-australia/react-env";

export default function ShowVersion(props) {

  const { data } = useQuery(queries.Version);

  const frontendPluginsManager = useFrontendPluginsManager();
  const frontendPlugins = frontendPluginsManager.allFrontendPlugins;

  if (!data) return "Loading...";

  return <>
    <h2>Version</h2>
    <div>
      Omnikeeper Core: {data.manage_version.coreVersion ?? 'unknown'}<br />
      Technical Frontend: {env('VERSION') ?? 'unknown'}<br />
      Loaded Backend-Plugins:
    </div>
      <ul>
      {data.manage_version.loadedPlugins.map(lp => {
        return <li key={lp.name}>{lp.name}: {lp.informationalVersion}</li>;
      })}
      </ul>
      Loaded Frontend-Plugins:
      <ul>
      {
        frontendPlugins.map(plugin => {
            return <li key={plugin.name}>{plugin.name} ({plugin.version}): {plugin.description}</li>;
        })
      }
      </ul>
  </>
}
