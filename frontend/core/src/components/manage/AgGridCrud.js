import React, { useState, useMemo } from 'react';
import { AgGridReact } from 'ag-grid-react';
import { Button } from 'antd';
import { ErrorModalCellRenderer } from '../ErrorModalCellRenderer';
import { RowStateCellRenderer } from '../RowStateCellRenderer';
import { LayerColorCellRenderer } from '../LayerColorCellRenderer';
import LinkCellRenderer from '../LinkCellRenderer';
import AuthRolePermissionsCellEditor from './AuthRolePermissionsCellEditor';
import JSONCellEditor from './JSONCellEditor';
import DeleteRowCellRenderer from '../DeleteRowCellRenderer';
import ARGBColorCellEditor from './ARGBColorCellEditor';
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';

export default function AgGridCrud(props) {

  const {rowData, setRowData, deleteRow, saveRow, disableAddRow, setupNewRowData, onRefresh, columnDefs, idIsUserCreated, deletableRows, loading, onGridReady} = props;

  var [isSaving, setIsSaving] = useState(false);

  const editedRowData = useMemo(() => rowData.filter(rd => rd.isEdited), [rowData]);
  const deletedRowData = useMemo(() => rowData.filter(rd => rd.isDeleted), [rowData]);
  const hasEditedOrDeletedRowData = useMemo(() => editedRowData.length > 0 || deletedRowData.length > 0, [editedRowData, deletedRowData]);

  const getRowNodeId = data => data.id;
  const isFrontendRowNodeOnly = data => data.id === undefined && data.frontend_id !== undefined && !idIsUserCreated;

  // TODO: replace with uuidv4 from uuid package
  const uuidv4 = () => ([1e7]+-1e3+-4e3+-8e3+-1e11).replace(/[018]/g, c => (c ^ ((crypto.getRandomValues(new Uint8Array(1))[0] & 15) >> c / 4)).toString(16));

  let deleteColumn = [];
  if (deletableRows)
    deleteColumn = [{ 
      headerName: "", sortable: false, resizable: false, filter: false, colId: 'state', editable: false, valueGetter: 'data', 
        cellRenderer: 'deleteRowCellRenderer', cellRendererParams: { flipDelete: (params) => {
          setRowData(oldData => {
            var foundIndex = oldData.findIndex(x => {
                if (isFrontendRowNodeOnly(params.data))
                    return x.frontend_id === params.data.frontend_id;
                else
                    return getRowNodeId(x) === getRowNodeId(params.data);
            });
            if (foundIndex !== -1) {
              var newData = [...oldData];
              var newItem = {...params.data, isDeleted: !!!params.data.isDeleted };
              newData[foundIndex] = newItem;
              return newData;
            } else return oldData; // TODO: error handling
          }); 
        } }
      }];

  const finalColumnDefs = [
    { headerName: "", sortable: false, resizable: false, filter: false, width: 40, colId: 'state', editable: false, valueGetter: 'data', 
      cellRenderer: 'rowStateCellRenderer'
    },
    { headerName: "", field: "error", sortable: false, resizable: false, filter: false, width: 30, editable: false, 
      cellRenderer: 'errorModalCellRenderer'
    },
    ...columnDefs,
    ...deleteColumn
  ];

  function save() {
    setIsSaving(true);

    Promise.all(
      deletedRowData.map(async row => deleteRow(row))
    )
    .then(results => {
      let indicesToDelete = [];
      setRowData(oldRowData => {
        var newRowData = [...oldRowData];
        results.forEach(result => {
          var index = newRowData.findIndex(p => {
            if (isFrontendRowNodeOnly(result))
                return p.frontend_id === result.frontend_id;
            else
                return getRowNodeId(p) === result.id
          });
          if (index === -1) {
              console.log("Error!"); // TODO
          } else {
              if (result.result instanceof Error) {
                  newRowData[index] = {...newRowData[index], error: result.result};
              } else {
                  indicesToDelete.push(index);
              }
          }
        });

        newRowData = newRowData.filter((v, index) => !indicesToDelete.includes(index));
        return newRowData;
      });
    })
    .then(Promise.all(
        editedRowData.map(async row => saveRow(row))
    ).then(results => {
      setRowData(oldRowData => {
        var newRowData = [...oldRowData];
          results.forEach(result => {
            var index = newRowData.findIndex(p => {
                if (isFrontendRowNodeOnly(result))
                    return p.frontend_id === result.frontend_id;
                else
                    return getRowNodeId(p) === result.id
            });
            if (index === -1) {
                console.log("Error!"); // TODO
            } else {
                if (result.result instanceof Error) {
                    newRowData[index] = {...newRowData[index], error: result.result};
                } else {
                    newRowData[index] = {...result.result, isEdited: false, error: undefined, frontend_id: undefined};
                }
            }
          });
          return newRowData;
      });
    })).finally(() => setIsSaving(false));
  }

  function addRow() {
    let newItem = {isEdited: true, isNew: true, error: undefined};
    if (!idIsUserCreated) {
        newItem = {...newItem, frontend_id: uuidv4() }
    }
    if (setupNewRowData) {
      var d = setupNewRowData();
      newItem = { ...newItem, ...d };
    }
    setRowData(oldRowData => {
      return [...oldRowData, newItem];
    });
  }

  const defaultColDef = {
    sortable: true,
    filter: true,
    editable: true,
    resizable: true,
    valueSetter: function (params) {
      var wasSuccessful = false;

      setRowData(oldData => {
        var foundIndex = oldData.findIndex(x => {
            if (isFrontendRowNodeOnly(params.data))
                return x.frontend_id === params.data.frontend_id;
            else
                return getRowNodeId(x) === getRowNodeId(params.data);
        });
        if (foundIndex !== -1) {
          var newData = [...oldData];
          var newItem = {...params.data, [params.column.colDef.field]: params.newValue, isEdited: true };
          newData[foundIndex] = newItem;
          wasSuccessful = true;
          return newData;
        } else return oldData; // TODO: error handling
      });
      return wasSuccessful;
    },
    cellStyle: function(params) {
      if (params.column.isCellEditable(params.node)) {
        return { color: 'black'};
      } else {
        return { color: '#777777'};
      }
    }
  }

  return <>
    <div className="button-toolbar">
        <div className="button-toolbar-row" style={{ display: "flex", justifyContent: "space-between", marginTop: "10px", marginBottom: "10px" }}>
            <div style={{ display: "flex" }}>
                {!disableAddRow && <Button style={{ display: "flex" }} onClick={e => addRow()}>Add Row</Button>}
            </div>
            <div style={{ display: "flex" }}>
                <Button onClick={e => save()} disabled={!hasEditedOrDeletedRowData} loading={isSaving}>Save</Button>
                <Button onClick={e => onRefresh()} loading={loading}>{((!hasEditedOrDeletedRowData) ? 'Refresh' :  'Reset')}</Button>
            </div>
        </div>
    </div>

    <div className="ag-theme-balham" style={{ flexGrow: 1, width: '100%' }}>
      <AgGridReact
        components={{
          errorModalCellRenderer: ErrorModalCellRenderer, rowStateCellRenderer: RowStateCellRenderer,
          layerColorCellRenderer: LayerColorCellRenderer, deleteRowCellRenderer: DeleteRowCellRenderer,
          linkCellRenderer: LinkCellRenderer,
          authRolePermissionsCellEditor: AuthRolePermissionsCellEditor,
          ARGBColorCellEditor: ARGBColorCellEditor, JSONCellEditor: JSONCellEditor }}
        columnDefs={finalColumnDefs}
        defaultColDef={defaultColDef}
        rowData={rowData}
        enableCellTextSelection={true}
        ensureDomOrder={true}
        getRowId={params => {
            if (isFrontendRowNodeOnly(params.data)) return params.data.frontend_id; else return getRowNodeId(params.data);
        }} 
        onGridReady={onGridReady} />
    </div>
    </>;
}
