import React, { useState, useEffect, useRef } from "react";
import PropTypes from 'prop-types'
import { useMutation } from '@apollo/client';
import { mutations } from 'graphql/mutations'
import { Form, Button, Card, Radio } from "antd";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus } from '@fortawesome/free-solid-svg-icons'
import LayerDropdown from "components/LayerDropdown";
import { ErrorPopupButton } from "components/ErrorPopupButton";
import PredicateSelect from "components/PredicateSelect";
import SingleCISelect from "components/SingleCISelect";

function AddNewRelation(props) {
  const [insertError, setInsertError] = useState(undefined);
  const canBeEdited = props.isEditable && props.visibleAndWritableLayers.length > 0;
  // use useRef to ensure reference is constant and can be properly used in dependency array
  const visibleLayersRef = useRef(JSON.stringify(props.visibleLayers)); // because JS only does reference equality, we need to convert the array to a string
  const { current: initialRelation } = useRef({predicateID: "", targetCIID: null, forward: true, layer: null });
  const [isOpen, setOpen] = useState(false);

  const [newRelation, setNewRelation] = useState(initialRelation);
  useEffect(() => { if (!canBeEdited) setOpen(false); }, [canBeEdited]);
  useEffect(() => { setOpen(false);  }, 
    [
      props.ciIdentity, 
      visibleLayersRef, 
      initialRelation
    ]);
  useEffect(() => { if (!isOpen) setNewRelation(initialRelation); }, [isOpen, initialRelation]);

  const [insertRelation] = useMutation(mutations.INSERT_RELATION);
  const [setSelectedTimeThreshold] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);

  const addButton = <Button disabled={!canBeEdited} onClick={() => setOpen(!isOpen)} icon={<FontAwesomeIcon icon={faPlus} style={{marginRight: "10px"}}/>} type="primary">
    Add Relation
  </Button>
  
  let addRelation = <></>;
  if (isOpen) {

    const predicateSelect = <Form.Item name="predicate" key="predicate" noStyle style={{ flexGrow: '1' }}>
      <PredicateSelect predicateID={newRelation.predicateID} setPredicateID={(predicateID) => {
          setNewRelation({...newRelation, predicateID: predicateID});
        }} />
    </Form.Item>;
    const targetCISelect = <Form.Item name="targetCI" key="ci" noStyle style={{ flexGrow: '2' }}>
      <SingleCISelect 
        layers={props.visibleLayers} 
        selectedCIID={newRelation.targetCIID} 
        setSelectedCIID={(ciid) => setNewRelation({...newRelation, targetCIID: ciid})} 
      />
    </Form.Item>;
    const thisCIStyle = {height: '30px', display: 'inline-flex', alignItems: 'center', whiteSpace: 'nowrap'};
    const thisCI = <span key="thisCI" style={thisCIStyle}>{newRelation.forward ? `This CI...` : `...this CI`}</span>;
    const directionalUI = (newRelation.forward) ? 
      (<>{thisCI} {predicateSelect} {targetCISelect}</>) : 
      (<>{targetCISelect} {predicateSelect} {thisCI}</>);


    // move add functionality into on-prop
    addRelation = 
      <Card style={{ "boxShadow": "0px 0px 5px 0px rgba(0,0,0,0.25)" }}>
        <Form onFinish={e => {
            setInsertError(undefined);

            const fromTo = (newRelation.forward) 
              ? { fromCIID: props.ciIdentity, toCIID: newRelation.targetCIID } 
              : { fromCIID: newRelation.targetCIID, toCIID: props.ciIdentity };

            insertRelation({ variables: { ...fromTo, predicateID: newRelation.predicateID, layerID: newRelation.layer.id, layers: props.visibleLayers} })
              .then(d => {
                setOpen(false);
                setNewRelation(initialRelation);
                setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true, refreshTimeline: true, refreshCI: true }});
              }).catch(e => {
                setInsertError(e);
              });
          }} labelCol={{ span: 4 }} wrapperCol={{ span: 20 }}>

          <Form.Item label="Direction">
            <Radio.Group onChange={e => setNewRelation({...newRelation, forward: e.target.value})} value={newRelation.forward}>
              <Radio value={true}>Forward</Radio>
              <Radio value={false}>Backward</Radio>
            </Radio.Group>
          </Form.Item>

          <Form.Item label="Relation" name="relation">
            <div style={{display: 'flex', gap: '10px'}}>
              {directionalUI}
            </div>
          </Form.Item>
          
          <Form.Item label="Layer" name="layer">
            <LayerDropdown layers={props.visibleAndWritableLayers} style={{maxWidth: '300px'}}
              selectedLayer={newRelation.layer} onSetSelectedLayer={l => setNewRelation({...newRelation, layer: l})} />
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

export default AddNewRelation;