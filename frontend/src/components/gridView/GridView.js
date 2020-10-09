import React, { useState } from "react";
import { AgGridReact } from "ag-grid-react";
import "ag-grid-community/dist/styles/ag-grid.css";
import "ag-grid-community/dist/styles/ag-theme-balham.css";
import { Layout } from "antd";
import GridViewButtonToolbar from "./GridViewButtonToolbar";
import _ from "lodash";
import "./GridView.css";
import getMockUpData from "./GridViewMockUpDataProvider"; // returns mockUp-data for testing // TODO: remove, when finally using API

const { Header, Content } = Layout;

export default function GridView(props) {
    const [gridApi, setGridApi] = useState(null);
    const [gridColumnApi, setGridColumnApi] = useState(null);

    const [columnDefs, setColumnDefs] = useState(null);
    const [rowData, setRowData] = useState(null);
    const [tempId, setTempId] = useState(null);
    const defaultColDef = initDefaultColDef(); // Init defaultColDef

    // status objects
    const rowStatus = {
        new: { id: 0, name: "New" },
        edited: { id: 1, name: "Edit" },
        clean: { id: 2, name: "Clean" },
        deleted: { id: 3, name: "Del" },
        error: { id: 4, name: "Err" },
    };

    return (
        <Layout
            style={{
                height: "100%",
                maxHeight: "100%",
                width: "100%",
                maxWidth: "100%",
                padding: "10px",
            }}
        >
            <Header
                style={{
                    paddingLeft: "0px",
                    background: "none",
                    height: "auto",
                    padding: "unset",
                }}
            >
                <GridViewButtonToolbar
                    setCellToNotSet={setCellToNotSet}
                    setCellToEmpty={setCellToEmpty}
                    newRows={newRows}
                    markRowAsDeleted={markRowAsDeleted}
                    autoSizeAll={autoSizeAll}
                    save={save}
                    refreshData={refreshData}
                />
            </Header>
            <Content>
                <div
                    className="ag-theme-balham"
                    style={{
                        height: "100%",
                        width: "100%",
                    }}
                >
                    <AgGridReact
                        onGridReady={onGridReady}
                        rowData={rowData}
                        columnDefs={columnDefs}
                        defaultColDef={defaultColDef}
                        animateRows={true}
                        rowSelection="multiple"
                        onCellValueChanged={updateCellValue}
                        getRowNodeId={function (data) {
                            return data.ciid;
                        }}
                    ></AgGridReact>
                </div>
            </Content>
        </Layout>
    );

    // ######################################## INIT FUNCTIONS ########################################

    // grid ready
    function onGridReady(params) {
        setGridApi(params.api);
        setGridColumnApi(params.columnApi);
        refreshData();
    }

    // Init Data/rowData
    function initRowData(data) {
        let dataTemp = [];
        _.forEach(data.rows, function (value) {
            let dataCellTemp = [];
            _.forEach(value.cells, function (value) {
                dataCellTemp[value.name] = value.value;
            });
            dataTemp.push({
                status: rowStatus.clean, // set status to 'clean'
                ciid: value.ciid,
                ...dataCellTemp,
            });
        });
        return dataTemp;
    }

    // Init Schema/columnDefs
    function initColumnDefs(schema) {
        let columnDefsTemp = [
            {
                // new, edited, clean, deleted
                headerName: "Status",
                field: "status",
                editable: false,
                checkboxSelection: true, // checkbox for selecting row
                pinned: "left", // pinn to the left
                // set width = minWidth = maxWith, so fitting is suppressed in every possible way
                width: 92,
                minWidth: 92,
                maxWidth: 92,
                resizable: false,
                suppressSizeToFit: true, // suppress sizeToFit
                // get name of status
                valueGetter: function (params) {
                    if (params.data.status !== undefined)
                        return params.data.status.name;
                },
            },
            {
                headerName: "CIID",
                field: "ciid",
                editable: false,
                hide: !schema.showCIIDColumn,
                cellStyle: { fontStyle: "italic" },
            },
        ];
        _.forEach(schema.columns, function (value) {
            columnDefsTemp.push({
                headerName: value.description,
                field: value.name,
                editable: function (params) {
                    const ciid = params.node.data.ciid;
                    const name = params.colDef.field;
                    return getCellEditable(ciid, name);
                },
                cellStyle: function (params) {
                    const editable = params.colDef.editable(params);
                    return editable ? {} : { fontStyle: "italic" };
                },
            });
        });
        return columnDefsTemp;
    }

    // Init defaultColDef
    function initDefaultColDef() {
        return {
            sortable: true,
            filter: true,
            editable: true,
            resizable: true,
            cellClassRules: {
                // specified in css
                new: function (params) {
                    if (params.data.status.id === rowStatus.new.id) return true;
                },
                edited: function (params) {
                    if (params.data.status.id === rowStatus.edited.id)
                        return true;
                },
                clean: function (params) {
                    if (params.data.status.id === rowStatus.clean.id)
                        return true;
                },
                deleted: function (params) {
                    if (params.data.status.id === rowStatus.deleted.id)
                        return true;
                },
            },
            valueSetter: function (params) {
                // undefined/null -> ""
                if (
                    (params.oldValue === undefined ||
                        params.oldValue === null) &&
                    params.newValue === ""
                ) {
                    return true;
                }
                // normal input
                else {
                    params.data[params.colDef.field] = params.newValue;
                    return true;
                }
            },
            valueFormatter: (params) => {
                if (params.value === undefined || params.value === null)
                    return "[not set]";
                else return params.value;
            },
        };
    }

    // ######################################## CELL FUNCTIONS ########################################

    // sets focused cell to [not set]
    function setCellToNotSet() {
        var focusedCell = gridApi.getFocusedCell();
        if (focusedCell) {
            var rowNode = gridApi.getDisplayedRowAtIndex(focusedCell.rowIndex);
            const name = focusedCell.column.colDef.field;
            const ciid = rowNode.data.ciid;
            const editableCell = getCellEditable(ciid, name);
            const editableCol = focusedCell.column.colDef.editable;
            if (editableCol && editableCell)
                rowNode.setDataValue(focusedCell.column.colId, null);
        }
    }

    // sets focused cell to ""
    function setCellToEmpty() {
        var focusedCell = gridApi.getFocusedCell();
        if (focusedCell) {
            var rowNode = gridApi.getDisplayedRowAtIndex(focusedCell.rowIndex);
            const name = focusedCell.column.colDef.field;
            const ciid = rowNode.data.ciid;
            const editableCell = getCellEditable(ciid, name);
            const editableCol = focusedCell.column.colDef.editable;
            if (editableCol && editableCell)
                rowNode.setDataValue(focusedCell.column.colId, "");
        }
    }

    // returns editable/changeable-attr of cell, defined by its ciid and name/colName
    function getCellEditable(ciid, name) {
        const data = getMockUpData("data");
        let obj;
        if (data) {
            obj = _.find(data.rows, function (o) {
                return o.ciid === ciid;
            });
            if (obj)
                obj = _.find(obj.cells, function (o) {
                    return o.name === name;
                });
        }
        return obj ? obj.changeable : true;
    }

    // update cell
    function updateCellValue(e) {
        var rowNode = gridApi.getRowNode(e.data.ciid);
        if (rowNode) {
            if (
                e.colDef.field !== "status" && // ignore status changes
                e.data.status.id !== rowStatus.new.id // ignore status "new"
            ) {
                const oldValue =
                    e.oldValue === null || e.oldValue === undefined // [not set]-Attributes: null, undefined
                        ? null // set [not set]-Attributes to null, so js-comparison does not detect a difference
                        : e.oldValue.toString(); // [set]-Attributes: e.g. "" (empty string), any other set value

                // analog to oldValue
                const newValue =
                    e.newValue === null || e.newValue === undefined
                        ? null
                        : e.newValue.toString();

                // ignore unchanged data
                if (newValue !== oldValue)
                    rowNode.setDataValue("status", rowStatus.edited);
            }
        }
    }

    // ######################################## ROW FUNCTIONS ########################################

    // add new row(s)
    function newRows(e) {
        if (e) {
            var numberOfNewRows = e.currentTarget.value; // how many rows to add
            for (var i = 0; i < numberOfNewRows; i++)
                gridApi.applyTransaction({
                    add: [
                        {
                            ciid: "_t_" + tempId, // set tempId
                            status: rowStatus.new, // set status to 'new'
                        },
                    ], // remaining attributes: undefined
                });
            setTempId(tempId + 1); // increment tempId
        }
    }

    // ######################################## CRUD OPERATIONS ########################################

    // mark row as 'deleted' // TODO
    function markRowAsDeleted() {
        var selectedRows = gridApi.getSelectedRows(); // get selected rows
        var numberOfSelectedRows = selectedRows.length;

        for (var i = 0; i < numberOfSelectedRows; i++) {
            var rowNode = gridApi.getRowNode(selectedRows[i].ciid);
            // directly delete entry, if "new"
            if (rowNode && selectedRows[i].status.id === rowStatus.new.id)
                gridApi.applyTransaction({ remove: [selectedRows[i]] });
            // set status to "deleted", when not "new"
            else if (rowNode) rowNode.setDataValue("status", rowStatus.deleted);
        }
    }

    // CREATE / UPDATE / DELETE on pressing 'save' // TODO
    function save() {
        alert("not implemented yet");
    }

    // READ / refresh data
    function refreshData() {
        // TODO: use API, when implemented
        const schema = getMockUpData("schema"); // get mockUp schema
        const data = getMockUpData("data"); // get mockUp data

        setColumnDefs(initColumnDefs(schema)); // Init Schema/columnDefs
        setRowData(initRowData(data)); // Init Data/rowData
        setTempId(0); // Reset tempId
        if (gridApi) gridApi.setRowData(rowData); // Tell it AgGrid
    }

    // ######################################## AG GRID FORMATTING ########################################

    // resize table and fit to column sizes
    function autoSizeAll() {
        gridColumnApi.autoSizeAllColumns();
    }
}
