import React, { useState } from "react";
import { useQuery } from '@apollo/client';
import PropTypes from 'prop-types'
import { useMutation } from '@apollo/react-hooks';
import { withApollo } from 'react-apollo';
import Button from 'react-bootstrap/Button';
import Form from 'react-bootstrap/Form';
import Col from 'react-bootstrap/Col';
import { mutations } from './mutations'
import { queries } from './queries'
import { Row } from "react-bootstrap";
import { Dropdown } from 'semantic-ui-react';

function AddNewRelation(props) {

  let initialRelation = {predicate: '', toCIID: null };
  const [selectedLayer, setSelectedLayer] = useState(undefined);
  const [newRelation, setNewRelation] = useState(initialRelation);
  React.useEffect(() => { if (!props.isEditable) setSelectedLayer(undefined); }, [props.isEditable]);
  
  let visibleLayers = props.layers.filter(l => l.visibility).map(l => l.name);

  let addButtons = <div>Add Relation to Layer: {props.layers.map(layer => {
    return <Button disabled={!props.isEditable} key={layer.name} style={{backgroundColor: layer.color, borderColor: layer.color, color: '#111'}} className={"mx-1"}
    onClick={() => {if (selectedLayer === layer) setSelectedLayer(undefined); else setSelectedLayer(layer);}}>{layer.name}</Button>;
  })}</div>;

  // TODO: loading
  const { loading: loadingCIs, error: errorCIs, data: dataCIs } = useQuery(queries.CIList);
  const [insertRelation, { loading }] = useMutation(mutations.INSERT_RELATION, { refetchQueries: ['changesets', 'ci'], awaitRefetchQueries: true });
  const [setSelectedTimeThreshold, { _ }] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);

  
  let addRelation = <span></span>;
  if (selectedLayer && dataCIs) {
    var ciList = dataCIs.cis.map(d => {
      return { key: d.identity, value: d.identity, text: d.identity };
    });

    addRelation = 
      <div style={{backgroundColor: selectedLayer.color, borderColor: selectedLayer.color}} className={"p-2"}>
        <Form onSubmit={e => {
            e.preventDefault();
            insertRelation({ variables: { layers: visibleLayers, fromCIID: props.ciIdentity, toCIID: newRelation.toCIID, predicate: newRelation.predicate, layerID: selectedLayer.id} }).then(d => {
              setSelectedLayer(undefined);
              setNewRelation(initialRelation);
              setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true }});
            });
          }}>
            <Form.Group as={Row} controlId="name">
              <Form.Label column>Predicate</Form.Label>
              <Col sm={10}>
                <Form.Control type="text" placeholder="Enter predicate" value={newRelation.predicate} onChange={e => setNewRelation({...newRelation, predicate: e.target.value})} />
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
            <Button variant="primary" type="submit">Insert</Button>
        </Form>
      </div>;
  }

  return <div className={"m-2"}>
    {addButtons}
    {addRelation}
    </div>;
}

AddNewRelation.propTypes = {
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

export default withApollo(AddNewRelation);