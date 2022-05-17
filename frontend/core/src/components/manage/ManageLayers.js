import React, { useState } from 'react';
import { useQuery, useMutation, useApolloClient } from '@apollo/client';
import { queries } from '../../graphql/queries_manage'
import { mutations } from '../../graphql/mutations_manage'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import AgGridCrud from './AgGridCrud';
import CreateLayer from './CreateLayer';

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
  const [upsertLayer] = useMutation(mutations.UPSERT_LAYERDATA);
  const apolloClient = useApolloClient();

  const columnDefs = [
    { headerName: "ID", field: "id", editable: (params) => params.data.isNew },
    { headerName: "Description", field: "description" },
    { headerName: "Color", field: "color", width: 70, cellEditor: 'ARGBColorCellEditor', cellRenderer: 'layerColorCellRenderer' },
    { headerName: "Compute Layer Config ID", field: "clConfigID" },
    { headerName: "Online Inbound Adapter", field: "onlineInboundAdapterName" },
    { headerName: "Generators", field: "generators", 
      valueFormatter: (params) => params.value.join(','),
      valueParser: (params) => {
          const a = (Array.isArray(params.newValue)) ? params.newValue : params.newValue.split(','); 
          return a.filter(e => e);
        },
    },
    { headerName: "State", field: "state", width: 80, cellEditor: 'agSelectCellEditor', cellEditorParams: {
        values: ['ACTIVE', 'DEPRECATED', 'INACTIVE', 'MARKED_FOR_DELETION'],
      },
    },
    { headerName: "Statistics & Operations", cellRenderer: 'linkCellRenderer', editable: false,
      cellRendererParams: { link: (props) => `layers/operations/${props.node.id}`, content: (props) => 'Statistics & Operations' }
    }
  ];

  return <>
    <h2>Layers</h2>

    <CreateLayer isEditable={true} onAfterCreation={refetch} />

    <AgGridCrud idIsUserCreated={true} rowData={rowData} setRowData={setRowData} loading={loading} 
      columnDefs={columnDefs} onRefresh={refetch} disableAddRow={true}
      onGridReady={(params) => {
          var defaultSortModel = [ {colId: "id", sort: "asc"} ];
          params.api.setSortModel(defaultSortModel);
      }}
      saveRow={async row => {
          return upsertLayer({variables: { layer: { id: row.id, description: row.description, state: row.state, clConfigID: row.clConfigID, onlineInboundAdapterName: row.onlineInboundAdapterName, color: row.color, generators: row.generators ?? [] }}})
            .then(r => { apolloClient.resetStore(); return r; })
            .then(r => ({result: r.data.manage_upsertLayerData, id: row.id}))
            .catch(e => ({result: e, id: row.id }));
      }}
    />
  </>;
}
