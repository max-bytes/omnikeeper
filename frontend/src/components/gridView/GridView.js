import React, { useState } from "react";
import { AgGridReact } from "ag-grid-react";
import "ag-grid-community/dist/styles/ag-grid.css";
import "ag-grid-community/dist/styles/ag-theme-balham.css";
import { Layout } from "antd";
import GridViewButtonToolbar from "./GridViewButtonToolbar";
import _ from "lodash";
import getMockUpData from "./GridViewMockUpDataProvider"; // returns mockUp-data for testing // TODO: remove, when finally using API

const { Header, Content } = Layout;

export default function GridView(props) {
    const [gridApi, setGridApi] = useState(null);
    const [gridColumnApi, setGridColumnApi] = useState(null);

    // Load mockup data
    const schema = getMockUpData("schema");
    const data = getMockUpData("data");
    const [columnDefs, setColumnDefs] = useState(initColumnDefs(schema)); // Init Schema/columnDefs
    const [rowData, setRowData] = useState(initRowData(data)); // Init Data/rowData
    const defaultColDef = initDefaultColDef(); // Init defaultColDef

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
                headerName: "CIID",
                field: "ciid",
                editable: false,
                hide: !schema.showCIIDColumn,
                cellStyle: { backgroundColor: "#f2f2f2" },
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
                    return editable ? {} : { backgroundColor: "#f2f2f2" };
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
        let obj = _.find(data.rows, function (o) {
            return o.ciid === ciid;
        });
        if (obj)
            obj = _.find(obj.cells, function (o) {
                return o.name === name;
            });
        return obj ? obj.changeable : true;
    }

    // ######################################## ROW FUNCTIONS ########################################

    // add new row(s)
    function newRows(e) {
        if (e) {
            var numberOfNewRows = e.currentTarget.value; // how many rows to add
            for (var i = 0; i < numberOfNewRows; i++)
                gridApi.updateRowData({
                    add: [{}], // add empty values
                });
        }
    }

    // ######################################## CRUD OPERATIONS ########################################

    // mark row as 'deleted' // TODO
    function markRowAsDeleted() {
        alert("not implemented yet");
    }

    // CREATE / UPDATE / DELETE on pressing 'save' // TODO
    function save() {
        alert("not implemented yet");
    }

    // refresh data // TODO
    function refreshData() {
        alert("not implemented yet");
    }

    // ######################################## AG GRID FORMATTING ########################################

    // resize table and fit to column sizes
    function autoSizeAll() {
        gridColumnApi.autoSizeAllColumns();
    }
}
