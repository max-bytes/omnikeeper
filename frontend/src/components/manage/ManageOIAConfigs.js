import React, { useState } from 'react';
import { Link  } from 'react-router-dom'
import { Icon } from 'semantic-ui-react';
import { useQuery, useMutation } from '@apollo/client';
import { queries } from '../../graphql/queries'
import { mutations } from '../../graphql/mutations'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import AgGridCrud from './AgGridCrud';

export default function ManageOIAConfigs(props) {
  var [rowData, setRowData] = useState([]);
  const { loading, refetch } = useQuery(queries.OIAConfigs, { 
    notifyOnNetworkStatusChange: true,
    onCompleted: (data) => {
      setRowData(data.oiaconfigs);
    },
    onError: (e) => {
      console.log("error"); // TODO
      console.log(e);
    }
  });
  const [createOIAConfig] = useMutation(mutations.CREATE_OIACONFIG);
  const [updateOIAConfig] = useMutation(mutations.UPDATE_OIACONFIG);
  const [deleteOIAConfig] = useMutation(mutations.DELETE_OIACONFIG);

  const columnDefs = [
    { headerName: "ID", field: "id", editable: false },
    { headerName: "Name", field: "name" },
    { headerName: "Config", field: "config", autoHeight: true, flex: 1, cellEditor: 'agLargeTextCellEditor', cellClass: 'cell-wrap-text',
      cellEditorParams: { maxLength: 999999, cols: 120 }  }
  ];

  return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
    <h2>Online Inbound Adapter Configurations</h2>
    <div style={{marginBottom: '10px'}}><Link to="/manage"><Icon name="angle left" fitted /> Back</Link></div>

    <AgGridCrud idIsUserCreated={false} rowData={rowData} setRowData={setRowData} loading={loading} columnDefs={columnDefs} onRefresh={refetch} 
    deletableRows={true}
      deleteRow={async row => {
        if (row.id === undefined && row.frontend_id !== undefined) {
          // TODO
        } else {
          return deleteOIAConfig({variables: {oiaID: row.id}}).catch(e => ({result: e, id: row.id }))
          .then(r => ({result: true, id: row.id}))
          .catch(e => ({result: e, id: row.id }));
        }
      }}
      saveRow={async row => {
        if (row.id === undefined && row.frontend_id !== undefined) {
          return createOIAConfig({variables: { oiaConfig: { name: row.name, config: row.config }}})
            .then(r => ({result: r.data.createOIAConfig, frontend_id: row.frontend_id}))
            .catch(e => ({result: e, frontend_id: row.frontend_id }));
        } else {
          return updateOIAConfig({variables: { oiaConfig: { id: row.id, name: row.name, config: row.config }}})
            .then(r => ({result: r.data.updateOIAConfig, id: row.id}))
            .catch(e => ({result: e, id: row.id }));
        }
      }} />
  </div>;
}
