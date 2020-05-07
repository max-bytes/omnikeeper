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
import { Dropdown, Button, Icon, Segment } from 'semantic-ui-react';
import LayerDropdown from "./LayerDropdown";

function AddNewRelation(props) {
  const canBeEdited = props.isEditable && props.visibleAndWritableLayers.length > 0;
  let initialRelation = {predicateID: null, toCIID: null, layer: null };
  // const [selectedLayer, setSelectedLayer] = useState(undefined);
  const [isOpen, setOpen] = useState(false);
  const [newRelation, setNewRelation] = useState(initialRelation);
  React.useEffect(() => { if (!canBeEdited) setOpen(false); }, [canBeEdited]);

  // TODO: loading
  const { data: dataCIs } = useQuery(queries.CIList, { variables: {layers: props.visibleLayers } });
  const { data: dataPredicates } = useQuery(queries.PredicateList, {
    variables: {stateFilter: 'ACTIVE_AND_DEPRECATED'}
  });
  const [insertRelation] = useMutation(mutations.INSERT_RELATION);
  const [setSelectedTimeThreshold] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);

  const addButton = <Button disabled={!canBeEdited} onClick={() => setOpen(!isOpen)} icon primary labelPosition='left'>
    <Icon name='plus' />Add Relation
  </Button>
  
  let addRelation = <></>;
  if (isOpen && dataCIs && dataPredicates) {
    var ciList = dataCIs.compactCIs.map(d => {
      return { key: d.id, value: d.id, text: d.name };
    });
    const sortedPredicates = [...dataPredicates.predicates]
    sortedPredicates.sort((a,b) => (a.state + "_" + a.wordingFrom).localeCompare(b.state + "_" + b.wordingFrom));
    var predicateList = sortedPredicates.map(d => {
      const isDisabled = d.state !== "ACTIVE";
      return { key: d.id, value: d.id, text: d.labelWordingFrom, disabled: isDisabled };
    });

    // move add functionality into on-prop
    addRelation = 
      <Segment raised>
        <Form onSubmit={e => {
            e.preventDefault();
            insertRelation({ variables: { fromCIID: props.ciIdentity, toCIID: newRelation.toCIID, predicateID: newRelation.predicateID, 
              includeRelated: props.perPredicateLimit, layerID: newRelation.layer.id, layers: props.visibleLayers} }).then(d => {
              setOpen(false);
              setNewRelation(initialRelation);
              setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true }});
            });
          }}>

          <Form.Group as={Row} controlId="layer">
            <Form.Label column>Layer</Form.Label>
            <Col sm={10}>
              <LayerDropdown layers={props.visibleAndWritableLayers} selectedLayer={newRelation.layer} onSetSelectedLayer={l => setNewRelation({...newRelation, layer: l})} />
            </Col>
          </Form.Group>

          <Form.Group as={Row} controlId="name">
            <Form.Label column>Predicate</Form.Label>
            <Col sm={10}>
              {/* TODO: create own PredicateDropdown (similar to LayerDropdown) */}
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
              {/* TODO: create own CIDropdown (similar to LayerDropdown) */}
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