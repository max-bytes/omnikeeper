import React, { useState } from "react";
import { AgGridReact } from "ag-grid-react";
import "ag-grid-community/dist/styles/ag-grid.css";
import "ag-grid-community/dist/styles/ag-theme-balham.css";
import { Layout } from "antd";
import GridViewButtonToolbar from "./GridViewButtonToolbar";

const { Header, Content } = Layout;

export default function GridView(props) {
    const [gridApi, setGridApi] = useState(null);
    const [gridColumnApi, setGridColumnApi] = useState(null);

    const [rowData, setRowData] = useState([
        {
            ciName: "CI-A",
            attr1: "Value A-1",
            attr2: "Value A-2",
            attr3: "Value A-3",
        },
        { ciName: "CI-B", attr1: "Value B-1", attr3: "Value B-3" },
        {
            ciName: "CI-C",
            attr1: "Value C-1",
            attr2: "Value C-2",
            attr3: "Value C-3",
            attr4: "Value C-4",
        },
    ]);

    const [columnDefs, setColumnDefs] = useState([
        {
            headerName: "CI-Name",
            field: "ciName",
        },
        {
            headerName: "Attribute 1",
            field: "attr1",
        },
        {
            headerName: "Attribute 2",
            field: "attr2",
        },
        {
            headerName: "Attribute 3",
            field: "attr3",
        },
    ]);

    const defaultColDef = {
        sortable: true,
        filter: true,
        editable: true,
        resizable: true,
        valueSetter: function (params) {
            // undefined -> ""
            if (params.oldValue === undefined && params.newValue === "") {
                return true;
            }
            // normal input
            else {
                params.data[params.colDef.field] = params.newValue;
                return true;
            }
        },
        valueFormatter: (params) => {
            if (params.value === undefined) return "[not set]";
            else return params.value;
        },
    };

    function onGridReady(params) {
        setGridApi(params.api);
        setGridColumnApi(params.columnApi);
    }

    function setCellToNotSet() {
        var focusedCell = gridApi.getFocusedCell();
        var rowNode = gridApi.getDisplayedRowAtIndex(focusedCell.rowIndex);
        rowNode.setDataValue(focusedCell.column.colId, undefined);
    }

    function setCellToEmpty() {
        var focusedCell = gridApi.getFocusedCell();
        var rowNode = gridApi.getDisplayedRowAtIndex(focusedCell.rowIndex);
        rowNode.setDataValue(focusedCell.column.colId, "");
    }

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
                }}
            >
                <GridViewButtonToolbar
                    setCellToNotSet={setCellToNotSet}
                    setCellToEmpty={setCellToEmpty}
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
}
