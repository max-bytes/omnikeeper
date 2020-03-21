import React, { useState } from "react";
import PropTypes from 'prop-types'
import { useMutation } from '@apollo/react-hooks';
import { withApollo } from 'react-apollo';
import Button from 'react-bootstrap/Button';
import Form from 'react-bootstrap/Form';
import Col from 'react-bootstrap/Col';
import { mutations } from '../graphql/mutations'
import { AttributeTypes, attributeType2InputProps } from '../utils/attributeTypes'
import { Row } from "react-bootstrap";
import { Dropdown } from 'semantic-ui-react'

function AddNewAttribute(props) {

  let initialAttribute = {name: '', type: 'TEXT', value: ''};
  const [selectedLayer, setSelectedLayer] = useState(undefined);
  const [newAttribute, setNewAttribute] = useState(initialAttribute);
  React.useEffect(() => { if (!props.isEditable) setSelectedLayer(undefined); }, [props.isEditable]);
  
  let visibleLayers = props.layers.filter(l => l.visibility).map(l => l.name);

  let addButtons = <div>Add Attribute to Layer: {props.layers.map(layer => {
    return <Button disabled={!props.isEditable} key={layer.name} style={{backgroundColor: layer.color, borderColor: layer.color, color: '#111'}} className={"mx-1"}
    onClick={() => {if (selectedLayer === layer) setSelectedLayer(undefined); else setSelectedLayer(layer);}}>{layer.name}</Button>;
  })}</div>;

  // TODO: loading
  const [insertCIAttribute] = useMutation(mutations.INSERT_CI_ATTRIBUTE, { refetchQueries: ['changesets', 'ci'], awaitRefetchQueries: true });
  const [setSelectedTimeThreshold] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);

  let addAttribute = <span></span>;
  if (selectedLayer) {
    addAttribute = 
      <div style={{backgroundColor: selectedLayer.color, borderColor: selectedLayer.color}} className={"p-2"}>
        <Form onSubmit={e => {
            e.preventDefault();
            insertCIAttribute({ variables: { layers: visibleLayers, ciIdentity: props.ciIdentity, name: newAttribute.name, layerID: selectedLayer.id, value: {
              type: newAttribute.type,
              value: newAttribute.value
            } } }).then(d => {
              setSelectedLayer(undefined);
              setNewAttribute(initialAttribute);
              setSelectedTimeThreshold({ variables:{ newTimeThreshold: null, isLatest: true }});
            });
          }}>
          <Form.Group as={Row} controlId="type">
            <Form.Label column>Type</Form.Label>
            <Col sm={10}>
              <Dropdown placeholder='Select attribute type' fluid search selection value={newAttribute.type}
                onChange={(e, data) => {
                  // we'll clear the value, to be safe, TODO: better value migration between types
                  setNewAttribute({...newAttribute, type: data.value, value: ''});
                }}
                options={AttributeTypes.map(at => { return {key: at.id, value: at.id, text: at.name }; })}
              />
            </Col>
          </Form.Group>
          <Form.Group as={Row} controlId="name">
            <Form.Label column>Name</Form.Label>
            <Col sm={10}>
              <Form.Control type="text" placeholder="Enter name" value={newAttribute.name} onChange={e => setNewAttribute({...newAttribute, name: e.target.value})} />
            </Col>
          </Form.Group>
          <Form.Group as={Row} controlId="value">
            <Form.Label column>Value</Form.Label>
            <Col sm={10}>
              <Form.Control {...attributeType2InputProps(newAttribute.type)} placeholder="Enter value" value={newAttribute.value} onChange={e => setNewAttribute({...newAttribute, value: e.target.value})} />                        
            </Col>
          </Form.Group>
          <Button variant="primary" type="submit">Insert</Button>
        </Form>
      </div>;
  }

  return <div className={"m-2"}>
    {addButtons}
    {addAttribute}
    </div>;
}

AddNewAttribute.propTypes = {
  isEditable: PropTypes.bool.isRequired,
  ciIdentity: PropTypes.string.isRequired,
  layers: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.number.isRequired,
      name: PropTypes.string.isRequired,
      visibility: PropTypes.bool.isRequired,
      color: PropTypes.string.isRequired
    }).isRequired
  ).isRequired
}

export default withApollo(AddNewAttribute);