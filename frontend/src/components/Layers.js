
import React from 'react';
import LayerIcon from './LayerIcon';
import { Icon } from 'semantic-ui-react'
import { Button } from 'semantic-ui-react'
import { Flipper, Flipped } from 'react-flip-toolkit'
import { useLayers } from '../utils/useLayers'

function Layers(props) {

  const { data: layers } = useLayers();

  return (<ul style={{listStyle: 'none', paddingLeft: '0px', marginBottom: '0px'}}>
    <Flipper flipKey={layers.map(a => a.id + ";" + a.visibility).join(' ')}>
    {layers.map(layer => (
      <Flipped key={layer.id} flipId={layer.id}>
        <li style={{paddingBottom: '5px', display: 'flex'}}>
          <LayerIcon layer={layer}></LayerIcon>
          
            <Icon.Group>
              {!layer.writable && (<Icon fitted disabled name='dont' />)}
              {layer.writable && (<Icon fitted name='pencil' />)}
              {/* <Icon fitted name={'pencil'} disabled={!layer.writable} /> */}
            </Icon.Group>&nbsp;
            <span style={{flexGrow: 1}}>
              <span style={((layer.visibility) ? {} : {color: '#ccc'})}>{layer.name} {((layer.state !== 'ACTIVE') ? " (DEPRECATED)" : "")}</span>
              {layer.brainName !== "" && (<Icon fitted name='lightning' />)}
            </span>
            &nbsp;&nbsp;
              <Button basic size='mini' compact onClick={() => props.toggleLayerVisibility(layer.id)}>
                <Icon fitted name={((layer.visibility) ? 'eye' : 'eye slash')} />
              </Button>
            <Button.Group basic size='mini'>
              <Button compact onClick={() => props.changeLayerSortOrder(layer.id, 1)}>
                <Icon fitted name='arrow alternate circle up' />
              </Button>
              <Button compact onClick={() => props.changeLayerSortOrder(layer.id, -1)}>
                <Icon fitted name='arrow alternate circle down' />
              </Button>
            </Button.Group>
        </li>
      </Flipped>))}
    </Flipper>
    </ul>);
}

export default Layers;