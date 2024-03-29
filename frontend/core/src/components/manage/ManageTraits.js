import React, { useState } from 'react';
import { useQuery, useMutation, useApolloClient } from '@apollo/client';
import { queries } from '../../graphql/queries_manage'
import { mutations } from '../../graphql/mutations_manage'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import AgGridCrud from './AgGridCrud';

export default function ManageTraits() {
  var [rowData, setRowData] = useState([]);

  // TODO: is the ManageTraits interface not nothing else than a specific gridview?
  // Consider re-use!

  const { loading, refetch } = useQuery(queries.RecursiveTraits, { 
    variables: {},
    notifyOnNetworkStatusChange: true,
    onCompleted: (data) => {

      const finalData = data.manage_recursiveTraits.map(rt => {
        return {
          id: rt.id,
          requiredAttributes: JSON.stringify(rt.requiredAttributes),
          optionalAttributes: JSON.stringify(rt.optionalAttributes),
          optionalRelations: JSON.stringify(rt.optionalRelations),
          requiredTraits: JSON.stringify(rt.requiredTraits),
        };
      })
      setRowData(finalData);
    },
    onError: (e) => {
      console.log("error"); // TODO
      console.log(e);
    }
  });
  const [upsert] = useMutation(mutations.UPSERT_RECURSIVE_TRAIT);
  const [remove] = useMutation(mutations.REMOVE_RECURSIVE_TRAIT);
  
  const apolloClient = useApolloClient();

  const columnDefs = [
    { headerName: "ID", field: "id", editable: (params) => params.data.isNew, sort: "asc" },
    { headerName: "Required Attributes", field: "requiredAttributes", flex: 1 },
    { headerName: "Optional Attributes", field: "optionalAttributes", flex: 1 },
    { headerName: "Optional Relations", field: "optionalRelations", flex: 1 },
    { headerName: "Required Traits", field: "requiredTraits", flex: 1 },
  ];

  return <>
    <h2>Traits</h2>

    <AgGridCrud idIsUserCreated={true} rowData={rowData} setRowData={setRowData} loading={loading} columnDefs={columnDefs} onRefresh={refetch} 
      saveRow={async row => {
        const trait = {
          id: row.id, 
          requiredAttributes: JSON.parse(row.requiredAttributes),
          optionalAttributes: JSON.parse(row.optionalAttributes),
          optionalRelations: JSON.parse(row.optionalRelations),
          requiredTraits: JSON.parse(row.requiredTraits),
        };
        return upsert({ variables: { trait: trait } })
          .then(r => { apolloClient.resetStore(); return r; })
          .then(r => ({result: r.data.manage_upsertRecursiveTrait, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
      }}
      deletableRows={true}
      deleteRow={async row => {
        if (row.id === undefined) {
          // TODO
        } else {
          return remove({variables: {id: row.id}})
          .then(r => ({result: r.removeRecursiveTrait, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
        }
      }} />
  </>;
}
