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

  const [value, setValue] = useState(attribute.attribute.value.value);
  React.useEffect(() => setValue(attribute.attribute.value.value), [attribute.attribute.value.value])

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

  const layerID = props.attribute.layerStackIDs[props.attribute.layerStackIDs.length - 1];

  if (isEditable) {

    let removeButton = (
      <Button variant="danger" onClick={e => {
        e.preventDefault();
        removeCIAttribute({ variables: { layers: visibleLayers, ciIdentity: props.ciIdentity, name: attribute.attribute.name, layerID } })
        .then(d => setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true }}));
      }}>Remove</Button>
    );
    
    input = (
      <Form inline onSubmit={e => {
          e.preventDefault();
          insertCIAttribute({ variables: { layers: visibleLayers, ciIdentity: props.ciIdentity, name: attribute.attribute.name, layerID: layerID, value: {
            type: attributeTypename2Object(attribute.attribute.value.__typename).id,
            value: value
          } } })
          .then(d => setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true }}));
        }} >
          <LayerStackIcons layerStack={attribute.layerStack}></LayerStackIcons>
          <ChangesetPopup changesetID={attribute.attribute.changesetID} />
          <Form.Group controlId={`value:${attribute.attribute.name}`} style={{flexGrow: 1}}>
            <Form.Label className={"pr-1"} style={{flexBasis: '160px', justifyContent: 'flex-start', whiteSpace: 'nowrap'}}>{attribute.attribute.name}:</Form.Label>
            <Form.Control style={{flexGrow: 1}} type={attribute2InputType(attributeTypename2Object(attribute.attribute.value.__typename))} placeholder="Enter value" value={value} onChange={e => setValue(e.target.value)} />
            <Button type="submit" className={'mx-1'} disabled={attribute.attribute.value.value === value}>Update</Button>
            {removeButton}
          </Form.Group>
      </Form>
    );
  } else {
    input = (<Form inline>
      <LayerStackIcons layerStack={attribute.layerStack}></LayerStackIcons>
      <ChangesetPopup changesetID={attribute.attribute.changesetID} />
      <Form.Group controlId={`value:${attribute.attribute.name}`} style={{flexGrow: 1}}>
        <Form.Label className={"pr-1"} style={{flexBasis: '160px', justifyContent: 'flex-start', whiteSpace: 'nowrap'}}>{attribute.attribute.name}:</Form.Label>
        <Form.Control style={{flexGrow: 1}} type={attribute2InputType(attributeTypename2Object(attribute.attribute.value.__typename))} placeholder="Enter value" value={value} readOnly />
      </Form.Group>
    </Form>);
  }


  return (
    <div key={attribute.attribute.name} style={{margin: "5px"}} {...rest}>
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
      attribute: PropTypes.shape({
        name: PropTypes.string.isRequired,
        state: PropTypes.string.isRequired,
        value: PropTypes.shape({
          __typename: PropTypes.string.isRequired,
          value: PropTypes.oneOfType([
            PropTypes.string,
            PropTypes.number,
            PropTypes.bool
          ]).isRequired
        })
      })
    }).isRequired
}

export default withApollo(Attribute);