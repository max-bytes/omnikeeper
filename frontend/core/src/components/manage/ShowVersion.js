import React from 'react';
import { Link  } from 'react-router-dom'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faChevronLeft } from '@fortawesome/free-solid-svg-icons';
import { useQuery } from '@apollo/client';
import { queries } from '../../graphql/queries'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';

export default function ShowVersion(props) {

  const { data } = useQuery(queries.Version);

  if (!data) return "Loading";

  return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
    <h2>Version</h2>
    <div style={{marginBottom: '10px'}}><Link to="/manage"><FontAwesomeIcon icon={faChevronLeft} /> Back</Link></div>
    <div>
      Omnikeeper Core: {data.version.coreVersion ?? 'unknown'}<br />
      Technical Frontend: {process.env.REACT_APP_VERSION ?? 'unknown'}<br />
      Loaded Backend-Plugins:
    </div>
      <ul>
      {data.version.loadedPlugins.map(lp => {
        return <li key={lp.name}>{lp.name}: {lp.informationalVersion}</li>;
      })}
      </ul>
      Loaded Frontend-Plugins:
      <ul>
      {props.availableFrontenedPlugins?.map(plugin => {
        return <li key={plugin.pluginName}>{plugin.pluginName}: {plugin.pluginVersion}</li>;
      })}
      </ul>
  </div>
}
