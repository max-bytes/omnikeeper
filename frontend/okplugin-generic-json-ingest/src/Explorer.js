import React, { useState } from "react";
import { AgGridReact } from "ag-grid-react";
import "ag-grid-community/dist/styles/ag-grid.css";
import "ag-grid-community/dist/styles/ag-theme-balham.css";
import { Layout, Button } from "antd";
import AgGridCopyCutPasteHOC from "aggrid_copy_cut_paste";
import FeedbackMsg from "components/FeedbackMsg.js";

import {  withRouter } from "react-router-dom";

const AgGridCopyCutPaste = AgGridCopyCutPasteHOC(
    AgGridReact, // React-AgGrid component
    { className: "ag-theme-balham" }, // hocProps
    true // logging off
);  

const { Header, Content } = Layout;

export function Explorer(props) {
    const swaggerClient = props.swaggerClient;
    const [/*context*/, setContext] = useState(null)
    
    const [swaggerMsg, setSwaggerMsg] = useState("");
    const [swaggerErrorJson, setSwaggerErrorJson] = useState(false);

    // status objects
    const rowStatus = {
        new: { id: 0, name: "New" },
        edited: { id: 1, name: "Edit" },
        clean: { id: 2, name: "Clean" },
        deleted: { id: 3, name: "Del" },
        error: { id: 4, name: "Err" },
    };
    const defaultColDef = initDefaultColDef(); // Init defaultColDef

    return (
        <Layout style={{ height: "100%", maxHeight: "100%", width: "100%", maxWidth: "100%", padding: "10px", backgroundColor: "white" }} >
            <Header style={{ paddingLeft: "0px", background: "none", height: "auto", padding: "unset" }} >
                {swaggerMsg && <FeedbackMsg alertProps={{message: swaggerMsg, type: swaggerErrorJson ? "error": "success", showIcon: true, banner: true}} swaggerErrorJson={swaggerErrorJson} />}
                
                {/* TODO: outsource as 'ButtonToolbar' */}
                <div style={{ display: "flex", justifyContent: "flex-end", marginTop: "10px", marginBottom: "10px" }} >
                    <div style={{ display: "flex" }}>
                        <Button onClick={() => refreshData()}>Refresh</Button>
                    </div>
                </div>
            </Header>
            <Content>
                <AgGridCopyCutPaste
                    stopEditingWhenGridLosesFocus={true}
                    onGridReady={onGridReady}
                    rowData={[]}
                    defaultColDef={defaultColDef}
                    animateRows={true}
                    rowSelection="multiple"
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
        };
    }

    // READ / refresh context
    async function refreshData() {
        try {
            const context = await swaggerClient().apis.OKPluginGenericJSONIngest.GetAllContexts({
                    version: props.apiVersion,
                })
                .then((result) => result.body);
            console.log(context); // TODO: remove

            // TODO: parse and pass to agGrid

            setContext(context);
        } catch(e) {
            setSwaggerErrorJson(JSON.stringify(e.response, null, 2));
            setSwaggerMsg(e.toString());
        }
    }
}

export default withRouter(Explorer);