import React, { useState } from "react";
import { AgGridReact } from "ag-grid-react";
import "ag-grid-community/dist/styles/ag-grid.css";
import "ag-grid-community/dist/styles/ag-theme-balham.css";
import { Layout, Button } from "antd";
// import AgGridCopyCutPasteHOC from "aggrid_copy_cut_paste";
import FeedbackMsg from "components/FeedbackMsg.js";
import EditRemoveButtonCellRenderer from "./EditRemoveButtonCellRenderer.js";

import {  withRouter } from "react-router-dom";


const AgGridCopyCutPaste = AgGridReact;

// const AgGridCopyCutPaste = AgGridCopyCutPasteHOC(
//     AgGridReact, // React-AgGrid component
//     { className: "ag-theme-balham" }, // hocProps
//     true // logging
// );  

const { Header, Content } = Layout;

export function Explorer(props) {
    const swaggerClient = props.swaggerClient;

    const [gridApi, setGridApi] = useState(null);
    const [gridColumnApi, setGridColumnApi] = useState(null);

    const [rowData, setRowData] = useState(null);
    const defaultColDef = initDefaultColDef(); // Init defaultColDef

    const [swaggerMsg, setSwaggerMsg] = useState("");
    const [swaggerErrorJson, setSwaggerErrorJson] = useState(false);

    const columnDefs = [
        { 
            headerName: "ID",
            field: "id", 
            width: 1000,
        },
        {
            headerName: "",
            field: "edit",
            // set width = minWidth = maxWith, so fitting is suppressed in every possible way
            width: 84,
            minWidth: 84,
            maxWidth: 84,
            resizable: false,
            pinned: "right", // pinn to the right
            suppressSizeToFit: true, // suppress sizeToFit
            sortable: false,
            filter: false,
            cellRenderer: "editRemoveButtonCellRenderer",
            cellRendererParams: {
                operation: "edit",
                history: props.history,
            },
        },
        {
            headerName: "",
            field: "remove",
            // set width = minWidth = maxWith, so fitting is suppressed in every possible way
            width: 104,
            minWidth:104,
            maxWidth: 104,
            resizable: false,
            pinned: "right", // pinn to the right
            suppressSizeToFit: true, // suppress sizeToFit
            sortable: false,
            filter: false,
            cellRenderer: "editRemoveButtonCellRenderer",
            cellRendererParams: {
                operation: "remove",
                removeContext: removeContext,
            },
        },
    ];

    return (
        <Layout style={{ height: "100%", maxHeight: "100%", width: "100%", maxWidth: "100%", padding: "10px", backgroundColor: "white" }} >
            <Header style={{ paddingLeft: "0px", background: "none", height: "auto", padding: "unset" }} >
                {swaggerMsg && <FeedbackMsg alertProps={{message: swaggerMsg, type: swaggerErrorJson ? "error": "success", showIcon: true, banner: true}} swaggerErrorJson={swaggerErrorJson} />}
                <div style={{ display: "flex", justifyContent: "flex-end", marginTop: "10px", marginBottom: "10px" }} >
                    <div style={{ display: "flex" }}>
                        <Button style={{ marginRight: "10px" }} onClick={autoSizeAll}>Fit</Button>
                    </div>
                    <div style={{ display: "flex" }}>
                        <Button onClick={() => refreshData()}>Refresh</Button>
                    </div>
                </div>
            </Header>
            <Content>
                <AgGridCopyCutPaste
                    stopEditingWhenCellsLoseFocus={true}
                    onGridReady={onGridReady}
                    rowData={rowData}
                    columnDefs={columnDefs}
                    defaultColDef={defaultColDef}
                    components={{
                        editRemoveButtonCellRenderer: EditRemoveButtonCellRenderer,
                    }}
                    animateRows={true}
                    rowSelection="multiple"
                    getRowId={function (params) {
                        return params.data.id;
                    }}
                    overlayLoadingTemplate={
                        '<span class="ag-overlay-loading-center">Loading...</span>'
                    }
                    overlayNoRowsTemplate={
                        '<span class="ag-overlay-loading-center">No data.</span>'
                    }
                />
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
            editable: false,
            resizable: true,
            width: 500,
            cellStyle: { fontStyle: "italic" },
        };
    }

    // ######################################## CRUD OPERATIONS ########################################

    // READ / refresh context
    async function refreshData() {
        // important to re-create FeedbackMsg, after it has been closed!
        setSwaggerMsg("");
        setSwaggerErrorJson("");

        // Tell AgGrid to reset rowData // important!
        if (gridApi) {
            gridApi.setRowData(null);
            gridApi.showLoadingOverlay(); // trigger "Loading"-state (otherwise would be in "No Rows"-state instead)
        }
        try {
            const contexts = await swaggerClient.apis.OKPluginGenericJSONIngest.ManageContext_GetAllContexts({
                    version: props.apiVersion,
                })
                .then((result) => result.body);

            setRowData(contexts); // set rowData

            // Tell AgGrid to set rowData
            if (gridApi) {
                gridApi.setRowData(contexts);
            }

        // INFO: don't show message on basic load
        } catch(e) {
            setSwaggerErrorJson(JSON.stringify(e.response, null, 2));
            setSwaggerMsg(e.toString());
        }
    }

    async function removeContext(contextID) {
        try {
            await swaggerClient.apis.OKPluginGenericJSONIngest.ManageContext_RemoveContext(
                    {
                        version: props.apiVersion,
                        id: contextID,
                    }
                )
                .then((result) => result.body);

            setSwaggerErrorJson(false);
            setSwaggerMsg("'" + contextID + "' has been removed.");
            refreshData(); // reload
        } catch(e) {
            setSwaggerErrorJson(JSON.stringify(e.response, null, 2));
            setSwaggerMsg(e.toString());
        }
    }

    // ######################################## AG GRID FORMATTING ########################################

    // resize table and fit to column sizes
    function autoSizeAll() {
        gridColumnApi.autoSizeAllColumns();
    }

}

export default withRouter(Explorer);