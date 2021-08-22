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

export default function ManagePredicates(props) {
  var [rowData, setRowData] = useState([]);

  // TODO: is the ManagePredicates interface not nothing else than a specific gridview?
  // Consider re-use!

  const { loading, refetch } = useQuery(queries.Predicates, { 
    variables: {},
    notifyOnNetworkStatusChange: true,
    onCompleted: (data) => {
      const preparedData = data.manage_predicates;
      setRowData(preparedData);
    },
    onError: (e) => {
      console.log("error"); // TODO
      console.log(e);
    }
  });
  const [upsert] = useMutation(mutations.UPSERT_PREDICATE);
  const [remove] = useMutation(mutations.REMOVE_PREDICATE);
  
  const apolloClient = useApolloClient();

  const columnDefs = [
    { headerName: "ID", field: "id", editable: (params) => params.data.isNew },
    { headerName: "Wording (from)", field: "wordingFrom" },
    { headerName: "Wording (to)", field: "wordingTo" },
  ];

  return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
    <h2>Predicates</h2>
    <div style={{marginBottom: '10px'}}><Link to="."><FontAwesomeIcon icon={faChevronLeft} /> Back</Link></div>

    <AgGridCrud idIsUserCreated={true} rowData={rowData} setRowData={setRowData} loading={loading} columnDefs={columnDefs} onRefresh={refetch} 
      saveRow={async row => {
        const predicate = { id: row.id, wordingFrom: row.wordingFrom, wordingTo: row.wordingTo };
        return upsert({ variables: { predicate: predicate } })
          .then(r => { apolloClient.resetStore(); return r; })
          .then(r => ({result: r.data.manage_upsertPredicate, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
      }}
      deletableRows={true}
      deleteRow={async row => {
        if (row.id === undefined) {
          // TODO
        } else {
          return remove({variables: {predicateID: row.id}})
          .then(r => ({result: r.manage_removePredicate, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
        }
      }} />
  </div>;
}
