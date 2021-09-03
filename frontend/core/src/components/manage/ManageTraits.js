import React, { useState } from 'react';
import { Link  } from 'react-router-dom'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faChevronLeft } from '@fortawesome/free-solid-svg-icons';
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
          requiredAttributes: JSON.stringify(rt.requiredAttributes.map(e => JSON.parse(e))), // TODO
          optionalAttributes: JSON.stringify(rt.optionalAttributes.map(e => JSON.parse(e))), // TODO
          requiredRelations: JSON.stringify(rt.requiredRelations.map(e => JSON.parse(e))), // TODO
          optionalRelations: JSON.stringify(rt.optionalRelations.map(e => JSON.parse(e))), // TODO
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
    { headerName: "ID", field: "id", editable: (params) => params.data.isNew },
    { headerName: "Required Attributes", field: "requiredAttributes" },
    { headerName: "Optional Attributes", field: "optionalAttributes" },
    { headerName: "Required Relations", field: "requiredRelations" },
    { headerName: "Optional Relations", field: "optionalRelations" },
    { headerName: "Required Traits", field: "requiredTraits" },
  ];

  return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
    <h2>Traits</h2>
    <div style={{marginBottom: '10px'}}><Link to="."><FontAwesomeIcon icon={faChevronLeft} /> Back</Link></div>

    <AgGridCrud idIsUserCreated={true} rowData={rowData} setRowData={setRowData} loading={loading} columnDefs={columnDefs} onRefresh={refetch} 
      saveRow={async row => {
        const trait = {
          id: row.id, 
          requiredAttributes: JSON.parse(row.requiredAttributes).map(e => JSON.stringify(e)),
          optionalAttributes: JSON.parse(row.optionalAttributes).map(e => JSON.stringify(e)),
          requiredRelations: JSON.parse(row.requiredRelations).map(e => JSON.stringify(e)),
          optionalRelations: JSON.parse(row.optionalRelations).map(e => JSON.stringify(e)),
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
  </div>;
}
