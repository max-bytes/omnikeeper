import React from "react";
import LayerIcon from "./LayerIcon";
import { Dropdown } from 'semantic-ui-react';

function LayerDropdown(props) {
  return <Dropdown placeholder='Select layer' fluid trigger={<><LayerIcon layer={props.selectedLayer} />{props.selectedLayer.name}</>} selection value={props.selectedLayer.id}
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