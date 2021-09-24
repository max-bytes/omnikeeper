import React, { useState } from 'react';
import { useQuery, useMutation, useApolloClient } from '@apollo/client';
import { queries } from '../../graphql/queries_manage'
import { mutations } from '../../graphql/mutations_manage'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import AgGridCrud from './AgGridCrud';

export default function ManageGenerators() {
  var [rowData, setRowData] = useState([]);

  // TODO: is the ManageGenerators interface not nothing else than a specific gridview?
  // Consider re-use!

  const { loading, refetch } = useQuery(queries.Generators, { 
    notifyOnNetworkStatusChange: true,
    onCompleted: (data) => {

      const finalData = data.manage_generators;
      setRowData(finalData);
    },
    onError: (e) => {
      console.log("error"); // TODO
      console.log(e);
    }
  });
  const [upsert] = useMutation(mutations.UPSERT_GENERATOR);
  const [remove] = useMutation(mutations.REMOVE_GENERATOR);
  
  const apolloClient = useApolloClient();

  const columnDefs = [
    { headerName: "ID", field: "id", editable: (params) => params.data.isNew },
    { headerName: "Attribute Name", field: "attributeName" },
    { headerName: "Attribute Value Template", width: 400, field: "attributeValueTemplate", cellEditor: "agLargeTextCellEditor" },
  ];

  return <>
    <h2>Generators</h2>

    <AgGridCrud idIsUserCreated={true} rowData={rowData} setRowData={setRowData} loading={loading} columnDefs={columnDefs} onRefresh={refetch} 
      saveRow={async row => {
        const generator = {
          id: row.id, 
          attributeName: row.attributeName,
          attributeValueTemplate: row.attributeValueTemplate,
        };
        return upsert({ variables: { generator: generator } })
          .then(r => { apolloClient.resetStore(); return r; })
          .then(r => ({result: r.data.manage_upsertGenerator, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
      }}
      deletableRows={true}
      deleteRow={async row => {
        if (row.id === undefined) {
          // TODO
        } else {
          return remove({variables: {id: row.id}})
          .then(r => ({result: r.removeGenerator, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
        }
      }} />
  </>;
}
