import React from "react";
import { Popover } from "antd";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faExclamationCircle } from "@fortawesome/free-solid-svg-icons";
import { ErrorView } from "./ErrorView";

export function ErrorPopupButton(props) {
    if (!props.error) return "";
    return (
        <Popover
            className="semantic-popup"
            placement="right"
            trigger="click"
            content={<ErrorView error={props.error} inPopup={true} />}
        >
            <FontAwesomeIcon
                color="red"
                icon={faExclamationCircle}
                style={{ marginLeft: "0.5rem" }}
            />
        </Popover>
    );
}
