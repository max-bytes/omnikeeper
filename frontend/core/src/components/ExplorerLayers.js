import React from 'react';
import Layers from './Layers';
import { useLocalStorage } from 'utils/useLocalStorage';

function ExplorerLayers() {
  const [layerSettings, setLayerSettings] = useLocalStorage('layerSettings', null);

  return <Layers layerSettings={layerSettings} 
    setLayerSettings={ newLS => {
      setLayerSettings(newLS);
    }} />
}

export default ExplorerLayers;