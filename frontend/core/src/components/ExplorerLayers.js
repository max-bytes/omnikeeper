import React from 'react';
import Layers from './Layers';
import { mutations } from 'graphql/mutations'
import { queries } from 'graphql/queries'
import { useMutation, useQuery } from '@apollo/client';

function ExplorerLayers() {
  var { data: { layerSettings }, loading } = useQuery(queries.LayerSettings, {fetchPolicy: 'cache-only'});
  const [setLayerSettings] = useMutation(mutations.SET_LAYER_SETTINGS);

  if (loading) return "Loading";

  return <Layers layerSettings={layerSettings} 
    setLayerSettings={ newLS => setLayerSettings({variables: {layerSettings: newLS}})} />
}

export default ExplorerLayers;