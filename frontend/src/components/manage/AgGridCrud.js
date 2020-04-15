import React, { useState, useMemo } from 'react';
import { AgGridReact } from 'ag-grid-react';
import { Button } from 'semantic-ui-react';
import { ErrorModalCellRenderer } from '../ErrorModalCellRenderer';
import { RowStateCellRenderer } from '../RowStateCellRenderer';
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';

export default function AgGridCrud(props) {
  var [isSaving, setIsSaving] = useState(false);

  const editedRowData = useMemo(() => props.rowData.filter(rd => rd.isEdited), [props.rowData]);
  const hasEditedRowData = useMemo(() => editedRowData.length > 0, [editedRowData]);

  const getRowNodeId = data => data.id;
  const isFrontendRowNodeOnly = data => data.id === undefined && data.frontend_id !== undefined && !props.idIsUserCreated;

  const uuidv4 = () => ([1e7]+-1e3+-4e3+-8e3+-1e11).replace(/[018]/g, c => (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16));

  const columnDefs = [
    { headerName: "", sortable: false, resizable: false, filter: false, width: 30, colId: 'state', editable: false, valueGetter: 'data', 
      cellRenderer: 'rowStateCellRenderer'
    },
    { headerName: "", field: "error", sortable: false, resizable: false, filter: false, width: 30, editable: false, 
      cellRenderer: 'errorModalCellRenderer'
    },
    ...props.columnDefs
  ];

  function save() {
    setIsSaving(true);

    Promise.all(
        editedRowData.map(async row => props.saveRow(row))
    ).then(results => {
      props.setRowData(oldRowData => {
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
    }).finally(() => setIsSaving(false));
  }

  function addRow() {
    let newItem = {isEdited: true, isNew: true, error: undefined};
    if (!props.idIsUserCreated) {
        newItem = {...newItem, frontend_id: uuidv4() }
    }
    props.setRowData(oldRowData => {
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
      props.setRowData(oldData => {
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
    <div style={{marginBottom: '10px'}}>
      <Button primary onClick={e => save()} disabled={!hasEditedRowData} loading={isSaving} icon='save' content='Save' />
      <Button positive icon='plus' onClick={e => addRow()} content='Add Row' />
      <Button onClick={e => props.onRefresh()} loading={props.loading} icon='refresh' content={((!hasEditedRowData) ? 'Refresh' :  'Reset')} />
    </div>
    <div className="ag-theme-balham" style={{ flexGrow: 1, width: '100%' }}>
      <AgGridReact
        frameworkComponents={{errorModalCellRenderer: ErrorModalCellRenderer, rowStateCellRenderer: RowStateCellRenderer }}
        columnDefs={columnDefs}
        defaultColDef={defaultColDef}
        rowData={props.rowData}
        deltaRowDataMode
        getRowNodeId={data => {
            if (isFrontendRowNodeOnly(data)) return data.frontend_id; else return getRowNodeId(data);
        }} />
    </div>
    </>;
}
