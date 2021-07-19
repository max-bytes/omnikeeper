import React, { useEffect } from "react";
import LayerIcon from "./LayerIcon";
import { Select } from "antd";

function LayerDropdown(props) {
    const {selectedLayer, layers, onSetSelectedLayer, style} = props;

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
                disabled
            />
        );

    return (
        <Select
            placeholder="Select layer"
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
            style={style}
        />
    );
}

export default LayerDropdown;
