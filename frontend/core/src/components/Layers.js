import React, { useCallback } from 'react';
import LayerIcon from './LayerIcon';
import { Button, Popover } from 'antd'
import { Flipper, Flipped } from 'react-flip-toolkit'
import { queries } from 'graphql/queries'
import { useQuery } from '@apollo/client';
import { mergeSettingsAndSortLayers } from 'utils/layers'; 
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faEye, faEyeSlash, faArrowAltCircleUp, faArrowAltCircleDown, faCogs, faPlug, faBan, faEdit, faInfo } from '@fortawesome/free-solid-svg-icons'
import _ from 'lodash';

function Layers(props) {

  const {layerSettings,setLayerSettings} = props;

  const toggleLayerVisibility = useCallback((layerID, allLayers) => {
    if (layerSettings) {
      var ls = _.find(layerSettings, ls => ls.layerID === layerID);
      if (ls) {
        setLayerSettings(layerSettings.map(ls => {
          if (ls.layerID !== layerID) return ls;
          return {...ls, visible: !ls.visible };
        }));
      } else {
        setLayerSettings(_.concat(layerSettings, [{layerID: layerID, visible: false, sortOffset: 0}]));
      }
    } else {
      var newLayerSettings = allLayers.map(l => {
        return { layerID: l.id, sortOffset: 0, visible: l.id !== layerID }
      });
      setLayerSettings(newLayerSettings);
    }
  }, [layerSettings, setLayerSettings]);

  const toggleLayerVisibilityAll = useCallback((allLayers) => {
    const safeLayerSettings = layerSettings ?? [];
    const areAllInvisible = safeLayerSettings.every(ls => !ls.visible);
    var newLayerSettings = allLayers.map(l => {
      var ls = _.find(safeLayerSettings, ls => ls.layerID === l.id);
      if (ls) {
        return {...ls, visible: areAllInvisible };
      } else {
        return {layerID: l.id, visible: areAllInvisible, sortOffset: 0};
      }
    });
    setLayerSettings(newLayerSettings);
  }, [layerSettings, setLayerSettings]);

  const changeLayerSortOrder = useCallback((layerIDA, layerIDB, change, allLayers) => {
    // console.log(`Swapping ${layerIDA} and ${layerIDB}`);
    var newLayerSettings;
    if (layerSettings) {
      newLayerSettings = layerSettings.concat();

      var swapLayerA = _.find(newLayerSettings, ls => ls.layerID === layerIDA);
      if (!swapLayerA) {
          swapLayerA = {layerID: layerIDA, sortOffset: 0, visible: true}
          newLayerSettings = _.concat(newLayerSettings, [swapLayerA]);
      }
      var swapLayerB = _.find(newLayerSettings, ls => ls.layerID === layerIDB);
      if (!swapLayerB) {
          swapLayerB = {layerID: layerIDB, sortOffset: 0, visible: true}
          newLayerSettings = _.concat(newLayerSettings, [swapLayerB]);
      }
      newLayerSettings = _.map(newLayerSettings, ls => {
          if (ls.layerID === layerIDA)
              return { ...ls, sortOffset: ls.sortOffset + change };
          else if (ls.layerID === layerIDB)
              return { ...ls, sortOffset: ls.sortOffset - change };
          else
              return ls;
      });

      setLayerSettings(newLayerSettings);
    } else {
      newLayerSettings = allLayers.map(l => {
        let sortOffset = 0;
        if (l.id === layerIDA) sortOffset += change;
        else if (l.id === layerIDB) sortOffset -= change;
        return { layerID: l.id, sortOffset: sortOffset, visible: true }
      });
      setLayerSettings(newLayerSettings);
    }
  }, [layerSettings, setLayerSettings]);

  const { error, data } = useQuery(queries.Layers);
  if (data) {
    let layers = mergeSettingsAndSortLayers(data.layers, layerSettings);

    return <>
        <div style={{paddingBottom: '5px', display: 'flex', alignItems: 'center', justifyContent: 'flex-end'}}>
          <Button size='small' onClick={() => toggleLayerVisibilityAll(data.layers)}>
            <FontAwesomeIcon icon={((true) ? faEye : faEyeSlash)} fixedWidth style={{ padding: "2px"}} />
          </Button>
        </div>
        <Flipper flipKey={layers.map(a => a.id + ";" + a.visible).join(' ')}>
        {layers.map((layer, index) => {

          var nextLayer = layers[index + 1];
          var previousLayer = layers[index - 1];

          let layerText = <span style={{flexGrow: '1', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', color: (layer.visible) ? 'unset': '#ccc'}}>{layer.id} {((layer.state !== 'ACTIVE') ? " (DEPRECATED)" : "")}</span>;
          layerText = <Popover placement="bottom" content={layer.description ? layer.description : <i>No description</i>}>{layerText}</Popover>;
          
          return (
            <Flipped key={layer.id} flipId={layer.id}>
              <div style={{paddingBottom: '5px', display: 'flex', alignItems: 'center'}}>
                {!layer.writable && (<FontAwesomeIcon fixedWidth icon={faBan} />)}
                {layer.writable && (<FontAwesomeIcon fixedWidth icon={faEdit} />)}
                &nbsp;
                <LayerIcon layer={layer} />
                {layerText}
                {layer.clConfigID !== "" && (<FontAwesomeIcon icon={faCogs} fixedWidth />)}
                {layer.onlineInboundAdapterName !== "" && (<FontAwesomeIcon icon={faPlug} fixedWidth />)}
                &nbsp;&nbsp;
                <Button size='small' disabled={!previousLayer} onClick={() => changeLayerSortOrder(layer.id, previousLayer.id, 1, data.layers)}>
                  <FontAwesomeIcon icon={faArrowAltCircleUp} fixedWidth style={{ padding: "2px"}} />
                </Button>
                <Button size='small' disabled={!nextLayer} onClick={() => changeLayerSortOrder(layer.id, nextLayer.id, -1, data.layers)}>
                  <FontAwesomeIcon icon={faArrowAltCircleDown} fixedWidth style={{ padding: "2px"}} />
                </Button>
                <Button size='small' onClick={() => toggleLayerVisibility(layer.id, data.layers)} style={{ marginLeft: "0.5rem" }}>
                  <FontAwesomeIcon icon={((layer.visible) ? faEye : faEyeSlash)} fixedWidth style={{ padding: "2px"}} />
                </Button>
              </div>
            </Flipped>)
          })}
        </Flipper>
      </>;
  } else if (error) {
    return "Error";
  } else {
    return "Loading";
  }
}

export default Layers;
