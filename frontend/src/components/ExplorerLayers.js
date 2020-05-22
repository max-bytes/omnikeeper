
import React from 'react';
import Layers from './Layers';
import { mutations } from '../graphql/mutations'
import { useMutation } from '@apollo/react-hooks';

function ExplorerLayers() {

  // TODO: loading
  const [toggleLayerVisibility] = useMutation(mutations.TOGGLE_LAYER_VISIBILITY);
  const [changeLayerSortOrder] = useMutation(mutations.CHANGE_LAYER_SORT_ORDER);

  return <Layers 
    toggleLayerVisibility={(layerID) => toggleLayerVisibility({variables: { id: layerID }}) } 
    changeLayerSortOrder={(layerID, change) => changeLayerSortOrder({variables: { id: layerID, change: change }})} />;
}

export default ExplorerLayers;