import React from 'react';
import Layers from './Layers';
import { mutations } from 'graphql/mutations'
import { queries } from 'graphql/queries'
import { useMutation, useQuery } from '@apollo/react-hooks';

function ExplorerLayers() {
  var { data: { hiddenLayers } } = useQuery(queries.HiddenLayers);
  var { data: { layerSortOffsets } } = useQuery(queries.LayerSortOffsets);
  const [setHiddenLayers] = useMutation(mutations.SET_HIDDEN_LAYERS);
  const [setLayerSortOffsets] = useMutation(mutations.SET_LAYER_SORT_OFFSETS);

  return <Layers hiddenLayers={hiddenLayers} layerSortOffsets={layerSortOffsets} 
    onSetHiddenLayers={ newHLs => setHiddenLayers({variables: {ids: newHLs}}) }
    onSetLayerSortOffsets={ newLSOs => setLayerSortOffsets({variables: {offsets: newLSOs}})}
    />
}

export default ExplorerLayers;