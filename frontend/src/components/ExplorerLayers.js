import React from 'react';
import Layers from './Layers';
import { mutations } from 'graphql/mutations'
import { queries } from 'graphql/queries'
import { useMutation, useQuery } from '@apollo/react-hooks';

function ExplorerLayers() {
  var { data: { layerSettings } } = useQuery(queries.LayerSettings);
  const [setLayerSettings] = useMutation(mutations.SET_LAYER_SETTINGS);

  return <Layers layerSettings={layerSettings} 
    setLayerSettings={ newLS => setLayerSettings({variables: {layerSettings: newLS}})} />
}

export default ExplorerLayers;