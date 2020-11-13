import React, { useState } from "react";
import PropTypes from 'prop-types'
import { useMutation } from '@apollo/react-hooks';
import { withApollo } from 'react-apollo';
import { mutations } from '../graphql/mutations'
import { AttributeTypes } from '../utils/attributeTypes'
import EditableAttributeValue from "./EditableAttributeValue";
import { Row, Col, Form, Input, Select, Checkbox, Button, Card } from "antd";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus } from '@fortawesome/free-solid-svg-icons'
import LayerDropdown from "./LayerDropdown";
import { ErrorPopupButton } from "./ErrorPopupButton";
import { useExplorerLayers } from '../utils/layers';

function AddNewAttribute(props) {
  const [insertError, setInsertError] = useState(undefined);
  const { data: visibleAndWritableLayers } = useExplorerLayers(true, true);
  const { data: visibleLayers } = useExplorerLayers(true);
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

  const addButton = <Button disabled={!canBeEdited} onClick={() => setOpen(!isOpen)} icon={<FontAwesomeIcon icon={faPlus} style={{marginRight: "10px"}}/>} type="primary">
    Add Attribute
  </Button>

  let addAttribute = <></>;
  if (isOpen) {
    addAttribute = 
      <Card style={{ "boxShadow": "0px 0px 5px 0px rgba(0,0,0,0.25)" }}>
        <Form labelCol={{ span: "4" }} onFinish={e => {
            setInsertError(undefined);
            insertCIAttribute({ variables: { layers: visibleLayers.map(l => l.name), ciIdentity: props.ciIdentity, name: newAttribute.name, layerID: selectedLayer.id, value: {
              type: newAttribute.type,
              isArray: newAttribute.isArray,
              values: newAttribute.values
            } } }).then(d => {
              setOpen(false);
              setNewAttribute(initialAttribute);
              setSelectedTimeThreshold({ variables:{ newTimeThreshold: null, isLatest: true, refreshTimeline: true }});
            }).catch(e => {
              setInsertError(e);
            });
          }}>

          <Row>
            <Col span={18}>
                <Form.Item name="layer" label="Layer">
                    <LayerDropdown layers={visibleAndWritableLayers} selectedLayer={selectedLayer} onSetSelectedLayer={l => setSelectedLayer(l)} />
                </Form.Item>
            </Col>
          </Row>

          <Row>
              <Col span={18}>
                <Form.Item label="Type" name="type">
                    <Select style={{ width: "100%" }} placeholder='Select attribute type' showSearch value={newAttribute.type}
                        onChange={(e, data) => {
                        // we'll clear the value, to be safe, TODO: better value migration between types
                        setNewAttribute({...newAttribute, type: data.value, value: ''});
                        }}
                        options={AttributeTypes.map(at => { return {key: at.id, value: at.id, label: at.name }; })}
                    />
                </Form.Item>
              </Col>
              <Col span={4} style={{ paddingLeft: "15px"}}>
                <Form.Item name="isArray" valuePropName="checked">
                    <Checkbox checked={newAttribute.isArray} onChange={e => setNewAttribute({...newAttribute, values: [''], isArray: e.target.checked })}>Is Array</Checkbox>
                </Form.Item>
              </Col>
          </Row>

          <Row>
            <Col span={18}>
                <Form.Item name="name" label="Name">
                    <Input type="text" placeholder="Enter name" value={newAttribute.name} onChange={e => setNewAttribute({...newAttribute, name: e.target.value})} />
                </Form.Item>
            </Col>
          </Row>

          <Row>
            <Col span={18}>
                <Form.Item name="value" label={((newAttribute.isArray) ? 'Values' : 'Value')}>
                    <EditableAttributeValue hideNameLabel setHasErrors={setHasErrors} name={'newAttribute'} autoFocus={valueAutofocussed} values={newAttribute.values} setValues={vs => setNewAttribute({...newAttribute, values: vs})} type={newAttribute.type} isArray={newAttribute.isArray} />
                </Form.Item>
            </Col>
          </Row>

          <Button type="secondary" onClick={() => setOpen(false)} style={{ marginRight: "0.5rem" }}>Cancel</Button>
          <Button type="primary" htmlType="submit" disabled={hasErrors || !newAttribute.name}>Insert</Button>
          <ErrorPopupButton error={insertError} />
        </Form>
      </Card>;
  }

  return <div style={{ marginBottom: "1.5rem" }}>
    {!isOpen && addButton}
    {addAttribute}
    </div>;
}

AddNewAttribute.propTypes = {
  isEditable: PropTypes.bool.isRequired,
  ciIdentity: PropTypes.string.isRequired
}

export default withApollo(AddNewAttribute);