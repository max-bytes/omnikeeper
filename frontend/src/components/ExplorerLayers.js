import React from 'react';
import Layers from './Layers';
import { mutations } from '../graphql/mutations'
import { useMutation } from '@apollo/react-hooks';
import { useLayers } from '../utils/useLayers'

function ExplorerLayers() {

  const { data: layers } = useLayers();
  const [toggleLayerVisibility] = useMutation(mutations.TOGGLE_LAYER_VISIBILITY);
  const [changeLayerSortOrder] = useMutation(mutations.CHANGE_LAYER_SORT_ORDER);

  return <Layers layers={layers}
    toggleLayerVisibility={(layerID) => toggleLayerVisibility({variables: { id: layerID }}) } 
    changeLayerSortOrder={(layerIDA, layerIDB, change) => changeLayerSortOrder({variables: { layerIDA: layerIDA, layerIDB: layerIDB, change: change }})} />;
}

export default ExplorerLayers;