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

export default function ManageLayers(props) {
  var [rowData, setRowData] = useState([]);
  const { loading, refetch } = useQuery(queries.Layers, { 
    notifyOnNetworkStatusChange: true,
    onCompleted: (data) => {
      setRowData(data.layers);
    },
    onError: (e) => {
      console.log("error"); // TODO
      console.log(e);
    }
  });
  const [createLayer] = useMutation(mutations.CREATE_LAYER);
  const [updateLayer] = useMutation(mutations.UPDATE_LAYER);
  const apolloClient = useApolloClient();

  const columnDefs = [
    { headerName: "ID", field: "id", editable: false },
    { headerName: "Name", field: "name", editable: (params) => params.data.isNew },
    { headerName: "Color", field: "color", width: 70, cellEditor: 'ARGBColorCellEditor', cellRenderer: 'layerColorCellRenderer' },
    { headerName: "Compute Layer Brain", field: "brainName" },
    { headerName: "Online Inbound Adapter", field: "onlineInboundAdapterName" },
    { headerName: "State", field: "state", cellEditor: 'agSelectCellEditor', cellEditorParams: {
        values: ['ACTIVE', 'DEPRECATED', 'INACTIVE', 'MARKED_FOR_DELETION'],
      },
    },
    { headerName: "Operations", cellRenderer: 'linkCellRenderer', editable: false,
      cellRendererParams: { link: (props) => `/layers/operations/${props.node.id}`, content: (props) => 'Operations' }
    }
  ];

  return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
    <h2>Layers</h2>
    <div style={{marginBottom: '10px'}}><Link to="/"><FontAwesomeIcon icon={faChevronLeft} /> Back</Link></div>

    <AgGridCrud idIsUserCreated={false} rowData={rowData} setRowData={setRowData} loading={loading} columnDefs={columnDefs} onRefresh={refetch} 
      saveRow={async row => {
        if (row.id === undefined && row.frontend_id !== undefined) {
          return createLayer({variables: { layer: { name: row.name, state: row.state, brainName: row.brainName, onlineInboundAdapterName: row.onlineInboundAdapterName, color: row.color }}})
            .then(r => ({result: r.data.createLayer, frontend_id: row.frontend_id}))
            .then(r => apolloClient.resetStore())
            .catch(e => ({result: e, frontend_id: row.frontend_id }));
        } else {
          return updateLayer({variables: { layer: { id: row.id, state: row.state, brainName: row.brainName, onlineInboundAdapterName: row.onlineInboundAdapterName, color: row.color }}})
            .then(r => ({result: r.data.updateLayer, id: row.id}))
            .then(r => apolloClient.resetStore())
            .catch(e => ({result: e, id: row.id }));
        }
      }} />
  </div>;
}
