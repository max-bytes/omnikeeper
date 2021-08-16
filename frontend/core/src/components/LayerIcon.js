import React from 'react';
import PropTypes from 'prop-types'

// TODO: refactor to own file, avoid duplication
function argbToRGB(color) {
  return '#'+ ('000000' + (color & 0xFFFFFF).toString(16)).slice(-6);
}

function LayerIcon(props) {
    const color = props.layer?.color ?? props.color;
    return (<span style={{display: 'inline-block', position: 'relative', height: '16px', width: '16px', margin: "0px 5px"}}>
      <div style={{backgroundColor: argbToRGB(color), position: 'absolute', top: 0 + "px", left: 0 + "px", width: "16px", height: "16px", border: "1px solid black"}}></div>
    </span>
  );
}

LayerIcon.propTypes = {
  layer: PropTypes.shape({
    color: PropTypes.number.isRequired
  }),
  color: PropTypes.number
}

export default LayerIcon;