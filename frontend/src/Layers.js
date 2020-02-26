import { gql } from '@apollo/client';
import React from 'react';
import {Mutation} from '@apollo/react-components';
import Button from 'react-bootstrap/Button';
import PropTypes from 'prop-types'
import LayerIcon from './LayerIcon';

function Layers(props) {

  const TOGGLE_LAYER_VISIBILITY = gql`
    mutation ToggleLayerVisibility($id: Int!) {
      toggleLayerVisibility(id: $id) @client
    }
  `;

  const CHANGE_LAYER_SORT_ORDER = gql`
    mutation ChangeLayerSortOrder($id: Int!, $change: Int!) {
      changeLayerSortOrder(id: $id, change: $change) @client
    }
  `;


  return (<ul style={{listStyle: 'none', paddingLeft: '0px', marginBottom: '0px', margin: '5px'}}>
    {props.layers.map(layer => (
    <li key={layer.id}>
      <LayerIcon layer={layer}></LayerIcon>
        {layer.name}&nbsp;{layer.visibility ? 'visible' : 'hidden'}
        &nbsp;
        <Mutation mutation={TOGGLE_LAYER_VISIBILITY} variables={{ id: layer.id }}>
          {toggleLayerVisibility => (
            <Button size="sm" variant="link" onClick={toggleLayerVisibility}>
              {layer.visibility ? 'hide' : 'show'}
            </Button>
          )}
        </Mutation>
        <Mutation mutation={CHANGE_LAYER_SORT_ORDER} variables={{ id: layer.id, change: 1 }}>
          {changeLayerSortOrder => (
            <Button size="sm" variant="link" onClick={changeLayerSortOrder}>
              up
            </Button>
          )}
        </Mutation>
        <Mutation mutation={CHANGE_LAYER_SORT_ORDER} variables={{ id: layer.id, change: -1 }}>
          {changeLayerSortOrder => (
            <Button size="sm" variant="link" onClick={changeLayerSortOrder}>
              down
            </Button>
          )}
        </Mutation>

    </li>))}
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