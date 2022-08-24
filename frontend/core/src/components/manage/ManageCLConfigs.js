import React, { useState } from 'react';
import { useQuery, useMutation } from '@apollo/client';
import { queries } from '../../graphql/queries_manage'
import { mutations } from '../../graphql/mutations_manage'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import AgGridCrud from './AgGridCrud';

export default function ManageCLConfigs(props) {
  var [rowData, setRowData] = useState([]);
  
  const { loading, refetch } = useQuery(queries.CLConfigs, { 
    notifyOnNetworkStatusChange: true,
    onCompleted: (data) => {
      setRowData(data.manage_clConfigs);
    },
    onError: (e) => {
      console.log("error"); // TODO
      console.log(e);
    }
  });
  const [upsert] = useMutation(mutations.UPSERT_CL_CONFIG);
  const [remove] = useMutation(mutations.REMOVE_CL_CONFIG);

  const columnDefs = [
    { headerName: "ID", field: "id", editable: (params) => params.data.isNew, sort: "asc" },
    { headerName: "CLBrain Reference", field: "clBrainReference" },
    { headerName: "CLBrain Config", field: "clBrainConfig", flex: 1, cellEditor: "JSONCellEditor", cellEditorPopup: true,
      suppressKeyboardEvent: params => { // disable enter key, so editing is properly possible
        const gridShouldDoNothing = params.editing && (params.event.key === 'Enter');
        return gridShouldDoNothing;
      }
    }
  ];

  return <>
    <h2>Compute Layer Configurations</h2>

    <AgGridCrud idIsUserCreated={true} rowData={rowData} setRowData={setRowData} loading={loading} columnDefs={columnDefs} onRefresh={refetch} 
      saveRow={async row => {
        const config = {
          id: row.id, 
          clBrainReference: row.clBrainReference,
          clBrainConfig: row.clBrainConfig
        };
        return upsert({ variables: { config: config } })
          .then(r => ({result: r.data.manage_upsertCLConfig, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
      }}
      deletableRows={true}
      deleteRow={async row => {
        if (row.id === undefined) {
          // TODO
        } else {
          return remove({variables: {id: row.id}})
          .then(r => ({result: r.manage_removeCLConfig, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
        }
      }} />
  </>;
}
