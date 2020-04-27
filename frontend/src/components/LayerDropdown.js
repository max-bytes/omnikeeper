import React from "react";
import LayerIcon from "./LayerIcon";
import { Dropdown } from 'semantic-ui-react';

function LayerDropdown(props) {
  if (props.layers.length === 0)
    return <Dropdown placeholder="No layer selectable" disabled />;
  let selectedLayer = props.selectedLayer;
  if (!selectedLayer) {
    selectedLayer = props.layers[0];
    props.onSetSelectedLayer(selectedLayer);
  }
  return <Dropdown placeholder='Select layer' fluid trigger={<><LayerIcon layer={selectedLayer} />{selectedLayer.name}</>} value={selectedLayer.id}
  onChange={(e, data) => {
    const newLayer = props.layers.find(l => l.id === data.value);
    props.onSetSelectedLayer(newLayer);
  }}
  options={props.layers.map(at => { return {
    key: at.id, value: at.id, text: at.name, content: (
      <><LayerIcon layer={at} />{at.name}</>
    ) }; })}
/>;
}

export default LayerDropdown;