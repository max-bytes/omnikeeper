import React, { useState } from 'react';
import { useQuery, useMutation } from '@apollo/client';
import { queries } from '../../graphql/queries_manage'
import { mutations } from '../../graphql/mutations_manage'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import AgGridCrud from './AgGridCrud';

export default function ManageAuthRoles(props) {
  var [rowData, setRowData] = useState([]);
  
  const { loading, refetch } = useQuery(queries.AuthRoles, { 
    notifyOnNetworkStatusChange: true,
    onCompleted: (data) => {
      setRowData(data.manage_authRoles);
    },
    onError: (e) => {
      console.log("error"); // TODO
      console.log(e);
    }
  });
  const [upsert] = useMutation(mutations.UPSERT_AUTH_ROLE);
  const [remove] = useMutation(mutations.REMOVE_AUTH_ROLE);

  const columnDefs = [
    { headerName: "ID", field: "id", editable: (params) => params.data.isNew },
    { headerName: "Permissions", field: "permissions", cellEditor: 'authRolePermissionsCellEditor', flex: 1,
      cellRenderer: (params) => {
        if (params.value)
          return params.value.join(', ');
        return '';
      } 
    }
  ];

  return <>
    <h2>Auth Roles</h2>

    <AgGridCrud idIsUserCreated={true} rowData={rowData} setRowData={setRowData} loading={loading} columnDefs={columnDefs} onRefresh={refetch} 
      saveRow={async row => {
        const authRole = {
          id: row.id, 
          permissions: row.permissions,
        };
        return upsert({ variables: { authRole: authRole } })
          .then(r => ({result: r.data.manage_upsertAuthRole, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
      }}
      deletableRows={true}
      deleteRow={async row => {
        if (row.id === undefined) {
          // TODO
        } else {
          return remove({variables: {id: row.id}})
          .then(r => ({result: r.manage_removeAuthRole, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
        }
      }} />
  </>;
}
