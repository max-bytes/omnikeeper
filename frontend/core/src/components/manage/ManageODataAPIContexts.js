import React, { useState } from 'react';
import { useQuery, useMutation } from '@apollo/client';
import { queries } from '../../graphql/queries_manage'
import { mutations } from '../../graphql/mutations_manage'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import AgGridCrud from './AgGridCrud';
import _ from 'lodash';

export default function ManageODataAPIContexts(props) {
  var [rowData, setRowData] = useState([]);
  const { loading, refetch } = useQuery(queries.ODataAPIContexts, { 
    notifyOnNetworkStatusChange: true,
    onCompleted: (data) => {
      setRowData(_.map(data.traitEntities.m__meta__config__odata_context.all, e => e.entity));
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

  return <>
    <h2>OData API Contexts</h2>
    <p>OData API url: https://[instance]/backend/api/odata/[context ID]</p>

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
        return upsertODataAPIContext({variables: { odataAPIContext: { id: row.id, config: row.config, name: `OData-Context - ${row.id}` }}})
          .then(r => ({result: r.data.upsertByDataID_m__meta__config__odata_context.entity, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
      }} />
  </>;
}
