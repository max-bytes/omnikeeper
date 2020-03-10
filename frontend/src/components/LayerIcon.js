import React from 'react';
import PropTypes from 'prop-types'

function LayerIcon(props) {
    return (<span style={{display: 'inline-block', position: 'relative', height: '16px', width: '16px', margin: "0px 5px"}}>
      <div style={{backgroundColor: props.layer.color, position: 'absolute', top: 0 + "px", left: 0 + "px", width: "16px", height: "16px", border: "1px solid black"}}></div>
    </span>
  );
}

LayerIcon.propTypes = {
  layer: PropTypes.shape({
    id: PropTypes.number.isRequired,
    name: PropTypes.string.isRequired,
    visibility: PropTypes.bool.isRequired,
    color: PropTypes.string.isRequired
  }).isRequired
}

export default LayerIcon;