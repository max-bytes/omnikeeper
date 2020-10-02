import React from "react";
import { Button } from "antd";

export default function GridViewButtonToolbar(props) {
    return (
        <div className="button-toolbar" style={{ width: "100%" }}>
            <Button onClick={props.setCellToNotSet}>Set to '[not set]'</Button>
            <Button
                onClick={props.setCellToEmpty}
                style={{ marginLeft: "10px" }}
            >
                Set empty
            </Button>
        </div>
    );
}
