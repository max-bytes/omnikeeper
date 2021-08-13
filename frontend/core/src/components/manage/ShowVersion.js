import React from 'react';
import { Link  } from 'react-router-dom'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faChevronLeft } from '@fortawesome/free-solid-svg-icons';
import { useQuery } from '@apollo/client';
import { queries } from '../../graphql/queries_manage'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import useFrontendPluginsManager from "utils/useFrontendPluginsManager";
import env from "@beam-australia/react-env";

export default function ShowVersion(props) {

  const { data } = useQuery(queries.Version);

  const { data: frontendPluginsManager, error: frontendPluginsmanagerError } = useFrontendPluginsManager();
  const frontendPlugins = frontendPluginsManager?.getAllFrontendPlugins();

  if (frontendPluginsmanagerError) return "Error:" + frontendPluginsmanagerError;
  if (!data) return "Loading...";

  return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
    <h2>Version</h2>
    <div style={{marginBottom: '10px'}}><Link to="."><FontAwesomeIcon icon={faChevronLeft} /> Back</Link></div>
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
            return <li key={plugin.name}>{plugin.name}: {plugin.version}</li>;
        })
      }
      </ul>
  </div>
}
