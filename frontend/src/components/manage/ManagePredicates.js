import React, { useState } from 'react';
import { Link  } from 'react-router-dom'
import { Icon } from 'semantic-ui-react';
import { useQuery, useMutation } from '@apollo/client';
import { queries } from '../../graphql/queries'
import { mutations } from '../../graphql/mutations'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import AgGridCrud from './AgGridCrud';

export default function ManagePredicates(props) {
  var [rowData, setRowData] = useState([]);
  const { loading, refetch } = useQuery(queries.PredicateList, { 
    variables: {stateFilter: 'all'},
    notifyOnNetworkStatusChange: true,
    onCompleted: (data) => {
      setRowData(data.predicates);
    },
    onError: (e) => {
      console.log("error"); // TODO
      console.log(e);
    }
  });
  const [upsert] = useMutation(mutations.UPSERT_PREDICATE);

  const columnDefs = [
    { headerName: "ID", field: "id", editable: (params) => params.data.isNew },
    { headerName: "Wording (from)", field: "wordingFrom" },
    { headerName: "Wording (to)", field: "wordingTo" },
    { headerName: "Constraints", field: "constraints", autoHeight: true, flex: 1,
      cellRenderer: function(params) { 
        return `From Traits: [${params.getValue().preferredTraitsFrom.join(',')}],<br />To Traits: [${params.getValue().preferredTraitsTo.join(',')}]`; 
      },
      cellEditor: 'predicateConstraintsCellEditor' },
    { headerName: "State", field: "state", cellEditor: 'agSelectCellEditor', cellEditorParams: {
        values: ['ACTIVE', 'DEPRECATED', 'INACTIVE', 'MARKED_FOR_DELETION'],
      },
    }
  ];

  return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
    <h2>Predicates</h2>
    <div style={{marginBottom: '10px'}}><Link to="/manage"><Icon name="angle left" fitted /> Back</Link></div>

    <AgGridCrud idIsUserCreated={true} rowData={rowData} setRowData={setRowData} loading={loading} columnDefs={columnDefs} onRefresh={refetch} 
      saveRow={async row => {
        const predicate = { id: row.id, wordingFrom: row.wordingFrom, wordingTo: row.wordingTo, state: row.state, constraints: row.constraints };
        return upsert({ variables: { predicate: predicate } })
          .then(r => ({result: r.data.upsertPredicate, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
      }} />
  </div>;
}
