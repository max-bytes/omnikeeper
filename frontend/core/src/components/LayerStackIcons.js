
import React from 'react';
import PropTypes from 'prop-types'

// TODO: refactor to own file, avoid duplication
function argbToRGB(color) {
  return '#'+ ('000000' + (color & 0xFFFFFF).toString(16)).slice(-6);
}
function LayerStackIcons(props) {
    var numLayers = props.layerStack.length;
    return (<span style={{display: 'block', position: 'relative', flexShrink: 0, height: '16px', width: '16px', margin: "0px 5px"}}>{props.layerStack.slice().reverse().map((l, i) => {
        var j = (numLayers - 1 - i);
        return <div key={i} style={{backgroundColor: argbToRGB(l.color), position: 'absolute', top: (j * 3) + "px", left: (-j * 3) + "px", width: "16px", height: "16px", border: "1px solid black"}}></div>
    })}</span>
  );
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