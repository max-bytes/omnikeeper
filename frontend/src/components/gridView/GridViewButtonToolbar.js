import React from "react";
import { Button } from "antd";

export default function GridViewButtonToolbar(props) {
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
                    <Button
                        type="text"
                        style={{ cursor: "default", paddingLeft: "19px" }}
                    >
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

                    {/* Delete row // TODO */}
                    {/* <Button
                        style={{ marginLeft: "10px" }}
                        onClick={props.markRowAsDeleted}
                    >
                        Delete row
                    </Button> */}
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

                    {/* Save // TODO */}
                    {/* <Button onClick={props.save}>Save</Button> */}

                    {/* Refresh */}
                    <Button onClick={props.refreshData}>Refresh</Button>
                </div>
            </div>
            <div
                className="button-toolbar-row"
                style={{ display: "flex", marginBottom: "10px" }}
            >
                <Button onClick={props.setCellToNotSet}>
                    Set to '[not set]'
                </Button>
                <Button
                    onClick={props.setCellToEmpty}
                    style={{ marginLeft: "10px" }}
                >
                    Set empty
                </Button>
            </div>
        </div>
    );
}
