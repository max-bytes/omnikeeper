import React from "react";
import { Button, Popconfirm } from "antd";

export default props => {
    if (props.operation === "edit")
        return (
            <span>
                <Button size="small" style={{ width: "60px" }} type="primary" onClick={() => props.history.push(`edit-context/${props.data.name}`)}>Edit</Button>
            </span>
        );
    if (props.operation === "remove")
        return (
            <span>
                <Popconfirm
                    title={`Are you sure to delete ${props.data.name}?`}
                    onConfirm={() => props.removeContext(props.data.name)}
                    okText="Yes"
                    okButtonProps={{type: "danger"}}
                    cancelText="No"
                    cancelButtonProps={{size: "normal"}}
                    placement="topRight"
                >
                    <Button size="small" htmlType="submit" style={{ width: "80px" }} type="danger" >Remove</Button>
                </Popconfirm>
            </span>
        );
 }