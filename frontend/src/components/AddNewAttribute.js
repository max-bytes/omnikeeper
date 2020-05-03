import React, { useState } from "react";
import PropTypes from 'prop-types'
import { useMutation } from '@apollo/react-hooks';
import { withApollo } from 'react-apollo';
//import Button from 'react-bootstrap/Button';
import Form from 'react-bootstrap/Form';
import Col from 'react-bootstrap/Col';
import { mutations } from '../graphql/mutations'
import { AttributeTypes } from '../utils/attributeTypes'
import { Row } from "react-bootstrap";
import EditableAttributeValue from "./EditableAttributeValue";
import { Dropdown, Segment, Button, Icon } from 'semantic-ui-react'
import LayerDropdown from "./LayerDropdown";
import { ErrorPopupButton } from "./ErrorPopupButton";
import { useLayers } from '../utils/useLayers';

function AddNewAttribute(props) {
  const [insertError, setInsertError] = useState(undefined);
  const { data: visibleAndWritableLayers } = useLayers(true, true);
  const canBeEdited = props.isEditable && visibleAndWritableLayers.length > 0;
  let initialAttribute = {name: '', type: 'TEXT', values: [''], isArray: false};
  const [selectedLayer, setSelectedLayer] = useState(visibleAndWritableLayers[0]);
  const [isOpen, setOpen] = useState(false);
  const [newAttribute, setNewAttribute] = useState(initialAttribute);
  const [valueAutofocussed, setValueAutofocussed] = useState(false);
  var [hasErrors, setHasErrors] = useState(false);
  React.useEffect(() => { if (!canBeEdited) setOpen(false); }, [canBeEdited]);

  React.useEffect(() => {if (props.prefilled) {
    setOpen(true);
    setSelectedLayer(s => s ?? props.prefilled.layer);
    setNewAttribute({name: props.prefilled.name, type: props.prefilled.type, values: props.prefilled.values ?? [''], isArray: props.prefilled.isArray ?? false});
    setValueAutofocussed(true);
  }}, [props.prefilled]);

  // TODO: loading
  const [insertCIAttribute] = useMutation(mutations.INSERT_CI_ATTRIBUTE);
  const [setSelectedTimeThreshold] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);

  let addButton = <Button disabled={!canBeEdited} onClick={() => setOpen(!isOpen)} icon primary labelPosition='left'>
      <Icon name='plus' />Add Attribute
    </Button>;

  let addAttribute = <></>;
  if (isOpen) {
    addAttribute = 
      <Segment raised>
        <Form onSubmit={e => {
            e.preventDefault();
            setInsertError(undefined);
            insertCIAttribute({ variables: { layers: visibleAndWritableLayers.map(l => l.name), ciIdentity: props.ciIdentity, name: newAttribute.name, layerID: selectedLayer.id, value: {
              type: newAttribute.type,
              isArray: newAttribute.isArray,
              values: newAttribute.values
            } } }).then(d => {
              setOpen(false);
              setNewAttribute(initialAttribute);
              setSelectedTimeThreshold({ variables:{ newTimeThreshold: null, isLatest: true }});
            }).catch(e => {
              setInsertError(e);
            });
          }}>

          <Form.Group as={Row} controlId="layer">
            <Form.Label column>Layer</Form.Label>
            <Col sm={10}>
              <LayerDropdown layers={visibleAndWritableLayers} selectedLayer={selectedLayer} onSetSelectedLayer={l => setSelectedLayer(l)} />
            </Col>
          </Form.Group>

          <Form.Group as={Row} controlId="type">
            <Form.Label column>Type</Form.Label>
            <Col sm={8}>
              <Dropdown placeholder='Select attribute type' fluid search selection value={newAttribute.type}
                onChange={(e, data) => {
                  // we'll clear the value, to be safe, TODO: better value migration between types
                  setNewAttribute({...newAttribute, type: data.value, value: ''});
                }}
                options={AttributeTypes.map(at => { return {key: at.id, value: at.id, text: at.name }; })}
              />
            </Col>
            <Col sm={2}>
              <Form.Group style={{height: '100%'}} controlId="isArray">
                <Form.Check style={{height: '100%'}} inline type="checkbox" label="Is Array" checked={newAttribute.isArray} onChange={e => setNewAttribute({...newAttribute, values: [''], isArray: e.target.checked })} />
              </Form.Group>
            </Col>
          </Form.Group>
          <Form.Group as={Row} controlId="name">
            <Form.Label column>Name</Form.Label>
            <Col sm={10}>
              <Form.Control type="text" placeholder="Enter name" value={newAttribute.name} onChange={e => setNewAttribute({...newAttribute, name: e.target.value})} />
            </Col>
          </Form.Group>
          <Form.Group as={Row} controlId="value">
            <Form.Label column>{((newAttribute.isArray) ? 'Values' : 'Value')}</Form.Label>
            <Col sm={10}>
              <EditableAttributeValue setHasErrors={setHasErrors} name={'newAttribute'} autoFocus={valueAutofocussed} values={newAttribute.values} setValues={vs => setNewAttribute({...newAttribute, values: vs})} type={newAttribute.type} isArray={newAttribute.isArray} />
            </Col>
          </Form.Group>
          <Button secondary className="mr-2" onClick={() => setOpen(false)}>Cancel</Button>
          <Button primary type="submit" disabled={hasErrors || !newAttribute.name}>Insert</Button>
          <ErrorPopupButton error={insertError} />
        </Form>
      </Segment>;
  }

  return <div className={"mb-4"}>
    {!isOpen && addButton}
    {addAttribute}
    </div>;
}

AddNewAttribute.propTypes = {
  isEditable: PropTypes.bool.isRequired,
  ciIdentity: PropTypes.string.isRequired
}

export default withApollo(AddNewAttribute);