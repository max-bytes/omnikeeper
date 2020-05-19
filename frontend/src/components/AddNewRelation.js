import React, { useState, useEffect } from "react";
import { useQuery, useLazyQuery } from '@apollo/client';
import PropTypes from 'prop-types'
import { useMutation } from '@apollo/react-hooks';
import { withApollo } from 'react-apollo';
import Form from 'react-bootstrap/Form';
import Col from 'react-bootstrap/Col';
import { mutations } from '../graphql/mutations'
import { queries } from '../graphql/queries'
import { Dropdown, Button, Icon, Segment } from 'semantic-ui-react';
import LayerDropdown from "./LayerDropdown";
import { ErrorPopupButton } from "./ErrorPopupButton";

function AddNewRelation(props) {
  const [insertError, setInsertError] = useState(undefined);
  const canBeEdited = props.isEditable && props.visibleAndWritableLayers.length > 0;
  let initialRelation = {predicateID: null, targetCIID: null, forward: true, layer: null };
  const [isOpen, setOpen] = useState(false);
  const [newRelation, setNewRelation] = useState(initialRelation);
  React.useEffect(() => { if (!canBeEdited) setOpen(false); }, [canBeEdited]);

  const [getValidTargetCIs, { data: dataCIs, loading: loadingCIs }] = useLazyQuery(queries.ValidRelationTargetCIs, { 
    variables: {layers: props.visibleLayers }
  });
  const { data: directedPredicates } = useQuery(queries.DirectedPredicateList, {
    variables: { preferredForCI: props.ciIdentity, layersForEffectiveTraits: props.visibleLayers }
  });
  useEffect(() => {
    setNewRelation(e => ({...e, targetCIID: null }));
    getValidTargetCIs({variables: { forward: newRelation.forward, predicateID: newRelation.predicateID }});
  }, [newRelation.predicateID, newRelation.forward, getValidTargetCIs]);
  const [insertRelation] = useMutation(mutations.INSERT_RELATION);
  const [setSelectedTimeThreshold] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);

  const addButton = <Button disabled={!canBeEdited} onClick={() => setOpen(!isOpen)} icon primary labelPosition='left'>
    <Icon name='plus' />Add Relation
  </Button>
  
  let addRelation = <></>;
  if (isOpen && directedPredicates) {
    var ciList = [];
    if (dataCIs)
      ciList = dataCIs.validRelationTargetCIs.map(d => {
        return { key: d.id, value: d.id, text: d.name };
      });
    const sortedPredicates = [...directedPredicates.directedPredicates]
    sortedPredicates.sort((a,b) => a.predicateID.localeCompare(b.predicateID));
    var predicateList = sortedPredicates.map(d => {
      const isDisabled = d.predicateState !== "ACTIVE";
      const forwardStr = (d.forward) ? 'forward' : 'back';
      return { key: `${d.predicateID}$$$$${forwardStr}`, value: `${d.predicateID}$$$$${forwardStr}`, disabled: isDisabled, text: `${d.wording}...`,
          content: (<><b>{d.wording}</b>... <span className="text-muted"><br />({d.predicateID})</span></>) };
    });

    // move add functionality into on-prop
    addRelation = 
      <Segment raised>
        <Form onSubmit={e => {
            e.preventDefault();
            setInsertError(undefined);

            const fromTo = (newRelation.forward) 
              ? { fromCIID: props.ciIdentity, toCIID: newRelation.targetCIID } 
              : { fromCIID: newRelation.targetCIID, toCIID: props.ciIdentity };

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
                {/* TODO: create own PredicateDropdown, it might be needed more often (similar to LayerDropdown) */}
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
                {/* TODO: create own RelatedTargetCIDropdown (similar to LayerDropdown) */}
                <Dropdown loading={loadingCIs}
                  disabled={loadingCIs}
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