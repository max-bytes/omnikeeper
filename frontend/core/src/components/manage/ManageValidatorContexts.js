import React, { useState } from 'react';
import { useQuery, useMutation } from '@apollo/client';
import { queries } from '../../graphql/queries_manage'
import { mutations } from '../../graphql/mutations_manage'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import AgGridCrud from './AgGridCrud';

export default function ManageValidatorContexts(props) {
  var [rowData, setRowData] = useState([]);
  
  const { loading, refetch } = useQuery(queries.ValidatorContexts, { 
    notifyOnNetworkStatusChange: true,
    onCompleted: (data) => {
      setRowData(data.manage_validatorContexts);
    },
    onError: (e) => {
      console.log("error"); // TODO
      console.log(e);
    }
  });
  const [upsert] = useMutation(mutations.UPSERT_VALIDATOR_CONTEXT);
  const [remove] = useMutation(mutations.REMOVE_VALIDATOR_CONTEXT);

  const columnDefs = [
    { headerName: "ID", field: "id", editable: (params) => params.data.isNew },
    { headerName: "Validator Reference", field: "validatorReference" },
    { headerName: "Config", field: "config", flex: 1, cellEditor: "JSONCellEditor", 
      suppressKeyboardEvent: params => { // disable enter key, so editing is properly possible
        const gridShouldDoNothing = params.editing && (params.event.key === 'Enter');
        return gridShouldDoNothing;
      }
    }
  ];

  return <>
    <h2>Validator Contexts</h2>

    <AgGridCrud idIsUserCreated={true} rowData={rowData} setRowData={setRowData} loading={loading} columnDefs={columnDefs} onRefresh={refetch} 
      onGridReady={(params) => {
        var defaultSortModel = [ {colId: "id", sort: "asc"} ];
        params.api.setSortModel(defaultSortModel);
      }}
      saveRow={async row => {
        const context = {
          id: row.id, 
          validatorReference: row.validatorReference,
          config: row.config
        };
        return upsert({ variables: { context: context } })
          .then(r => ({result: r.data.manage_upsertValidatorContext, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
      }}
      deletableRows={true}
      deleteRow={async row => {
        if (row.id === undefined) {
          // TODO
        } else {
          return remove({variables: {id: row.id}})
          .then(r => ({result: r.manage_removeValidatorContext, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
        }
      }} />
  </>;
}
