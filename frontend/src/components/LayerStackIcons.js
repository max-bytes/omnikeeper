
import React from 'react';
import PropTypes from 'prop-types'

function LayerStackIcons(props) {
    var numLayers = props.layerStack.length;
    return (<span style={{display: 'block', position: 'relative', height: '16px', width: '16px', margin: "0px 5px"}}>{props.layerStack.map((l, i) => {
        var j = (numLayers - 1 - i);
        return <div key={i} style={{backgroundColor: l.color, position: 'absolute', top: (j * 3) + "px", left: (-j * 3) + "px", width: "16px", height: "16px", border: "1px solid black"}}></div>
    })}</span>
  );
}

LayerStackIcons.propTypes = {
  layerStack: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.number.isRequired,
      name: PropTypes.string.isRequired,
      color: PropTypes.string.isRequired
    }).isRequired
  ).isRequired
}

export default LayerStackIcons;