import React, { useState } from "react";
import PropTypes from 'prop-types'
import { useMutation } from '@apollo/react-hooks';
import { withApollo } from 'react-apollo';
import Form from 'react-bootstrap/Form';
import { mutations } from '../graphql/mutations'
import Button from 'react-bootstrap/Button';
import { attributeTypename2Object, attribute2InputType } from '../utils/attributeTypes'
import LayerStackIcons from "./LayerStackIcons";
import ChangesetPopup from "./ChangesetPopup";

function Attribute(props) {

  var {ciIdentity, layers, attribute, isEditable, ...rest} = props;

  const [value, setValue] = useState(props.attribute.value.value);
  React.useEffect(() => setValue(props.attribute.value.value), [props.attribute.value.value])

  let visibleLayers = props.layers.filter(l => l.visibility).map(l => l.name);

  // TODO: loading
  const [insertCIAttribute] = useMutation(mutations.INSERT_CI_ATTRIBUTE, { refetchQueries: ['changesets', 'ci'], awaitRefetchQueries: true });
  const [removeCIAttribute] = useMutation(mutations.REMOVE_CI_ATTRIBUTE, { 
    refetchQueries: ['changesets', 'ci'], awaitRefetchQueries: true,
    update: (cache, data) => {
      /* HACK: find a better way to deal with cache invalidation! We would like to invalidate the affected CIs, which 
      translates to multiple entries in the cache, because each CI can be cached multiple times for each layerhash
      */
      data.data.mutate.affectedCIs.forEach(ci => {
        var id = props.client.cache.identify(ci);
        console.log("Evicting: " + id);
        cache.evict(id);
      });
    }
  });
  const [setSelectedTimeThreshold] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);

  let input;

  if (isEditable) {

    let removeButton = (
      <Button variant="danger" onClick={e => {
        e.preventDefault();
        removeCIAttribute({ variables: { layers: visibleLayers, ciIdentity: props.ciIdentity, name: props.attribute.name, layerID: props.attribute.layerID } })
        .then(d => setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true }}));
      }}>Remove</Button>
    );
    
    input = (
      <Form inline onSubmit={e => {
          e.preventDefault();
          insertCIAttribute({ variables: { layers: visibleLayers, ciIdentity: props.ciIdentity, name: props.attribute.name, layerID: props.attribute.layerID, value: {
            type: attributeTypename2Object(props.attribute.value.__typename).id,
            value: value
          } } })
          .then(d => setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true }}));
        }} >
          <LayerStackIcons layerStack={props.attribute.layerStack}></LayerStackIcons>
          <ChangesetPopup changesetID={props.attribute.changesetID} />
          <Form.Group controlId={`value:${props.attribute.name}`} style={{flexGrow: 1}}>
            <Form.Label className={"pr-1"} style={{flexBasis: '160px', justifyContent: 'flex-start', whiteSpace: 'nowrap'}}>{props.attribute.name}:</Form.Label>
            <Form.Control style={{flexGrow: 1}} type={attribute2InputType(attributeTypename2Object(props.attribute.value.__typename))} placeholder="Enter value" value={value} onChange={e => setValue(e.target.value)} />
            <Button type="submit" className={'mx-1'} disabled={props.attribute.value.value === value}>Update</Button>
            {removeButton}
          </Form.Group>
      </Form>
    );
  } else {
    input = (<Form inline>
      <LayerStackIcons layerStack={props.attribute.layerStack}></LayerStackIcons>
      <ChangesetPopup changesetID={props.attribute.changesetID} />
      <Form.Group controlId={`value:${props.attribute.name}`} style={{flexGrow: 1}}>
        <Form.Label className={"pr-1"} style={{flexBasis: '160px', justifyContent: 'flex-start', whiteSpace: 'nowrap'}}>{props.attribute.name}:</Form.Label>
        <Form.Control style={{flexGrow: 1}} type={attribute2InputType(attributeTypename2Object(props.attribute.value.__typename))} placeholder="Enter value" value={value} readOnly />
      </Form.Group>
    </Form>);
  }


  return (
    <div key={props.attribute.name} style={{margin: "5px"}} {...rest}>
      {input}
    </div>
  );
}

Attribute.propTypes = {
  isEditable: PropTypes.bool.isRequired,
  ciIdentity: PropTypes.string.isRequired,
  layers: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.number.isRequired,
      name: PropTypes.string.isRequired,
      visibility: PropTypes.bool.isRequired
    }).isRequired
  ).isRequired,
  attribute: PropTypes.shape({
      name: PropTypes.string.isRequired,
      layerID: PropTypes.number.isRequired,
      layer: PropTypes.shape({
        color: PropTypes.string.isRequired
      }),
      state: PropTypes.string.isRequired,
      value: PropTypes.shape({
        __typename: PropTypes.string.isRequired,
        value: PropTypes.oneOfType([
          PropTypes.string,
          PropTypes.number,
          PropTypes.bool
        ]).isRequired
      })
    }).isRequired
}

export default withApollo(Attribute);