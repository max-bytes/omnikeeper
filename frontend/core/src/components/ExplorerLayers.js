import React, { useContext } from "react";
import Layers from './Layers';
import { LayerSettingsContext } from "utils/layers";

export function ExplorerLayers() {
  const { layerSettings, setLayerSettings} = useContext(LayerSettingsContext);

  return <Layers layerSettings={layerSettings} setLayerSettings={ newLS => setLayerSettings(newLS) } />
}
