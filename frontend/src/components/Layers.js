
import React, {useState, useEffect} from 'react';
import LayerIcon from './LayerIcon';
import { Icon } from 'semantic-ui-react'
import { Button } from 'semantic-ui-react'
import { Flipper, Flipped } from 'react-flip-toolkit'
import { mutations } from 'graphql/mutations'
import { queries } from 'graphql/queries'
import { useMutation, useQuery } from '@apollo/react-hooks';
import { mergeAndSortLayers } from 'utils/layers'; 
import _ from 'lodash';

function Layers(props) {

  function toggleLayerVisibility(layerID) {
    if (props.hiddenLayers.includes(layerID))
      return props.onSetHiddenLayers(_.without(props.hiddenLayers, layerID));
    else
      return props.onSetHiddenLayers(_.concat(props.hiddenLayers, [layerID]));
  }

  function changeLayerSortOrder(layerIDA, layerIDB, change) {
    var newLayerSortings = props.layerSortOffsets.concat();
    // console.log(`Swapping ${layerIDA} and ${layerIDB}`);

    var swapLayerA = _.find(newLayerSortings, ls => ls.layerID === layerIDA);
    if (!swapLayerA) {
        swapLayerA = {layerID: layerIDA, sortOffset: 0}
        newLayerSortings = _.concat(newLayerSortings, [swapLayerA]);
    }
    var swapLayerB = _.find(newLayerSortings, ls => ls.layerID === layerIDB);
    if (!swapLayerB) {
        swapLayerB = {layerID: layerIDB, sortOffset: 0}
        newLayerSortings = _.concat(newLayerSortings, [swapLayerB]);
    }
    newLayerSortings = _.map(newLayerSortings, ls => {
        if (ls.layerID === layerIDA)
            return { ...ls, sortOffset: ls.sortOffset + change };
        else if (ls.layerID === layerIDB)
            return { ...ls, sortOffset: ls.sortOffset - change };
        else
            return ls;
    });

    props.onSetLayerSortOffsets(newLayerSortings);
  }

  const { error, data, loading } = useQuery(queries.Layers);
    if (data) {
      let layers = mergeAndSortLayers(data.layers, props.hiddenLayers, props.layerSortOffsets);

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
                    {layer.brainName !== "" && (<Icon fitted name='lightning' />)}
                  </span>
                  &nbsp;&nbsp;
                    <Button basic size='mini' compact onClick={() => toggleLayerVisibility(layer.id)}>
                      <Icon fitted name={((layer.visible) ? 'eye' : 'eye slash')} />
                    </Button>
                  <Button.Group basic size='mini'>
                    <Button compact disabled={!previousLayer} onClick={() => changeLayerSortOrder(layer.id, previousLayer.id, 1)}>
                      <Icon fitted name='arrow alternate circle up' />
                    </Button>
                    <Button compact disabled={!nextLayer} onClick={() => changeLayerSortOrder(layer.id, nextLayer.id, -1)}>
                      <Icon fitted name='arrow alternate circle down' />
                    </Button>
                  </Button.Group>
              </li>
            </Flipped>)
          })}
        </Flipper>
        </ul>);
    } else {
      return "Loading";
    }
}

export default Layers;
