import React from 'react';
import { Link  } from 'react-router-dom'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faChevronLeft } from '@fortawesome/free-solid-svg-icons';import { useQuery } from '@apollo/client';
import { queries } from '../../graphql/queries_manage'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';

export default function ManageCache() {

  const { data } = useQuery(queries.CacheKeys);

  if (!data) return "Loading";

  var sortedKeys = [...data.manage_cacheKeys];
  sortedKeys.sort();

  return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
    <h2>Memory Cache</h2>
    <div style={{marginBottom: '10px'}}><Link to="."><FontAwesomeIcon icon={faChevronLeft} /> Back</Link></div>
    <p>TODO: make editable / manageable</p>
    <ul>
      {sortedKeys.map(k => (<li key={k}>{k}</li>))}
    </ul>
    
  </div>;
}
