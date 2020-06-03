import React from 'react';
import LayerIcon from './LayerIcon';
import { Icon } from 'semantic-ui-react'
import { Button } from 'semantic-ui-react'
import { Flipper, Flipped } from 'react-flip-toolkit'
import { queries } from 'graphql/queries'
import { useQuery } from '@apollo/react-hooks';
import { mergeSettingsAndSortLayers } from 'utils/layers'; 
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCogs } from '@fortawesome/free-solid-svg-icons'
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

          return (
            <Flipped key={layer.id} flipId={layer.id}>
              <li style={{paddingBottom: '5px', display: 'flex'}}>
                <LayerIcon layer={layer}></LayerIcon>
                
                  <Icon.Group>
                    {!layer.writable && (<Icon fitted disabled name='dont' />)}
                    {layer.writable && (<Icon fitted name='pencil' />)}
                    {/* <Icon fitted name={'pencil'} disabled={!layer.writable} /> */}
                  </Icon.Group>&nbsp;
                  <span style={{flexGrow: 1}}>
                    <span style={((layer.visible) ? {} : {color: '#ccc'})}>{layer.name} {((layer.state !== 'ACTIVE') ? " (DEPRECATED)" : "")}</span>
                    {layer.brainName !== "" && (<FontAwesomeIcon icon={faCogs} />)}
                  </span>
                  &nbsp;&nbsp;
                    <Button basic size='mini' compact onClick={() => toggleLayerVisibility(layer.id, data.layers)}>
                      <Icon fitted name={((layer.visible) ? 'eye' : 'eye slash')} />
                    </Button>
                  <Button.Group basic size='mini'>
                    <Button compact disabled={!previousLayer} onClick={() => changeLayerSortOrder(layer.id, previousLayer.id, 1, data.layers)}>
                      <Icon fitted name='arrow alternate circle up' />
                    </Button>
                    <Button compact disabled={!nextLayer} onClick={() => changeLayerSortOrder(layer.id, nextLayer.id, -1, data.layers)}>
                      <Icon fitted name='arrow alternate circle down' />
                    </Button>
                  </Button.Group>
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
