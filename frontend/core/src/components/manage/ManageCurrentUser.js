import React from 'react';
import { Link  } from 'react-router-dom'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faChevronLeft } from '@fortawesome/free-solid-svg-icons';
import { useQuery } from '@apollo/client';
import { queries } from '../../graphql/queries'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';

export default function ManageCurrentUser() {

  const { data } = useQuery(queries.DebugCurrentUserClaims);

  if (!data) return "Loading";

  return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
    <h2>Current User</h2>
    <div style={{marginBottom: '10px'}}><Link to="."><FontAwesomeIcon icon={faChevronLeft} /> Back</Link></div>
    <h3>DEBUG: Claims</h3>
    <ul>
      {data.debugCurrentUserClaims.map(k => (<li>{k}</li>))}
    </ul>
    
  </div>;
}
