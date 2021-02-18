import React, { useEffect } from "react";
import LayerIcon from "./LayerIcon";
import { Select } from "antd";

function LayerDropdown(props) {
    const {selectedLayer, layers, onSetSelectedLayer} = props;

    // setting a selected item by default (=first), if none is selected
    useEffect(() => {
        if (!selectedLayer && layers.length > 0) {
            onSetSelectedLayer(layers[0]);
        }
    }, [selectedLayer, layers, onSetSelectedLayer]);

    if (layers.length === 0)
        return (
            <Select
                placeholder="No layer selectable"
                style={{ width: "100%" }}
                disabled
            />
        );

    return (
        <Select
            placeholder="Select layer"
            style={{ width: "100%" }}
            value={selectedLayer?.id}
            onChange={(e, data) => {
                const newLayer = layers.find((l) => l.id === data.value);
                onSetSelectedLayer(newLayer);
            }}
            options={layers.map((at) => {
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
