import { mutations } from '../graphql/mutations'
import React from 'react';
import PropTypes from 'prop-types'
import LayerIcon from './LayerIcon';
import { useMutation } from '@apollo/react-hooks';
import { Icon } from 'semantic-ui-react'
import { Button } from 'semantic-ui-react'
import { Flipper, Flipped } from 'react-flip-toolkit'

function Layers(props) {

  // TODO: loading
  const [toggleLayerVisibility] = useMutation(mutations.TOGGLE_LAYER_VISIBILITY);
  const [changeLayerSortOrder] = useMutation(mutations.CHANGE_LAYER_SORT_ORDER);

  return (<ul style={{listStyle: 'none', paddingLeft: '0px', marginBottom: '0px'}}>
    <Flipper flipKey={props.layers.map(a => a.id + ";" + a.visibility).join(' ')}>
    {props.layers.map(layer => (
      <Flipped key={layer.id} flipId={layer.id}>
        <li style={{paddingBottom: '5px', display: 'flex'}}>
          <LayerIcon layer={layer}></LayerIcon>
          
            <Icon.Group>
              {!layer.writable && (<Icon fitted disabled name='dont' />)}
              {layer.writable && (<Icon fitted name='pencil' />)}
              {/* <Icon fitted name={'pencil'} disabled={!layer.writable} /> */}
            </Icon.Group>&nbsp;
            <span style={{flexGrow: 1}}>
              <span style={((layer.visibility) ? {} : {color: '#ccc'})}>{layer.name}</span>
            </span>
            &nbsp;&nbsp;
              <Button basic size='mini' compact onClick={() => toggleLayerVisibility({variables: { id: layer.id }})}>
                <Icon fitted name={((layer.visibility) ? 'eye' : 'eye slash')} />
              </Button>
            <Button.Group basic size='mini'>
              <Button compact onClick={() => changeLayerSortOrder({variables: { id: layer.id, change: 1 }})}>
                <Icon fitted name='arrow alternate circle up' />
              </Button>
              <Button compact onClick={() => changeLayerSortOrder({variables: { id: layer.id, change: -1 }})}>
                <Icon fitted name='arrow alternate circle down' />
              </Button>
            </Button.Group>
        </li>
      </Flipped>))}
    </Flipper>
    </ul>);
}

Layers.propTypes = {
  layers: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.number.isRequired,
      name: PropTypes.string.isRequired,
      visibility: PropTypes.bool.isRequired,
      color: PropTypes.string.isRequired
    }).isRequired
  ).isRequired,
}

export default Layers;