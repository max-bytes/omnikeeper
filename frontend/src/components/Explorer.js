import React from 'react';
import Layers from './Layers';
import MainAreaCI from './MainAreaCI';
import Timeline from './Timeline';
import { useParams } from 'react-router-dom'
import { mutations } from '../graphql/mutations'
import { useMutation } from '@apollo/react-hooks';

function Explorer() {
  const { ciid } = useParams();
  
  // TODO: loading
  const [toggleLayerVisibility] = useMutation(mutations.TOGGLE_LAYER_VISIBILITY);
  const [changeLayerSortOrder] = useMutation(mutations.CHANGE_LAYER_SORT_ORDER);

  return (
    <div style={{position: 'relative', height: '100%'}}>
      <div className="left-bar">
        <div className={"layers"}>
          <h5>Layers</h5>
          <Layers 
            toggleLayerVisibility={(layerID) => toggleLayerVisibility({variables: { id: layerID }}) } 
            changeLayerSortOrder={(layerID, change) => changeLayerSortOrder({variables: { id: layerID, change: change }})} />
        </div>
        <div className={"timeline"}>
          <Timeline ciid={ciid}></Timeline>
        </div>
      </div>
      <div className="center">
        <MainAreaCI ciid={ciid}></MainAreaCI>
      </div>
    </div>
  );
}

export default Explorer;