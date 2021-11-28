import React from "react";
import { Button, Input } from "antd";
import { useAGGridEnterprise } from 'utils/useAGGridEnterprise';

export default function ContextButtonToolbar(props) {
    const aGGridEnterpriseActive = useAGGridEnterprise();

    return (
        <div className="button-toolbar">
            <div
                className="button-toolbar-row"
                style={{ display: "flex", justifyContent: "space-between", marginTop: "10px", marginBottom: "10px" }}
            >
                <div style={{ display: "flex" }}>
                    {/* New rows: */}
                    <Button type="text" style={{ cursor: "default", paddingLeft: 0, paddingRight: "10px" }}>New rows:</Button>
                    <Button value={1} onClick={props.newRows}>1</Button>
                    <Button value={10} onClick={props.newRows}>10</Button>
                    <Button value={50} onClick={props.newRows}>50</Button>

                    {/* Delete row */}
                    <Button style={{ marginLeft: "10px" }} onClick={props.markRowAsDeleted}>Delete row</Button>

                    {/* Reset row */}
                    <Button onClick={props.resetRow}>Reset row</Button>
                </div>

                <div style={{ display: "flex" }}>
                    {/* Set cell to '[not set]' (= null/undefined) */}
                    <Button style={{ marginLeft: "10px" }} onClick={props.setCellToNotSet}>Set cell to '[not set]'</Button>
                </div>

                <div style={{ display: "flex", marginLeft: "10px" }}>
                    {/* Fit */}
                    <Button onClick={props.autoSizeAll}>Fit</Button>

                    {/* Save */}
                    <Button onClick={() => props.save()}>Save</Button>

                    {/* Refresh */}
                    <Button onClick={() => props.refreshData()}>Refresh</Button>
                </div>

                <div style={{ display: "flex", marginLeft: "10px" }}>
                    <Input placeholder="Search..." allowClear onChange={(e) => props.setQuickFilter(e.target.value)} />
                </div>
                {/* ######################################## Export ######################################## */}
                {aGGridEnterpriseActive &&
                    <div style={{ display: "flex", alignItems: "center" }}>
                        <Button type="text" style={{ cursor: "default" }}>Export:</Button>
                        <Button size="small" onClick={props.excelExport}>Excel</Button>
                    </div>
                }
            </div>
        </div>
    );
}
