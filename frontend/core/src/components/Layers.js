import React from 'react';
import LayerIcon from './LayerIcon';
import { Button, Popover, Radio } from 'antd'
import { Flipper, Flipped } from 'react-flip-toolkit'
import { queries } from 'graphql/queries'
import { useQuery } from '@apollo/client';
import { mergeSettingsAndSortLayers } from 'utils/layers'; 
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faEye, faEyeSlash, faArrowAltCircleUp, faArrowAltCircleDown, faCogs, faPlug, faBan, faEdit, faInfo } from '@fortawesome/free-solid-svg-icons'
import _ from 'lodash';

function Layers(props) {

  function toggleLayerVisibility(layerID, allLayers) {
    if (props.layerSettings) {
      var l = _.find(props.layerSettings, ls => ls.layerID === layerID);
      if (l) {
        props.setLayerSettings(props.layerSettings.map(ls => {
          if (ls.layerID !== layerID) return ls;
          return {...ls, visible: !ls.visible };
        }));
      } else return props.setLayerSettings(_.concat(props.layerSettings, [{layerID: layerID, visible: false, sortOffset: 0}]));
    } else {
      var layerSettings = allLayers.map(l => {
        return { layerID: l.id, sortOffset: 0, visible: l.id !== layerID }
      });
      props.setLayerSettings(layerSettings);
    }
  }

  function changeLayerSortOrder(layerIDA, layerIDB, change, allLayers) {
    // console.log(`Swapping ${layerIDA} and ${layerIDB}`);
    if (props.layerSettings) {
      var newLayerSettings = props.layerSettings.concat();

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

      props.setLayerSettings(newLayerSettings);
    } else {
      var layerSettings = allLayers.map(l => {
        let sortOffset = 0;
        if (l.id === layerIDA) sortOffset += change;
        else if (l.id === layerIDB) sortOffset -= change;
        return { layerID: l.id, sortOffset: sortOffset, visible: true }
      });
      props.setLayerSettings(layerSettings);
    }
  }

  const { error, data } = useQuery(queries.Layers);
    if (data) {
      let layers = mergeSettingsAndSortLayers(data.layers, props.layerSettings);

      return (<ul style={{listStyle: 'none', paddingLeft: '0px', marginBottom: '0px'}}>
        <Flipper flipKey={layers.map(a => a.id + ";" + a.visible).join(' ')}>
        {layers.map((layer, index) => {

          var nextLayer = layers[index + 1];
          var previousLayer = layers[index - 1];

          const layerDescPopup = <Popover
            placement="topRight"
            trigger="click"
            content={layer.description}
            on='click'
            position='top right'
          >
            <Button size='small'><FontAwesomeIcon icon={faInfo} color={"gray"} /></Button>
          </Popover>

          return (
            <Flipped key={layer.id} flipId={layer.id}>
              <li style={{paddingBottom: '5px', display: 'flex'}}>
                
                <span style={{flexGrow: 1}}>
                  {!layer.writable && (<FontAwesomeIcon icon={faBan} />)}
                  {layer.writable && (<FontAwesomeIcon icon={faEdit} />)}
                  &nbsp;
                  <span style={((layer.visible) ? {} : {color: '#ccc'})}>{layerDescPopup} <LayerIcon layer={layer} /> {layer.id} {((layer.state !== 'ACTIVE') ? " (DEPRECATED)" : "")}</span>
                  {layer.brainName !== "" && (<FontAwesomeIcon icon={faCogs} />)}
                  {layer.onlineInboundAdapterName !== "" && (<FontAwesomeIcon icon={faPlug} />)}
                </span>
                &nbsp;&nbsp;
                  <Button size='small' onClick={() => toggleLayerVisibility(layer.id, data.layers)} style={{ marginRight: "0.5rem" }}>
                    <FontAwesomeIcon icon={((layer.visible) ? faEye : faEyeSlash)} color={"grey"} style={{ padding: "2px"}} />
                  </Button>
                <Radio.Group size='small'>
                  <Radio.Button disabled={!previousLayer} onClick={() => changeLayerSortOrder(layer.id, previousLayer.id, 1, data.layers)}>
                    <FontAwesomeIcon icon={faArrowAltCircleUp} color={"grey"} style={{ padding: "2px"}} />
                  </Radio.Button>
                  <Radio.Button disabled={!nextLayer} onClick={() => changeLayerSortOrder(layer.id, nextLayer.id, -1, data.layers)}>
                    <FontAwesomeIcon icon={faArrowAltCircleDown} color={"grey"} style={{ padding: "2px"}} />
                  </Radio.Button>
                </Radio.Group>
              </li>
            </Flipped>)
          })}
        </Flipper>
        </ul>);
    } else if (error) {
      return "Error";
    } else {
      return "Loading";
    }
}

export default Layers;
