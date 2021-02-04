import React from "react";
import { Popover } from "antd";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faExclamationCircle } from "@fortawesome/free-solid-svg-icons";
import { ErrorView } from "./ErrorView";

export function ErrorModalCellRenderer(props) {
    var error = props.getValue();
    if (!error) return "";
    return (
        <Popover
            placement="right"
            trigger="click"
            content={<ErrorView error={error} inPopup={true} />}
        >
            <FontAwesomeIcon color="red" icon={faExclamationCircle} />
        </Popover>
    );
}
