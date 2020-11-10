import React, { useState } from "react";
import { AgGridReact } from "ag-grid-react";
import "ag-grid-community/dist/styles/ag-grid.css";
import "ag-grid-community/dist/styles/ag-theme-balham.css";
import { Layout } from "antd";
import GridViewButtonToolbar from "./GridViewButtonToolbar";
import "./GridView.css";
import GridViewDataParseModel from "./GridViewDataParseModel";
import _ from "lodash";
import SwaggerClient from "swagger-client";
// TODO: use aggrid_copy_cut_paste - USE THIS:
// import AgGridCopyCutPasteHOC from "aggrid_copy_cut_paste";
// const AgGridCopyCutPaste = AgGridCopyCutPasteHOC(
//     AgGridReact, // React-AgGrid component
//     { className: "ag-theme-balham" }, // hocProps
//     false // logging off
// );

const swaggerDefUrl =
    "https://acme.omnikeeper-dev.bymhx.at/backend/swagger/v1/swagger.json";
const apiVersion = 1;

const { Header, Content } = Layout;

export default function GridView(props) {
    const [gridApi, setGridApi] = useState(null);
    const [gridColumnApi, setGridColumnApi] = useState(null);

    const [columnDefs, setColumnDefs] = useState(null);
    const [rowData, setRowData] = useState([]);
    const [rowDataSnapshot, setRowDataSnapshot] = useState([]);
    const [context, setContext] = useState(null);
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
                    context={context}
                    applyContext={applyContext}
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
                {/* TODO: use aggrid_copy_cut_paste */}
                {/* REMOVE THIS: */}
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
                        overlayLoadingTemplate={
                            '<span class="ag-overlay-loading-center">Loading...</span>'
                        }
                        overlayNoRowsTemplate={
                            '<span class="ag-overlay-loading-center">Please choose context.</span>'
                        }
                    />
                </div>
                {/* USE THIS: */}
                {/* <AgGridCopyCutPaste
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
                    overlayLoadingTemplate={
                        '<span class="ag-overlay-loading-center">Loading...</span>'
                    }
                    overlayNoRowsTemplate={
                        '<span class="ag-overlay-loading-center">Please choose context.</span>'
                    }
                /> */}
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

    function applyContext(contextName) {
        refreshData(contextName);
    }

    // ######################################## CELL FUNCTIONS ########################################

    // sets focused cell to [not set]
    function setCellToNotSet() {
        var focusedCell = gridApi.getFocusedCell();
        if (focusedCell) {
            var rowNode = gridApi.getDisplayedRowAtIndex(focusedCell.rowIndex);
            if (rowNode) {
                const params = { ...focusedCell.column, node: rowNode }; // HACK: build needed 'params'-information
                const editableCell = focusedCell.column.colDef.editable(params);
                const editableCol = focusedCell.column.colDef.editable;
                if (editableCol && editableCell)
                    rowNode.setDataValue(focusedCell.column.colId, null);
            }
        }
    }

    // sets focused cell to ""
    function setCellToEmpty() {
        var focusedCell = gridApi.getFocusedCell();
        if (focusedCell) {
            var rowNode = gridApi.getDisplayedRowAtIndex(focusedCell.rowIndex);
            if (rowNode) {
                const params = { ...focusedCell.column, node: rowNode }; // HACK: build needed 'params'-information
                const editableCell = focusedCell.column.colDef.editable(params);
                const editableCol = focusedCell.column.colDef.editable;
                if (editableCol && editableCell)
                    rowNode.setDataValue(focusedCell.column.colId, "");
            }
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
    async function save(contextName) {
        let rowDataDiffs = [];
        let rowDataDiffsFullRow = []; // TODO: remove, when finally using API

        await gridApi.forEachNode(async (node) => {
            if (
                node.data.status.id === rowStatus.new.id || // CREATE
                node.data.status.id === rowStatus.edited.id || // UPDATE
                node.data.status.id === rowStatus.deleted.id // DELETE // TODO: HOW TO MARK AS 'TO DELETE'?
            ) {
                const rowSnapshot = _.find(rowDataSnapshot, function (
                    rowSnapshot
                ) {
                    return rowSnapshot.ciid === node.data.ciid;
                });
                let rowDataDiff = getDiffBetweenObjects(node.data, rowSnapshot);
                rowDataDiff["ciid"] = node.data.ciid; // add ciid

                rowDataDiffs.push(rowDataDiff); // add to rowDataDiffs
                rowDataDiffsFullRow.push(node.data); // TODO: remove, when finally using API
            }
        });

        const changes = gridViewDataParseModel.createChanges(rowDataDiffs); // Create changes from rowData (delta)
        console.log("changes to " + contextName + ": ", changes);

        // fake changeResults data here
        // TODO: pass 'changes' to API and get 'changeResults' back, when implemented
        const changeResults = {
            rows: gridViewDataParseModel.createChanges(rowDataDiffsFullRow)
                .sparseRows,
        };

        // Create rowData from changeResults
        const rowDataChangeResults = gridViewDataParseModel.createRowData(
            changeResults
        );

        // update rows
        _.forEach(rowDataChangeResults, function (value) {
            gridApi.applyTransaction({ update: [value] }); // delete from grid
        });
    }

    // READ / refresh data
    async function refreshData(contextName) {
        // Tell AgGrid to reset columnDefs and rowData // important!
        if (gridApi) {
            gridApi.setColumnDefs(null);
            gridApi.setRowData("");
        }

        // if called without a contextName -> fetch only context
        if (!contextName) {
            const context = await new SwaggerClient(swaggerDefUrl)
                .then((client) =>
                    client.apis.GridView.GetContexts({ version: apiVersion })
                )
                .then((result) => result.body);

            setContext(context); // set context
        }

        // if called with a contextName -> fetch schema & data
        else {
            const schema = await new SwaggerClient(swaggerDefUrl)
                .then((client) =>
                    client.apis.GridView.GetSchema({
                        version: apiVersion,
                        context: contextName,
                    })
                )
                .then((result) => result.body);
            const data = await new SwaggerClient(swaggerDefUrl)
                .then((client) =>
                    client.apis.GridView.GetData({
                        version: apiVersion,
                        context: contextName,
                    })
                )
                .then((result) => result.body);

            const parsedColumnDefs = gridViewDataParseModel.createColumnDefs(
                schema,
                data
            ); // Create columnDefs from schema and data
            const parsedRowData = gridViewDataParseModel.createRowData(data); // Create rowData from data

            setColumnDefs(parsedColumnDefs); // set columnDefs
            setRowData(parsedRowData); // set rowData
            setRowDataSnapshot(_.cloneDeep(parsedRowData)); // set rowData-snapshot

            // Tell AgGrid to set columnDefs and rowData
            if (gridApi) {
                gridApi.setColumnDefs(parsedColumnDefs);
                gridApi.setRowData(parsedRowData);
            }

            setTempId(0); // Reset tempId
        }
    }

    // ######################################## AG GRID FORMATTING ########################################

    // resize table and fit to column sizes
    function autoSizeAll() {
        gridColumnApi.autoSizeAllColumns();
    }

    // ######################################## HELPERS ########################################

    function getDiffBetweenObjects(newObj, oldObj) {
        function changes(v, oldObj) {
            return _.transform(newObj, function (result, value, key) {
                if (!_.isEqual(value, oldObj[key])) {
                    result[key] =
                        _.isObject(value) && _.isObject(oldObj[key])
                            ? changes(value, oldObj[key])
                            : value;
                }
            });
        }
        return changes(newObj, oldObj);
    }
}
