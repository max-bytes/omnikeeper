import React, { useState } from "react";
import PropTypes from 'prop-types'
import { useMutation } from '@apollo/client';
import { Button, Form, Row, Col, Typography } from "antd";
import { mutations } from 'graphql/mutations'
import LayerStackIcons from "components/LayerStackIcons";
import OriginPopup from "components/OriginPopup";
import EditableAttributeValue from "./EditableAttributeValue";

const { Text } = Typography;

function Attribute(props) {

  var {attribute, layerStack, isEditable, visibleLayers, hideNameLabel, controlIdSuffix, removed, ...rest} = props;
  
  const isArray = attribute.value.isArray;

  var [hasErrors, setHasErrors] = useState(false);
  const [values, setValues] = useState(attribute.value.values);
  React.useEffect(() => setValues(attribute.value.values), [attribute.value.values])

  // TODO: loading
  const [insertCIAttribute] = useMutation(mutations.INSERT_CI_ATTRIBUTE);
  const [removeCIAttribute] = useMutation(mutations.REMOVE_CI_ATTRIBUTE, {
    update: (cache, data) => {
      /* HACK: find a better way to deal with cache invalidation! We would like to invalidate the affected CIs, which 
      translates to multiple entries in the cache, because each CI can be cached multiple times for each layerhash
      */
      // data.data.mutate.affectedCIs.forEach(ci => {
      //   var id = props.client.cache.identify(ci);
      //   console.log("Evicting: " + id);
      //   cache.evict(id);
      // });
    }
  });
  const [setSelectedTimeThreshold] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);

  let input;

  const valueInput = <EditableAttributeValue hideNameLabel={hideNameLabel} name={attribute.name} controlIdSuffix={controlIdSuffix} setHasErrors={setHasErrors} isEditable={isEditable} values={values} setValues={setValues} type={attribute.value.type} isArray={isArray} ciid={attribute.ciid} />;

  const leftPart = (hideNameLabel) ? '' : <div style={{display: 'flex', minHeight: '38px', alignItems: 'center'}}>
    {/* TODO: according to ant design label should be part of control */}
    {/* NOTE: we use direction: 'rtl' to have the text-ellipsis at the start of the text, see https://davidwalsh.name/css-ellipsis-left */}
    <div className={"pr-1"} style={{
      whiteSpace: 'nowrap', flexGrow: 1, textAlign: 'right', paddingRight: '10px', 
      textOverflow: 'ellipsis', overflow: 'hidden', direction: 'rtl'}}>
      {removed ? <Text delete>{attribute.name}</Text> : attribute.name}
    </div>
  </div>;

  const rightPart = <div style={{minHeight: '38px', display: 'flex', alignItems: 'center'}}>
    <LayerStackIcons layerStack={layerStack} />
    <OriginPopup changesetID={attribute.changesetID} />
  </div>;

  if (isEditable) {
    const layerStackIDs = layerStack.map(l => l.id);
    const layerID = layerStackIDs[0];
  
    const removeButton = (
      <Button type="primary" danger onClick={e => {
        e.preventDefault();
        removeCIAttribute({ variables: { ciIdentity: attribute.ciid, name: attribute.name, layerID, layers: visibleLayers.map(l => l.id) } })
        .then(d => setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true, refreshTimeline: true, refreshCI: true }}));
      }} style={{ marginLeft: "0.5rem" }}>Remove</Button>
    );

    input = (
      <Form onFinish={e => {
          insertCIAttribute({ variables: { ciIdentity: attribute.ciid, name: attribute.name, layerID, layers: visibleLayers.map(l => l.id), value: {
            type: attribute.value.type,
            values: values,
            isArray: isArray
          } } })
          .then(d => setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true, refreshTimeline: true, refreshCI: true }}));
        }}
        id={`value:${attribute.name}:${controlIdSuffix}`}
        >
          <Row>
            {/* name */}
            <Col span={7}>
              {leftPart}
            </Col>

            {/* input */}
            <Col style={{ flexGrow: 1 }}>
              {valueInput}
            </Col>

             {/* layer & user icon */}
            <Col style={{ padding: "0 10px"}}>
              {rightPart}
            </Col>

            {/* buttons */}
            <Col>
              <Button htmlType="submit" type="primary" className={'mx-1'} disabled={attribute.value.values === values || hasErrors}>Update</Button>
              {removeButton}
            </Col> 
          </Row>
      </Form>
    );
  } else {
    input = (<Form id={`value:${attribute.name}:${controlIdSuffix}`}>
      <Row>
        {/* name */}
        {hideNameLabel ? "" :
        <Col span={6}>
          {leftPart}
        </Col>
        }

        {/* input */}
        <Col style={{ flexGrow: 1 }}>
          {valueInput}
        </Col>

        {/* layer & user icon */}
        <Col style={{ padding: "0 10px"}}>
          {rightPart}
        </Col>
      </Row>
    </Form>);
  }


  return (
    <div key={attribute.name} {...rest}>
      {input}
    </div>
  );
}

Attribute.propTypes = {
  isEditable: PropTypes.bool.isRequired,
  attribute: PropTypes.shape({
    name: PropTypes.string.isRequired,
    value: PropTypes.shape({
      type: PropTypes.string.isRequired,
      isArray: PropTypes.bool.isRequired,
      values: PropTypes.arrayOf(PropTypes.string).isRequired
    }),
  }).isRequired,
}

export default Attribute;
