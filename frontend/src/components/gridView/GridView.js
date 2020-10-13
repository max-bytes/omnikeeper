import React, { useState } from "react";
import { AgGridReact } from "ag-grid-react";
import "ag-grid-community/dist/styles/ag-grid.css";
import "ag-grid-community/dist/styles/ag-theme-balham.css";
import { Layout } from "antd";
import GridViewButtonToolbar from "./GridViewButtonToolbar";
import "./GridView.css";
import GridViewDataParseModel from "./GridViewDataParseModel";
import GridViewMockUpDataModel from "./GridViewMockUpDataModel"; // returns mockUp-data for testing // TODO: remove, when finally using API
import _ from "lodash";

const { Header, Content } = Layout;

export default function GridView(props) {
    const [gridApi, setGridApi] = useState(null);
    const [gridColumnApi, setGridColumnApi] = useState(null);

    const [columnDefs, setColumnDefs] = useState(null);
    const [rowData, setRowData] = useState(null);
    const [rowDataSnapshot, setRowDataSnapshot] = useState(null);
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

    const gridViewDataParseModel = new GridViewDataParseModel(rowStatus);
    const gridViewMockUpDataModel = new GridViewMockUpDataModel();

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
                    resetRow={resetRow}
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
            const params = { ...focusedCell.column, node: rowNode }; // HACK: build needed 'params'-information
            const editableCell = focusedCell.column.colDef.editable(params);
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
            const params = { ...focusedCell.column, node: rowNode }; // HACK: build needed 'params'-information
            const editableCell = focusedCell.column.colDef.editable(params);
            const editableCol = focusedCell.column.colDef.editable;
            if (editableCol && editableCell)
                rowNode.setDataValue(focusedCell.column.colId, "");
        }
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
            for (var i = 0; i < numberOfNewRows; i++) {
                gridApi.applyTransaction({
                    add: [
                        {
                            ciid: "_t_" + Number(tempId + i), // set tempId
                            status: rowStatus.new, // set status to 'new'
                        },
                    ], // remaining attributes: undefined
                });
                setTempId(Number(tempId) + Number(numberOfNewRows)); // set tempId (state)
            }
        }
    }

    // ######################################## CRUD OPERATIONS ########################################

    // mark row as 'deleted'
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

    // undo changes made to row
    function resetRow() {
        var selectedRows = gridApi.getSelectedRows(); // get selected rows
        var numberOfSelectedRows = selectedRows.length;

        for (var i = 0; i < numberOfSelectedRows; i++) {
            const row = selectedRows[i];
            const rowNode = gridApi.getRowNode(row.ciid);

            if (rowNode) {
                if (rowNode.data.status.id === rowStatus.new.id) {
                    // reset row
                    gridApi.applyTransaction({
                        update: [
                            {
                                ciid: row.ciid,
                                status: rowStatus.new,
                            },
                        ],
                    });
                } else {
                    // reset row
                    gridApi.applyTransaction({
                        update: [
                            _.cloneDeep(
                                _.find(rowDataSnapshot, function (rowSnapshot) {
                                    return rowSnapshot.ciid === row.ciid;
                                })
                            ),
                        ],
                    });
                    // set status to "clean"
                    rowNode.setDataValue("status", rowStatus.clean);
                }
            }
        }
    }

    // CREATE / UPDATE / DELETE on pressing 'save'
    async function save() {
        // TODO: changes should only contain the cells of row, that changed -> currently contains full row

        let changes = [];
        await gridApi.forEachNode(async (node) => {
            // CREATE
            if (node.data.status.id === rowStatus.new.id) {
                changes.push(node.data); // add to changes
                node.setDataValue("status", rowStatus.clean); // set to clean
            }
            // UPDATE
            else if (node.data.status.id === rowStatus.edited.id) {
                changes.push(node.data); // add to changes
                node.setDataValue("status", rowStatus.clean); // set to clean
            }
            // DELETE
            else if (node.data.status.id === rowStatus.deleted.id) {
                changes.push(node.data); // add to changes // TODO: HOW TO MARK AS 'TO DELETE'?
                gridApi.applyTransaction({ remove: [node.data] }); // delete from grid
            }
        });

        let sparseData = gridViewDataParseModel.createChanges(changes); // Create changes from rowData (delta)
        console.log(sparseData);
        // TODO: pass sparseData to API, when implemented
    }

    // READ / refresh data
    function refreshData() {
        // TODO: use API, when implemented
        const schema = gridViewMockUpDataModel.getMockUpData("schema"); // get mockUp schema
        const data = gridViewMockUpDataModel.getMockUpData("data"); // get mockUp data

        const parsedColumnDefs = gridViewDataParseModel.createColumnDefs(
            schema,
            data
        ); // Create columnDefs from schema and data
        const parsedRowData = gridViewDataParseModel.createRowData(data); // Create rowData from data

        setColumnDefs(parsedColumnDefs); // set columnDefs
        setRowData(parsedRowData); // set rowData
        setRowDataSnapshot(_.cloneDeep(parsedRowData)); // set rowData-snapshot
        if (gridApi) gridApi.setRowData(parsedRowData); // Tell it AgGrid

        setTempId(0); // Reset tempId
    }

    // ######################################## AG GRID FORMATTING ########################################

    // resize table and fit to column sizes
    function autoSizeAll() {
        gridColumnApi.autoSizeAllColumns();
    }
}
