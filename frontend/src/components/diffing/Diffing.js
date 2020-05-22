import React from 'react';
import Layers from './../Layers';

function Diffing() {
  return (
    <div style={{position: 'relative', height: '100%'}}>
      <div className="left-bar">
        <div className={"layers"}>
          <h5>Layers</h5>
          <Layers />
        </div>
      </div>
    </div>
  );
}

export default Diffing;