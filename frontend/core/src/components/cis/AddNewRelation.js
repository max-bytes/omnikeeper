import React, { useState, useEffect, useRef } from "react";
import PropTypes from 'prop-types'
import { useMutation } from '@apollo/client';
import { mutations } from 'graphql/mutations'
import { Form, Button, Card, Space, Input } from "antd";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus } from '@fortawesome/free-solid-svg-icons'
import LayerDropdown from "components/LayerDropdown";
import { ErrorPopupButton } from "components/ErrorPopupButton";
import SingleCISelect from "components/SingleCISelect";
import Checkbox from "antd/lib/checkbox/Checkbox";

function AddNewRelation(props) {
  const {isOutgoingRelation} = props;

  const [insertError, setInsertError] = useState(undefined);
  const canBeEdited = props.isEditable && props.visibleAndWritableLayers.length > 0;
  // use useRef to ensure reference is constant and can be properly used in dependency array
  const visibleLayersRef = useRef(JSON.stringify(props.visibleLayers)); // because JS only does reference equality, we need to convert the array to a string
  const { current: initialRelation } = useRef({predicateID: "", targetCIID: null, layer: null, mask: false });
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
    Add {(isOutgoingRelation) ? "Outgoing" : "Incoming"} Relation
  </Button>
  
  let addRelation = <></>;
  if (isOpen) {

    const predicateSelect = <Form.Item name="predicate" key="predicate" noStyle>
      <Input type="text" placeholder="Predicate" value={newRelation.predicateID} onChange={e => setNewRelation({...newRelation, predicateID: e.target.value})} />
    </Form.Item>;
    const targetCISelect = <Form.Item name="targetCI" key="ci" noStyle>
      <SingleCISelect
        layers={props.visibleLayers} 
        selectedCIID={newRelation.targetCIID} 
        setSelectedCIID={(ciid) => setNewRelation({...newRelation, targetCIID: ciid})} 
      />
    </Form.Item>;
    const thisCIStyle = {height: '30px', display: 'inline-flex', alignItems: 'center', whiteSpace: 'nowrap'};
    const thisCI = <span key="thisCI" style={thisCIStyle}>{isOutgoingRelation ? `This CI...` : `...this CI`}</span>;
    const directionalUI = (isOutgoingRelation) ? 
      (<Space wrap={true}>{thisCI} {predicateSelect} {targetCISelect}</Space>) : 
      (<Space wrap={true}>{targetCISelect} {predicateSelect} {thisCI}</Space>);


    // move add functionality into on-prop
    addRelation = 
      <Card style={{ "boxShadow": "0px 0px 5px 0px rgba(0,0,0,0.25)" }}>
        <Form onFinish={e => {
            setInsertError(undefined);

            const fromTo = (isOutgoingRelation) 
              ? { fromCIID: props.ciIdentity, toCIID: newRelation.targetCIID } 
              : { fromCIID: newRelation.targetCIID, toCIID: props.ciIdentity };

            insertRelation({ variables: { 
                ...fromTo, 
                predicateID: newRelation.predicateID, 
                mask: newRelation.mask,
                layerID: newRelation.layer.id, 
                layers: props.visibleLayers} })
              .then(d => {
                setOpen(false);
                setNewRelation(initialRelation);
                setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true, refreshTimeline: true, refreshCI: true }});
              }).catch(e => {
                setInsertError(e);
              });
          }} labelCol={{ span: 4 }} wrapperCol={{ span: 20 }}>

          <Form.Item label={(isOutgoingRelation) ? "Outgoing Relation" : "Incoming Relation"} name="relation">
              {directionalUI}
          </Form.Item>

          <Form.Item label="Is Mask" name="mask" key="mask" valuePropName="checked">
            <Checkbox checked={newRelation.mask} onChange={(e) => {setNewRelation({...newRelation, mask: e.target.checked})}} />
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