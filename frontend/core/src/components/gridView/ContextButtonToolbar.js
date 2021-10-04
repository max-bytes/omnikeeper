import React from "react";
import { Button } from "antd";
import { useAGGridEnterprise } from './../../utils/useAGGridEnterprise';

export default function ContextButtonToolbar(props) {
    const { data: aGGridEnterpriseActive, loading, error } = useAGGridEnterprise();

    if (error) return "Error:" + error;
    if (loading) return "Loading...";

    return (
        <div className="button-toolbar">
            <div
                className="button-toolbar-row"
                style={{
                    display: "flex",
                    justifyContent: "space-between",
                    marginTop: "10px",
                    marginBottom: "10px",
                }}
            >
                <div
                    style={{
                        display: "flex",
                    }}
                >
                    {/* New rows: */}
                    <Button type="text" style={{ cursor: "default" }}>
                        New rows:
                    </Button>
                    <Button value={1} onClick={props.newRows}>
                        1
                    </Button>
                    <Button value={10} onClick={props.newRows}>
                        10
                    </Button>
                    <Button value={50} onClick={props.newRows}>
                        50
                    </Button>

                    {/* Delete row */}
                    <Button
                        style={{ marginLeft: "10px" }}
                        onClick={props.markRowAsDeleted}
                    >
                        Delete row
                    </Button>

                    {/* Reset row */}
                    <Button
                        // style={{ marginLeft: "10px" }}
                        onClick={props.resetRow}
                    >
                        Reset row
                    </Button>
                </div>

                <div
                    style={{
                        display: "flex",
                    }}
                >
                    {/* Set cell to '[not set]' (= null/undefined) */}
                    <Button
                        style={{ marginLeft: "10px" }}
                        onClick={props.setCellToNotSet}
                    >
                        Set cell to '[not set]'
                    </Button>
                </div>

                <div
                    style={{
                        display: "flex",
                    }}
                >
                    {/* Fit */}
                    <Button
                        style={{ marginRight: "10px" }}
                        onClick={props.autoSizeAll}
                    >
                        Fit
                    </Button>

                    {/* Save */}
                    <Button onClick={() => props.save()}>Save</Button>

                    {/* Refresh */}
                    <Button onClick={() => props.refreshData()}>Refresh</Button>
                </div>

                {/* ######################################## Export ######################################## */}
                {aGGridEnterpriseActive &&
                <div style={{ display: "flex", alignItems: "center" }}>
                    <Button type="text" style={{ cursor: "default" }}>Export:</Button>
                    <Button size="small" style={{ marginRight: "8px" }} onClick={props.excelExport}>Excel</Button>
                </div>
                }
            </div>
        </div>
    );
}
