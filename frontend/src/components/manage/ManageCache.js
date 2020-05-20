import React from 'react';
import { Link  } from 'react-router-dom'
import { Icon } from 'semantic-ui-react';
import { useQuery } from '@apollo/client';
import { queries } from '../../graphql/queries'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';

export default function ManageCache() {

  const { data } = useQuery(queries.CacheKeys);

  if (!data) return "Loading";

  var sortedKeys = [...data.cacheKeys];
  sortedKeys.sort();

  return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
    <h2>Memory Cache</h2>
    <div style={{marginBottom: '10px'}}><Link to="/manage"><Icon name="angle left" fitted /> Back</Link></div>
    <p>TODO: make editable / manageable</p>
    <ul>
      {sortedKeys.map(k => (<li>{k}</li>))}
    </ul>
    
  </div>;
}
