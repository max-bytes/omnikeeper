import React from "react";
import { Button } from "antd";

export default function GridViewButtonToolbar(props) {
    return (
        <div className="button-toolbar">
            <div
                className="button-toolbar-row1"
                style={{
                    display: "flex",
                    marginTop: "10px",
                    marginBottom: "10px",
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
            </div>
            <div
                className="button-toolbar-row2"
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
