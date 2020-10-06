import React, { useState } from "react";
import { AgGridReact } from "ag-grid-react";
import "ag-grid-community/dist/styles/ag-grid.css";
import "ag-grid-community/dist/styles/ag-theme-balham.css";
import { Layout } from "antd";
import GridViewButtonToolbar from "./GridViewButtonToolbar";
import _ from "lodash";

const { Header, Content } = Layout;

export default function GridView(props) {
    const [gridApi, setGridApi] = useState(null);
    const [gridColumnApi, setGridColumnApi] = useState(null);

    // Load mockup data
    const schema = getMockUpData("schema");
    const data = getMockUpData("data");

    // Init Data/rowData
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
    const [rowData, setRowData] = useState(dataTemp);

    // Init Schema/columnDefs
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
                const name = params.colDef.field;
                const ciid = params.node.data.ciid;

                let obj = _.find(data.rows, function (o) {
                    return o.ciid === ciid;
                });

                obj = _.find(obj.cells, function (o) {
                    return o.name === name;
                });

                return obj ? obj.changeable : true;
            },
            cellStyle: function (params) {
                const editable = params.colDef.editable(params);
                return editable ? {} : { backgroundColor: "#f2f2f2" };
            },
        });
    });
    const [columnDefs, setColumnDefs] = useState(columnDefsTemp);

    // Init defaultColDef
    const defaultColDef = {
        sortable: true,
        filter: true,
        editable: true,
        resizable: true,
        valueSetter: function (params) {
            // undefined/null -> ""
            if (
                (params.oldValue === undefined || params.oldValue === null) &&
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

    // ########## AG GRID HELPER FUNCTIONS ##########

    function onGridReady(params) {
        setGridApi(params.api);
        setGridColumnApi(params.columnApi);
    }

    function setCellToNotSet() {
        var focusedCell = gridApi.getFocusedCell();
        var rowNode = gridApi.getDisplayedRowAtIndex(focusedCell.rowIndex);
        rowNode.setDataValue(focusedCell.column.colId, null);
    }

    function setCellToEmpty() {
        var focusedCell = gridApi.getFocusedCell();
        var rowNode = gridApi.getDisplayedRowAtIndex(focusedCell.rowIndex);
        rowNode.setDataValue(focusedCell.column.colId, "");
    }

    // ########## HELPER FUNCTIONS ##########

    // function renameObjectKey(obj, oldKey, newKey) {
    //     const clonedObj = Object.assign({}, obj) ;
    //     const targetKey = clonedObj[oldKey];
    //     delete clonedObj[oldKey];
    //     clonedObj[newKey] = targetKey;
    //     return clonedObj;
    // }

    // ########## MOCK UP DATA FUNCTIONS ##########

    // TODO: remove, when implemented
    // returns mockUp-data for testing
    // also see: https://www.mhx.at/gitlab/landscape/registry/snippets/1
    function getMockUpData(type) {
        switch (type) {
            // Contexts:
            // note: when implementing, don't start with this. We know how this will work and it doesn't give us much insight.
            // work with a single, static context at first
            case "context":
                return {
                    configuredContexts: [
                        {
                            name: "test",
                            speakingName: "Test-Context",
                            description:
                                "This is a context used to test out basic GridView capabilities",
                        },
                        {
                            name: "test2",
                            speakingName: "Test-Context2",
                            description:
                                "This is a context2 used to test out basic GridView capabilities",
                        },
                    ],
                };
            // Schema:
            case "schema":
                return {
                    showCIIDColumn: true,
                    columns: [
                        {
                            name: "attr1",
                            description: "Attribute 1",
                        },
                        {
                            name: "attr2",
                            description: "Attribute 2",
                        },
                        {
                            name: "attr3",
                            description: "Attribute 3",
                        },
                    ],
                };
            // Data:
            case "data":
                return {
                    rows: [
                        {
                            ciid: "035fbf89-1ed2-4432-a67e-620577cc806d",
                            // cells that are not present here, but have a defined column in the schema should implicitly
                            // be treated as not-set cells.
                            // if a value of a cell is null, it should be treated as a not-set cell
                            cells: [
                                {
                                    name: "attr1",
                                    value: "Value A-1",
                                    changeable: false,
                                },
                                {
                                    name: "attr2",
                                    value: "Value A-2",
                                    changeable: false,
                                },
                                {
                                    name: "attr3",
                                    value: "Value A-3",
                                    changeable: true,
                                },
                            ],
                        },
                        {
                            ciid: "620577cc806d-a67e-035fbf89-4432-1ed2",
                            cells: [
                                {
                                    name: "attr1",
                                    value: "Value B-1",
                                    changeable: true,
                                },
                                {
                                    name: "attr3",
                                    value: "Value B-3",
                                    changeable: true,
                                },
                            ],
                        },
                        {
                            ciid: "4432-a67e-620577cc806d-035fbf89-1ed2",
                            cells: [
                                {
                                    name: "attr1",
                                    value: "Value C-1",
                                    changeable: true,
                                },
                                {
                                    name: "attr2",
                                    value: "Value C-2",
                                    changeable: true,
                                },
                                {
                                    name: "attr3",
                                    value: "Value C-3",
                                    changeable: false,
                                },
                                {
                                    name: "attr4",
                                    value: "Value C-4",
                                    changeable: true,
                                },
                            ],
                        },
                    ],
                };
            // Changes:
            case "changes":
                return {
                    sparseRows: [
                        // prefixed "sparse", explanation why below
                        {
                            ciid: "035fbf89-1ed2-4432-a67e-620577cc806d",
                            // two possible modes:
                            // 1) only needs to contain the cells that changed
                            //    cells that should be changed to a not-set value should specify value: null
                            // 2) sends ALL the cells for this CI, even the ones that did not change
                            // -> prefer 1! Reason: less traffic, better maps to single attribute changes in backend
                            cells: [
                                {
                                    name: "attributeA",
                                    value: "Value A-1 changed",
                                },
                                // ...
                            ],
                        },
                        // ...
                    ],
                };
            // ChangeResults:
            case "changeResults":
                return {
                    rows: [
                        {
                            ciid: "035fbf89-1ed2-4432-a67e-620577cc806d",
                            // two possible modes:
                            // 1) only needs to contain the cells that changed
                            //    cells that were changed to a not-set value should specify value: null
                            // 2) returns ALL the cells for this CI, even the ones that did not change
                            // -> prefer 2! Reason: better works with concurrent changes, is more in line with
                            //  the regular data retrieval, more future-proof (dependent columns)
                            cells: [
                                {
                                    name: "attributeA",
                                    value: "Value A-1 changed",
                                    changeable: true, // we need to return (and act on) changeable here too
                                },
                                // ...
                            ],
                        },
                        // ...
                    ],
                };

            default:
                break;
        }
    }
}
