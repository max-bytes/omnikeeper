import React from "react";
import LayerIcon from "./LayerIcon";
import { Select } from "antd";

function LayerDropdown(props) {
    if (props.layers.length === 0)
        return (
            <Select
                placeholder="No layer selectable"
                style={{ width: "100%" }}
                disabled
            />
        );
    let selectedLayer = props.selectedLayer;
    if (!selectedLayer) {
        selectedLayer = props.layers[0];
        props.onSetSelectedLayer(selectedLayer);
    }

    return (
        <Select
            placeholder="Select layer"
            style={{ width: "100%" }}
            value={selectedLayer.id}
            onChange={(e, data) => {
                const newLayer = props.layers.find((l) => l.id === data.value);
                props.onSetSelectedLayer(newLayer);
            }}
            options={props.layers.map((at) => {
                return {
                    key: at.id,
                    value: at.id,
                    label: (
                        <>
                            <LayerIcon layer={at} />
                            {at.name}
                        </>
                    ),
                };
            })}
        />
    );
}

export default LayerDropdown;
