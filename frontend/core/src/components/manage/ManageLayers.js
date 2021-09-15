import React, { useState } from 'react';
import { useQuery, useMutation, useApolloClient } from '@apollo/client';
import { queries } from '../../graphql/queries_manage'
import { mutations } from '../../graphql/mutations_manage'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import AgGridCrud from './AgGridCrud';

export default function ManageLayers(props) {
  var [rowData, setRowData] = useState([]);
  
  const { loading, refetch } = useQuery(queries.Layers, { 
    notifyOnNetworkStatusChange: true,
    onCompleted: (data) => {
      setRowData(data.manage_layers);
    },
    onError: (e) => {
      console.log("error"); // TODO
      console.log(e);
    }
  });
  const [upsertLayer] = useMutation(mutations.UPSERT_LAYER);
  const apolloClient = useApolloClient();

  const columnDefs = [
    { headerName: "ID", field: "id", editable: (params) => params.data.isNew },
    { headerName: "Description", field: "description", editable: (params) => params.data.isNew },
    { headerName: "Color", field: "color", width: 70, cellEditor: 'ARGBColorCellEditor', cellRenderer: 'layerColorCellRenderer' },
    { headerName: "Compute Layer Brain", field: "brainName" },
    { headerName: "Online Inbound Adapter", field: "onlineInboundAdapterName" },
    { headerName: "State", field: "state", cellEditor: 'agSelectCellEditor', cellEditorParams: {
        values: ['ACTIVE', 'DEPRECATED', 'INACTIVE', 'MARKED_FOR_DELETION'],
      },
    },
    { headerName: "Statistics & Operations", cellRenderer: 'linkCellRenderer', editable: false,
      cellRendererParams: { link: (props) => `layers/operations/${props.node.id}`, content: (props) => 'Statistics & Operations' }
    }
  ];

  return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
    <h2>Layers</h2>

    <AgGridCrud idIsUserCreated={true} rowData={rowData} setRowData={setRowData} loading={loading} columnDefs={columnDefs} onRefresh={refetch} 
      saveRow={async row => {
          return upsertLayer({variables: { layer: { id: row.id, description: row.description, state: row.state, brainName: row.brainName, onlineInboundAdapterName: row.onlineInboundAdapterName, color: row.color }}})
            .then(r => { apolloClient.resetStore(); return r; })
            .then(r => ({result: r.data.manage_upsertLayer, id: row.id}))
            .catch(e => ({result: e, id: row.id }));
      }} />
  </div>;
}
