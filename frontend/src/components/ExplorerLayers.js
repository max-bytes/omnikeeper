import React from 'react';
import Layers from './Layers';
import { mutations } from 'graphql/mutations'
import { queries } from 'graphql/queries'
import { useMutation, useQuery } from '@apollo/react-hooks';

function ExplorerLayers() {
  var { data: { visibleLayers } } = useQuery(queries.VisibleLayers);
  var { data: { layerSortOffsets } } = useQuery(queries.LayerSortOffsets);
  const [setVisibleLayers] = useMutation(mutations.SET_VISIBLE_LAYERS);
  const [setLayerSortOffsets] = useMutation(mutations.SET_LAYER_SORT_OFFSETS);

  return <Layers visibleLayers={visibleLayers} layerSortOffsets={layerSortOffsets} 
    onSetVisibleLayers={ newHLs => setVisibleLayers({variables: {ids: newHLs}}) }
    onSetLayerSortOffsets={ newLSOs => setLayerSortOffsets({variables: {offsets: newLSOs}})}
    />
}

export default ExplorerLayers;