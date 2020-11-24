import React, { useState } from "react";
import { AgGridReact } from "ag-grid-react";
import "ag-grid-community/dist/styles/ag-grid.css";
import "ag-grid-community/dist/styles/ag-theme-balham.css";
import { Layout, Alert } from "antd";
import ContextButtonToolbar from "./ContextButtonToolbar";
import "./Context.css";
import GridViewDataParseModel from "./GridViewDataParseModel";
import _ from "lodash";
// TODO: use aggrid_copy_cut_paste - USE THIS:
// import AgGridCopyCutPasteHOC from "aggrid_copy_cut_paste";
// const AgGridCopyCutPaste = AgGridCopyCutPasteHOC(
//     AgGridReact, // React-AgGrid component
//     { className: "ag-theme-balham" }, // hocProps
//     false // logging off
// );
import { useParams, withRouter } from "react-router-dom";

const { Header, Content } = Layout;

export function Context(props) {
    const swaggerJson = props.swaggerJson;
    const apiVersion = props.apiVersion;

    const { contextName } = useParams(); // get contextName from path

    const [gridApi, setGridApi] = useState(null);
    const [gridColumnApi, setGridColumnApi] = useState(null);

    const [columnDefs, setColumnDefs] = useState(null);
    const [rowData, setRowData] = useState(null);
    const [rowDataSnapshot, setRowDataSnapshot] = useState(null);
    const [tempId, setTempId] = useState(null);
    const defaultColDef = initDefaultColDef(); // Init defaultColDef

    const [swaggerMsg, setSwaggerMsg] = useState("");
    const [swaggerError, setSwaggerError] = useState(false);

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
                backgroundColor: "white",
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
                {swaggerMsg && <Alert message={swaggerMsg} type={swaggerError ? "error": "success"} showIcon banner/>}
                <ContextButtonToolbar
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
                { swaggerJson ? (
                    //  TODO: use aggrid_copy_cut_paste
                    // REMOVE THIS:
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
                                '<span class="ag-overlay-loading-center">No data.</span>'
                            }
                        />
                    </div>
                    // USE THIS:
                    // <AgGridCopyCutPaste
                    //     onGridReady={onGridReady}
                    //     rowData={rowData}
                    //     columnDefs={columnDefs}
                    //     defaultColDef={defaultColDef}
                    //     animateRows={true}
                    //     rowSelection="multiple"
                    //     onCellValueChanged={updateCellValue}
                    //     getRowNodeId={function (data) {
                    //         return data.ciid;
                    //     }}
                    //     overlayLoadingTemplate={
                    //         '<span class="ag-overlay-loading-center">Loading...</span>'
                    //     }
                    //     overlayNoRowsTemplate={
                    //         '<span class="ag-overlay-loading-center">Please choose context.</span>'
                    //     }
                    // />
                ) :
                <>Loading...</> }
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
    async function save() {
        if (swaggerJson) {
            let rowDataDiffs = [];

            await gridApi.forEachNode(async (node) => {
                if (
                    node.data.status.id === rowStatus.new.id || // CREATE
                    node.data.status.id === rowStatus.edited.id || // UPDATE
                    node.data.status.id === rowStatus.deleted.id // DELETE
                ) {
                    const rowSnapshot = _.find(rowDataSnapshot, function (
                        rowSnapshot
                    ) {
                        return rowSnapshot.ciid === node.data.ciid;
                    });
                    let rowDataDiff = getDiffBetweenObjects(node.data, rowSnapshot);
                    rowDataDiff["ciid"] = node.data.ciid; // add ciid

                    rowDataDiffs.push(rowDataDiff); // add to rowDataDiffs
                }
            });

            const changes = gridViewDataParseModel.createChanges(rowDataDiffs); // Create changes from rowData (delta)

            try {
                // actually do the changes
                const changeResults = await swaggerJson.apis.GridView.ChangeData(
                        {
                            version: apiVersion,
                            context: contextName,
                        },
                        {
                            requestBody: changes,
                        }
                    )
                    .then((result) => result.body);

                // Create rowData from changeResults
                const rowDataChangeResults = gridViewDataParseModel.createRowData(
                    changeResults
                );

                // update rows
                _.forEach(rowDataChangeResults, function (value) {
                    gridApi.applyTransaction({ update: [value] }); // delete from grid
                });

                setSwaggerError(false);
                setSwaggerMsg("Saved.");
            } catch(e) { // TODO: find a way to get HTTP-Error-Code and -Msg and give better feedback!
                setSwaggerError(true);
                setSwaggerMsg(e.toString());
            }
        }
    }

    // READ / refresh data
    async function refreshData() {
        if (swaggerJson) {
            // Tell AgGrid to reset columnDefs and rowData // important!
            if (gridApi) {
                gridApi.setColumnDefs(null);
                gridApi.setRowData(null);
                gridApi.showLoadingOverlay(); // trigger "Loading"-state (otherwise would be in "No Rows"-state instead)
            }
            try {
                const schema = await swaggerJson.apis.GridView.GetSchema({
                        version: apiVersion,
                        context: contextName,
                    })
                    .then((result) => result.body);
                const data = await swaggerJson.apis.GridView.GetData({
                        version: apiVersion,
                        context: contextName,
                    })
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

                // INFO: don't show message on basic load
            } catch(e) { // TODO: find a way to get HTTP-Error-Code and -Msg and give better feedback!
                setSwaggerError(true);
                setSwaggerMsg(e.toString());
            }
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

export default withRouter(Context);