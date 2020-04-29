import React, { useState } from "react";
import PropTypes from 'prop-types'
import { useMutation } from '@apollo/react-hooks';
import { withApollo } from 'react-apollo';
import Form from 'react-bootstrap/Form';
import { mutations } from '../graphql/mutations'
import Button from 'react-bootstrap/Button';
import LayerStackIcons from "./LayerStackIcons";
import ChangesetPopup from "./ChangesetPopup";
import EditableAttributeValue from "./EditableAttributeValue";


function Attribute(props) {

  var {ciIdentity, layers, attribute, isEditable, ...rest} = props;
  

  const isArray = attribute.attribute.value.isArray;

  var [hasErrors, setHasErrors] = useState(false);
  const [values, setValues] = useState(attribute.attribute.value.values);
  React.useEffect(() => setValues(attribute.attribute.value.values), [attribute.attribute.value.values])

  // TODO: loading
  const [insertCIAttribute] = useMutation(mutations.INSERT_CI_ATTRIBUTE);
  const [removeCIAttribute] = useMutation(mutations.REMOVE_CI_ATTRIBUTE, {
    update: (cache, data) => {
      /* HACK: find a better way to deal with cache invalidation! We would like to invalidate the affected CIs, which 
      translates to multiple entries in the cache, because each CI can be cached multiple times for each layerhash
      */
      // data.data.mutate.affectedCIs.forEach(ci => {
      //   var id = props.client.cache.identify(ci);
      //   console.log("Evicting: " + id);
      //   cache.evict(id);
      // });
    }
  });
  const [setSelectedTimeThreshold] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);

  let input;

  const layerID = props.attribute.layerStackIDs[props.attribute.layerStackIDs.length - 1];

  let valueInput = <>
    <EditableAttributeValue name={attribute.attribute.name} setHasErrors={setHasErrors} isEditable={isEditable} values={values} setValues={setValues} type={attribute.attribute.value.type} isArray={isArray} />
  </>;

  const leftPart = <div style={{display: 'flex', flexBasis: '220px', alignItems: 'center', minHeight: '33px'}}>
    <LayerStackIcons layerStack={attribute.layerStack} />
    <ChangesetPopup changesetID={attribute.attribute.changesetID} />
    <Form.Label className={"pr-1"} style={{whiteSpace: 'nowrap', flexGrow: 1, justifyContent: 'flex-end'}}>{attribute.attribute.name}</Form.Label>
  </div>;

  if (isEditable) {
    const removeButton = (
      <Button variant="danger" onClick={e => {
        e.preventDefault();
        removeCIAttribute({ variables: { ciIdentity: props.ciIdentity, name: attribute.attribute.name, layerID } })
        .then(d => setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true }}));
      }}>Remove</Button>
    );

    input = (
      <Form inline style={{alignItems: 'flex-start', flexFlow: 'row'}} onSubmit={e => {
          e.preventDefault();
          insertCIAttribute({ variables: { ciIdentity: props.ciIdentity, name: attribute.attribute.name, layerID, value: {
            type: attribute.attribute.value.type,
            values: values,
            isArray: isArray
          } } })
          .then(d => setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true }}));
        }}>
          {leftPart}
          <Form.Group controlId={`value:${attribute.attribute.name}`} style={{flexGrow: 1, alignItems: 'flex-start'}}>
            {valueInput}
            <Button type="submit" className={'mx-1'} disabled={attribute.attribute.value.values === values || hasErrors}>Update</Button>
            {removeButton}
          </Form.Group>
      </Form>
    );
  } else {
    input = (<Form inline style={{alignItems: 'flex-start'}} >
      {leftPart}
      <Form.Group controlId={`value:${attribute.attribute.name}`} style={{flexGrow: 1}}>
        {valueInput}
      </Form.Group>
    </Form>);
  }


  return (
    <div key={attribute.attribute.name} {...rest}>
      {input}
    </div>
  );
}

Attribute.propTypes = {
  isEditable: PropTypes.bool.isRequired,
  ciIdentity: PropTypes.string.isRequired,
  attribute: PropTypes.shape({
      attribute: PropTypes.shape({
        name: PropTypes.string.isRequired,
        state: PropTypes.string.isRequired,
        value: PropTypes.shape({
          type: PropTypes.string.isRequired,
          isArray: PropTypes.bool.isRequired,
          values: PropTypes.arrayOf(PropTypes.string).isRequired
      })
      })
    }).isRequired
}

export default withApollo(Attribute);