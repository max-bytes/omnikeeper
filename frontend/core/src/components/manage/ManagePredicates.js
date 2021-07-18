import React, { useState } from 'react';
import { Link  } from 'react-router-dom'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faChevronLeft } from '@fortawesome/free-solid-svg-icons';
import { useQuery, useMutation, useApolloClient } from '@apollo/client';
import { queries } from '../../graphql/queries'
import { mutations } from '../../graphql/mutations'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import AgGridCrud from './AgGridCrud';
import _ from 'lodash';

export default function ManagePredicates(props) {
  var [rowData, setRowData] = useState([]);

  // TODO: is the ManagePredicates interface not nothing else than a specific gridview?
  // Consider re-use!

  // HACK: we need to manually remove __typename properties from the constraints because it causes errors
  // when sending the data back via a mutation
  // see https://github.com/apollographql/apollo-feature-requests/issues/6 why
  const removeTypename = (predicate) => ({...predicate, constraints: _.omit(predicate.constraints, ['__typename'])});

  const { loading, refetch } = useQuery(queries.PredicateList, { 
    variables: {},
    notifyOnNetworkStatusChange: true,
    onCompleted: (data) => {
      const preparedData = _.map(data.predicates, (p) => removeTypename(p));
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
    { headerName: "Constraints", field: "constraints", autoHeight: true, flex: 1,
      cellRenderer: function(params) {
        const value = params.getValue();

        return `From Traits: [${value?.preferredTraitsFrom.join(',') ?? ''}],<br />To Traits: [${value?.preferredTraitsTo.join(',') ?? ''}]`;  
      },
      cellEditor: 'predicateConstraintsCellEditor' },
  ];

  return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
    <h2>Predicates</h2>
    <div style={{marginBottom: '10px'}}><Link to="."><FontAwesomeIcon icon={faChevronLeft} /> Back</Link></div>

    <AgGridCrud idIsUserCreated={true} rowData={rowData} setRowData={setRowData} loading={loading} columnDefs={columnDefs} onRefresh={refetch} 
      saveRow={async row => {
        const constraints = row.constraints ?? { preferredTraitsFrom: [], preferredTraitsTo: [] };
        const predicate = { id: row.id, wordingFrom: row.wordingFrom, wordingTo: row.wordingTo, constraints: constraints };
        return upsert({ variables: { predicate: predicate } })
          .then(r => ({result: removeTypename(r.data.upsertPredicate), id: row.id}))
          .then(r => apolloClient.resetStore())
          .catch(e => ({result: e, id: row.id }));
      }}
      deletableRows={true}
      deleteRow={async row => {
        if (row.id === undefined) {
          // TODO
        } else {
          return remove({variables: {predicateID: row.id}})
          .then(r => ({result: r.removePredicate, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
        }
      }} />
  </div>;
}
