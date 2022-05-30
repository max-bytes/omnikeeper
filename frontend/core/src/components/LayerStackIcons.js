
import React from 'react';
import PropTypes from 'prop-types'
import { Popover } from 'antd';

// TODO: refactor to own file, avoid duplication
function argbToRGB(color) {
  return '#'+ ('000000' + (color & 0xFFFFFF).toString(16)).slice(-6);
}
function LayerStackIcons(props) {
  const numLayers = props.layerStack.length;
  const topLayer = props.layerStack[0];

  const popoverContent = <ul style={{marginBottom: '0px', listStyle: 'none', paddingLeft: '0px'}}>
    <li>Layer ID: {topLayer.id}</li>
    {topLayer.description ? <li>Layer Description: {topLayer.description}</li> : <></>}
  </ul>;

  return <Popover 
    trigger="click" 
    content={popoverContent}
    >
      <span style={{display: 'block', position: 'relative', flexShrink: 0, height: '16px', width: '16px', margin: "0px 5px"}}>{props.layerStack.slice().reverse().map((l, i) => {
            var j = (numLayers - 1 - i);
            return <div key={i} style={{backgroundColor: argbToRGB(l.color), position: 'absolute', top: (j * 3) + "px", left: (-j * 3) + "px", width: "16px", height: "16px", border: "1px solid black"}}></div>
        })}</span>
  </Popover>;
}

LayerStackIcons.propTypes = {
  layerStack: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.string.isRequired,
      color: PropTypes.number.isRequired
    }).isRequired
  ).isRequired
}

export default LayerStackIcons;