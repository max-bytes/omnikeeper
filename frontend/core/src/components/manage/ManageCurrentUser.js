import React from 'react';
import { useQuery } from '@apollo/client';
import { queries } from '../../graphql/queries_manage'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';

export default function ManageCurrentUser() {

  const { data } = useQuery(queries.DebugCurrentUser);

  if (!data) return "Loading";

  return <>
    <h2>Current User</h2>
    <h3>Debug-Infos</h3>
    <ul>
      {data.manage_debugCurrentUser.map(k => (<li key={k}>{k}</li>))}
    </ul>
  </>;
}
