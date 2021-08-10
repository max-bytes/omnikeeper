import React, { useState } from 'react';
import { Link  } from 'react-router-dom'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faChevronLeft } from '@fortawesome/free-solid-svg-icons';
import { useQuery, useMutation } from '@apollo/client';
import { queries } from '../../graphql/queries_manage'
import { mutations } from '../../graphql/mutations_manage'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import AgGridCrud from './AgGridCrud';

export default function ManageOIAContexts(props) {
  var [rowData, setRowData] = useState([]);
  const { loading, refetch } = useQuery(queries.OIAContexts, { 
    notifyOnNetworkStatusChange: true,
    onCompleted: (data) => {
      setRowData(data.manage_oiacontexts);
    },
    onError: (e) => {
      console.log("error"); // TODO
      console.log(e);
    }
  });
  const [createOIAContext] = useMutation(mutations.CREATE_OIACONTEXT);
  const [updateOIAContext] = useMutation(mutations.UPDATE_OIACONTEXT);
  const [deleteOIAContext] = useMutation(mutations.DELETE_OIACONTEXT);

  const columnDefs = [
    { headerName: "ID", field: "id", editable: false },
    { headerName: "Name", field: "name" },
    { headerName: "Config", field: "config", autoHeight: true, flex: 1, cellEditor: 'agLargeTextCellEditor', cellClass: 'cell-wrap-text',
      cellEditorParams: { maxLength: 999999, cols: 120 }  }
  ];

  return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
    <h2>Online Inbound Adapter Contexts</h2>
    <div style={{marginBottom: '10px'}}><Link to="."><FontAwesomeIcon icon={faChevronLeft} /> Back</Link></div>

    <AgGridCrud idIsUserCreated={false} rowData={rowData} setRowData={setRowData} loading={loading} columnDefs={columnDefs} onRefresh={refetch} 
      deletableRows={true}
      deleteRow={async row => {
        if (row.id === undefined && row.frontend_id !== undefined) {
          // TODO
        } else {
          return deleteOIAContext({variables: {oiaID: row.id}})
          .then(r => ({result: true, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
        }
      }}
      saveRow={async row => {
        if (row.id === undefined && row.frontend_id !== undefined) {
          return createOIAContext({variables: { oiaContext: { name: row.name, config: row.config }}})
            .then(r => ({result: r.data.manage_createOIAContext, frontend_id: row.frontend_id}))
            .catch(e => ({result: e, frontend_id: row.frontend_id }));
        } else {
          return updateOIAContext({variables: { oiaContext: { id: row.id, name: row.name, config: row.config }}})
            .then(r => ({result: r.data.manage_updateOIAContext, id: row.id}))
            .catch(e => ({result: e, id: row.id }));
        }
      }} />
  </div>;
}
