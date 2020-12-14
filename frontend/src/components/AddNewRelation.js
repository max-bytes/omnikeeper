import React, { useState, useEffect, useRef } from "react";
import { useQuery, useLazyQuery } from '@apollo/client';
import PropTypes from 'prop-types'
import { useMutation } from '@apollo/react-hooks';
import { withApollo } from 'react-apollo';
import { mutations } from '../graphql/mutations'
import { queries } from '../graphql/queries'
import { Form, Select, Button, Card } from "antd";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus } from '@fortawesome/free-solid-svg-icons'
import LayerDropdown from "./LayerDropdown";
import { ErrorPopupButton } from "./ErrorPopupButton";

function AddNewRelation(props) {
  const [insertError, setInsertError] = useState(undefined);
  const canBeEdited = props.isEditable && props.visibleAndWritableLayers.length > 0;
  // use useRef to ensure reference is constant and can be properly used in dependency array
  const visibleLayersRef = useRef(JSON.stringify(props.visibleLayers)); // because JS only does reference equality, we need to convert the array to a string
  const { current: initialRelation } = useRef({predicateID: null, targetCIID: null, forward: true, layer: null });
  const [isOpen, setOpen] = useState(false);

  const [newRelation, setNewRelation] = useState(initialRelation);
  useEffect(() => { if (!canBeEdited) setOpen(false); }, [canBeEdited]);
  useEffect(() => { setOpen(false); setNewRelation(initialRelation); }, 
    [
      props.ciIdentity, 
      visibleLayersRef, 
      initialRelation
    ]);

  const [getValidTargetCIs, { data: dataCIs, loading: loadingCIs }] = useLazyQuery(queries.ValidRelationTargetCIs, { 
    variables: {layers: props.visibleLayers}
  });


  const { data: directedPredicates } = useQuery(queries.DirectedPredicateList, {
    variables: { preferredForCI: props.ciIdentity, layersForEffectiveTraits: props.visibleLayers }
  });
  useEffect(() => {
    setNewRelation(e => ({...e, targetCIID: null }));
    if (newRelation.predicateID)
      getValidTargetCIs({variables: { forward: newRelation.forward, predicateID: newRelation.predicateID }});
  }, [newRelation.predicateID, newRelation.forward, props.ciIdentity, getValidTargetCIs]);
  const [insertRelation] = useMutation(mutations.INSERT_RELATION);
  const [setSelectedTimeThreshold] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);

  const addButton = <Button disabled={!canBeEdited} onClick={() => setOpen(!isOpen)} icon={<FontAwesomeIcon icon={faPlus} style={{marginRight: "10px"}}/>} type="primary">
    Add Relation
  </Button>
  
  let addRelation = <></>;
  if (isOpen && directedPredicates) {
    var ciList = [];
    if (dataCIs)
      ciList = dataCIs.validRelationTargetCIs.map(d => {
        return { key: d.id, value: d.id, label: d.name };
      });
    const sortedPredicates = [...directedPredicates.directedPredicates]
    sortedPredicates.sort((a,b) => a.predicateID.localeCompare(b.predicateID));
    var predicateList = sortedPredicates.map(d => {
      const isDisabled = d.predicateState !== "ACTIVE";
      const forwardStr = (d.forward) ? 'forward' : 'back';
      return { key: `${d.predicateID}$$$$${forwardStr}`, value: `${d.predicateID}$$$$${forwardStr}`, disabled: isDisabled,
          label: (<><b>{d.wording}</b>... <span className="text-muted"><br />({d.predicateID})</span></>) };
    });

    // move add functionality into on-prop
    addRelation = 
      <Card style={{ "boxShadow": "0px 0px 5px 0px rgba(0,0,0,0.25)" }}>
        <Form onFinish={e => {
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

          <Form.Item label="This CI..." style={{ marginBottom: 0 }}>
            <Form.Item name="predicate" style={{ display: 'inline-block', width: 'calc(33% - 8px)' }}>
                {/* TODO: create own PredicateDropdown, it might be needed more often (similar to LayerDropdown) */}
              <Select
                value={((newRelation.predicateID) ? `${newRelation.predicateID}$$$$${((newRelation.forward) ? 'forward' : 'back')}` : undefined)}
                placeholder='Select Predicate'
                style={{ width: "100%" }}
                onChange={(_, data) => { 
                  const [predicateID, forwardStr] = data.value.split('$$$$');
                  setNewRelation({...newRelation, predicateID: predicateID, forward: forwardStr === 'forward'});
                }}
                showSearch
                options={predicateList}
              />
            </Form.Item>

            <Form.Item name="targetCI" style={{ display: 'inline-block', width: 'calc(33% - 8px)', marginLeft: '8px' }}>
                {/* TODO: create own RelatedTargetCIDropdown (similar to LayerDropdown) */}
                <Select
                  loading={loadingCIs}
                  disabled={loadingCIs}
                  value={newRelation.targetCIID}
                  placeholder='Target CI'
                  style={{ width: "100%" }}
                  onChange={(_, data) => {
                    setNewRelation({...newRelation, targetCIID: data.value})
                  }}
                  showSearch
                  options={ciList}
                />
            </Form.Item>

            <Form.Item name="layer" style={{ display: 'inline-block', width: 'calc(33% - 8px)', marginLeft: '8px' }}>
              <LayerDropdown layers={props.visibleAndWritableLayers} selectedLayer={newRelation.layer} onSetSelectedLayer={l => setNewRelation({...newRelation, layer: l})} />
            </Form.Item>

          </Form.Item>
          <Button type="secondary" onClick={() => setOpen(false)} style={{ marginRight: "0.5rem" }}>Cancel</Button>
          <Button type="primary" htmlType="submit">Insert</Button>
          <ErrorPopupButton error={insertError} />
        </Form>
      </Card>;
  }

  return <div style={{ marginBottom: "1.5rem" }}>
    {!isOpen && addButton}
    {addRelation}
    </div>;
}

AddNewRelation.propTypes = {
  isEditable: PropTypes.bool.isRequired,
  ciIdentity: PropTypes.string.isRequired
}

export default withApollo(AddNewRelation);