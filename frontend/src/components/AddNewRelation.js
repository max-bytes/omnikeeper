import React, { useState } from "react";
import { useQuery } from '@apollo/client';
import PropTypes from 'prop-types'
import { useMutation } from '@apollo/react-hooks';
import { withApollo } from 'react-apollo';
import Form from 'react-bootstrap/Form';
import Col from 'react-bootstrap/Col';
import { mutations } from '../graphql/mutations'
import { queries } from '../graphql/queries'
import { Row } from "react-bootstrap";
import LayerIcon from "./LayerIcon";
import { Dropdown, Button, Icon, Segment } from 'semantic-ui-react';
import LayerDropdown from "./LayerDropdown";

function AddNewRelation(props) {
  const canBeEdited = props.isEditable && props.visibleAndWritableLayers.length > 0;
  let initialRelation = {predicateID: null, toCIID: null };
  const [selectedLayer, setSelectedLayer] = useState(props.visibleAndWritableLayers[0]);
  const [isOpen, setOpen] = useState(false);
  const [newRelation, setNewRelation] = useState(initialRelation);
  React.useEffect(() => { if (!canBeEdited) setOpen(false); }, [canBeEdited]);

  // TODO: loading
  const { data: dataCIs } = useQuery(queries.CIList);
  const { data: dataPredicates } = useQuery(queries.PredicateList);
  const [insertRelation] = useMutation(mutations.INSERT_RELATION);
  const [setSelectedTimeThreshold] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);

  // let addButtons = <div>Add Relation to Layer: {props.visibleAndWritableLayers.map(layer => {
  //   return <Button disabled={!props.isEditable} key={layer.name} style={{backgroundColor: layer.color, borderColor: layer.color, color: '#111'}} className={"mx-1"}
  //   onClick={() => {if (selectedLayer === layer) setSelectedLayer(undefined); else setSelectedLayer(layer);}}>{layer.name}</Button>;
  // })}</div>;

  const addButton = <Button disabled={!canBeEdited} onClick={() => setOpen(!isOpen)} icon primary labelPosition='left'>
    <Icon name='plus' />Add Relation
  </Button>
  
  let addRelation = <span></span>;
  if (isOpen && dataCIs && dataPredicates) {
    var ciList = dataCIs.ciids.map(d => {
      return { key: d, value: d, text: d };
    });
    var predicateList = dataPredicates.predicates.map(d => {
      return { key: d.id, value: d.id, text: d.wordingFrom };
    });

    addRelation = 
      <Segment raised>
        <Form onSubmit={e => {
            e.preventDefault();
            insertRelation({ variables: { fromCIID: props.ciIdentity, toCIID: newRelation.toCIID, predicateID: newRelation.predicateID, layerID: selectedLayer.id} }).then(d => {
              setOpen(false);
              setNewRelation(initialRelation);
              setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true }});
            });
          }}>

          <Form.Group as={Row} controlId="layer">
            <Form.Label column>Layer</Form.Label>
            <Col sm={10}>
              <LayerDropdown layers={props.visibleAndWritableLayers} selectedLayer={selectedLayer} onSetSelectedLayer={l => setSelectedLayer(l)} />
            </Col>
          </Form.Group>

          <Form.Group as={Row} controlId="name">
            <Form.Label column>Predicate</Form.Label>
            <Col sm={10}>
              <Dropdown
                value={newRelation.predicateID}
                placeholder='Select Predicate'
                onChange={(_, data) => setNewRelation({...newRelation, predicateID: data.value})}
                fluid
                search
                selection
                options={predicateList}
              />
            </Col>
          </Form.Group>

          <Form.Group as={Row} controlId="name">
            <Form.Label column>To CI</Form.Label>
            <Col sm={10}>
              <Dropdown
                value={newRelation.toCIID}
                placeholder='Select CI'
                onChange={(_, data) => setNewRelation({...newRelation, toCIID: data.value})}
                fluid
                search
                selection
                options={ciList}
              />
            </Col>
          </Form.Group>
          <Button secondary className="mr-2" onClick={() => setOpen(false)}>Cancel</Button>
          <Button primary type="submit">Insert</Button>
        </Form>
      </Segment>;
  }

  return <div className={"mb-4"}>
    {!isOpen && addButton}
    {addRelation}
    </div>;
}

AddNewRelation.propTypes = {
  isEditable: PropTypes.bool.isRequired,
  ciIdentity: PropTypes.string.isRequired
}

export default withApollo(AddNewRelation);