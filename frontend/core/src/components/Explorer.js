import React from 'react';
import MainAreaCI from './MainAreaCI';
import Timeline from './Timeline';
import { useParams } from 'react-router-dom'
import ExplorerLayers from './ExplorerLayers';

function Explorer() {
  const { ciid } = useParams();
  
  return (
    <div style={{position: 'relative', height: '100%'}}>
      <div className="left-bar">
        <div className={"timeline"}>
          <Timeline ciid={ciid}></Timeline>
        </div>
        <div className={"layers"}>
          <h4>Layers</h4>
          <ExplorerLayers />
        </div>
      </div>
      <div className="center">
        <MainAreaCI ciid={ciid}></MainAreaCI>
      </div>
    </div>
  );
}

export default Explorer;