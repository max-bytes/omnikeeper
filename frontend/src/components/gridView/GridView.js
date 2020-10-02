import React, { useState } from "react";
import { AgGridReact } from "ag-grid-react";
import "ag-grid-community/dist/styles/ag-grid.css";
import "ag-grid-community/dist/styles/ag-theme-balham.css";

export default function GridView(props) {
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
    };

    return (
        <div
            className="ag-theme-balham"
            style={{ flexGrow: 1, height: "100%", width: "100%" }}
        >
            <AgGridReact
                rowData={rowData}
                columnDefs={columnDefs}
                defaultColDef={defaultColDef}
            ></AgGridReact>
        </div>
    );
}
