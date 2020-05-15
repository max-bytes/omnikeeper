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
import { ErrorPopupButton } from "./ErrorPopupButton";

function AddNewRelation(props) {
  const [insertError, setInsertError] = useState(undefined);
  const canBeEdited = props.isEditable && props.visibleAndWritableLayers.length > 0;
  let initialRelation = {predicateID: null, targetCIID: null, forward: true, layer: null };
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
    var predicateList = sortedPredicates.flatMap(d => {
      const isDisabled = d.state !== "ACTIVE";
      return [
        { key: `${d.id}$$$$forward`, value: `${d.id}$$$$forward`, disabled: isDisabled, text: `${d.labelWordingFrom}...`,
          content: (<><b>{d.labelWordingFrom}</b>... <span className="text-muted"><br />({d.id})</span></>) },
        { key: `${d.id}$$$$back`, value: `${d.id}$$$$back`, disabled: isDisabled, text: `${d.labelWordingTo}...`,
          content: (<><b>{d.labelWordingTo}</b>... <span className="text-muted"><br />({d.id})</span></>) }
      ];
    });

    // move add functionality into on-prop
    addRelation = 
      <Segment raised>
        <Form onSubmit={e => {
            e.preventDefault();
            setInsertError(undefined);

            const fromTo = (newRelation.forward) ? { fromCIID: props.ciIdentity, toCIID: newRelation.targetCIID } : { fromCIID: newRelation.targetCIID, toCIID: props.ciIdentity };

            insertRelation({ variables: { ...fromTo, predicateID: newRelation.predicateID, 
              includeRelated: props.perPredicateLimit, layerID: newRelation.layer.id, layers: props.visibleLayers} })
              .then(d => {
                setOpen(false);
                setNewRelation(initialRelation);
                setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true, refreshTimeline: true }});
              }).catch(e => {
                setInsertError(e);
              });
          }}>

          <Form.Row>
            <Form.Label column xs={1}>
              This CI...
            </Form.Label>
            <Form.Group as={Col} xs={4} controlId="name">
                {/* TODO: create own PredicateDropdown (similar to LayerDropdown) */}
              <Dropdown
                value={((newRelation.predicateID) ? `${newRelation.predicateID}$$$$${((newRelation.forward) ? 'forward' : 'back')}` : undefined)}
                placeholder='Select Predicate'
                onChange={(_, data) => {
                  const [predicateID, forwardStr] = data.value.split('$$$$');
                  setNewRelation({...newRelation, predicateID: predicateID, forward: forwardStr === 'forward'});
                }}
                fluid
                search
                selection
                options={predicateList}
              />
            </Form.Group>

            <Form.Group as={Col} xs={4} controlId="name">
                {/* TODO: create own CIDropdown (similar to LayerDropdown) */}
                <Dropdown 
                  value={newRelation.targetCIID}
                  placeholder='Target CI'
                  onChange={(_, data) => setNewRelation({...newRelation, targetCIID: data.value})}
                  fluid
                  search
                  selection
                  options={ciList}
                />
            </Form.Group>

            <Form.Group as={Col} xs={3} controlId="layer">
              <LayerDropdown layers={props.visibleAndWritableLayers} selectedLayer={newRelation.layer} onSetSelectedLayer={l => setNewRelation({...newRelation, layer: l})} />
            </Form.Group>

          </Form.Row>
          <Button secondary className="mr-2" onClick={() => setOpen(false)}>Cancel</Button>
          <Button primary type="submit">Insert</Button>
          <ErrorPopupButton error={insertError} />
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