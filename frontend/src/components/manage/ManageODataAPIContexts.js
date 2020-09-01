import React, { useState } from 'react';
import { Link  } from 'react-router-dom'
import { Icon } from 'semantic-ui-react';
import { useQuery, useMutation } from '@apollo/client';
import { queries } from '../../graphql/queries'
import { mutations } from '../../graphql/mutations'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import AgGridCrud from './AgGridCrud';

export default function ManageODataAPIContexts(props) {
  var [rowData, setRowData] = useState([]);
  const { loading, refetch } = useQuery(queries.ODataAPIContexts, { 
    notifyOnNetworkStatusChange: true,
    onCompleted: (data) => {
      setRowData(data.odataapicontexts);
    },
    onError: (e) => {
      console.log("error"); // TODO
      console.log(e);
    }
  });
  const [upsertODataAPIContext] = useMutation(mutations.UPSERT_ODATAAPICONTEXT);
  const [deleteODataAPIContext] = useMutation(mutations.DELETE_ODATAAPICONTEXT);

  const columnDefs = [
    { headerName: "ID", field: "id", editable: (params) => params.data.isNew },
    { headerName: "Config", field: "config", autoHeight: true, flex: 1, cellEditor: 'agLargeTextCellEditor', cellClass: 'cell-wrap-text',
      cellEditorParams: { maxLength: 999999, cols: 120 }  }
  ];

  return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
    <h2>OData API Contexts</h2>
    <p>OData API url: https://[instance]/backend/api/odata/[context ID]</p>
    <div style={{marginBottom: '10px'}}><Link to="/manage"><Icon name="angle left" fitted /> Back</Link></div>

    <AgGridCrud idIsUserCreated={true} rowData={rowData} setRowData={setRowData} loading={loading} columnDefs={columnDefs} onRefresh={refetch} 
    deletableRows={true}
      deleteRow={async row => {
        if (row.id === undefined) {
          // TODO
        } else {
          return deleteODataAPIContext({variables: {id: row.id}}).catch(e => ({result: e, id: row.id }))
          .then(r => ({result: true, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
        }
      }}
      saveRow={async row => {
        return upsertODataAPIContext({variables: { odataAPIContext: { id: row.id, config: row.config }}})
          .then(r => ({result: r.data.upsertODataAPIContext, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
      }} />
  </div>;
}
