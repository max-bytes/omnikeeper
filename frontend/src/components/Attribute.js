import React, { useState } from "react";
import PropTypes from 'prop-types'
import { useMutation } from '@apollo/react-hooks';
import { withApollo } from 'react-apollo';
import { Button, Form, Row, Col } from "antd";
import { mutations } from '../graphql/mutations'
import LayerStackIcons from "./LayerStackIcons";
import OriginPopup from "./OriginPopup";
import EditableAttributeValue from "./EditableAttributeValue";

function Attribute(props) {

  var {ciIdentity, attribute, isEditable, visibleLayers, hideNameLabel, controlIdSuffix, ...rest} = props;
  
  const isArray = attribute.attribute.value.isArray;

  var [hasErrors, setHasErrors] = useState(false);
  const [values, setValues] = useState(attribute.attribute.value.values);
  React.useEffect(() => setValues(attribute.attribute.value.values), [attribute.attribute.value.values])

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

  const layerID = props.attribute.layerStackIDs[props.attribute.layerStackIDs.length - 1];

  let valueInput = 
    <EditableAttributeValue hideNameLabel={hideNameLabel} name={attribute.attribute.name} controlIdSuffix={controlIdSuffix} setHasErrors={setHasErrors} isEditable={isEditable} values={values} setValues={setValues} type={attribute.attribute.value.type} isArray={isArray} ciid={ciIdentity} />
;

  const leftPart = (hideNameLabel) ? '' : <div style={{display: 'flex', minHeight: '38px', alignItems: 'center'}}>
    {/* TODO: according to ant design label should be part of control */}
    <div className={"pr-1"} style={{whiteSpace: 'nowrap', flexGrow: 1, textAlign: 'right', paddingRight: '10px'}}>{attribute.attribute.name}</div>
  </div>;


  const rightPart = <div style={{minHeight: '38px', display: 'flex', alignItems: 'center'}}>
    <LayerStackIcons layerStack={attribute.layerStack} />
    <OriginPopup changesetID={attribute.attribute.changesetID} originType={attribute.attribute.origin.type} />
  </div>;

  if (isEditable) {
    const removeButton = (
      <Button type="primary" danger onClick={e => {
        e.preventDefault();
        removeCIAttribute({ variables: { ciIdentity: props.ciIdentity, name: attribute.attribute.name, layerID, layers: visibleLayers.map(l => l.name) } })
        .then(d => setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true, refreshTimeline: true }}));
      }} style={{ marginLeft: "0.5rem" }}>Remove</Button>
    );

    input = (
      <Form onFinish={e => {
          insertCIAttribute({ variables: { ciIdentity: props.ciIdentity, name: attribute.attribute.name, layerID, layers: visibleLayers.map(l => l.name), value: {
            type: attribute.attribute.value.type,
            values: values,
            isArray: isArray
          } } })
          .then(d => setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true, refreshTimeline: true }}));
        }}
        id={`value:${attribute.attribute.name}:${controlIdSuffix}`}
        >
          <Row>
            <Col span={4}>
              {leftPart}
            </Col>
            <Col span={14}>
              {valueInput}
            </Col>
            <Col span={2}>
              {rightPart}
            </Col>
            <Col span={4}>
              <Button htmlType="submit" type="primary" className={'mx-1'} disabled={attribute.attribute.value.values === values || hasErrors}>Update</Button>
              {removeButton}
            </Col> 
          </Row>
      </Form>
    );
  } else {
    input = (<Form id={`value:${attribute.attribute.name}:${controlIdSuffix}`}>
      <Row>
        <Col span={4}>
          {leftPart}
        </Col>
        <Col span={18}>
          {valueInput}
        </Col>
        <Col span={2}>
          {rightPart}
        </Col>
      </Row>
    </Form>);
  }


  return (
    <div key={attribute.attribute.name} {...rest}>
      {input}
    </div>
  );
}

Attribute.propTypes = {
  isEditable: PropTypes.bool.isRequired,
  ciIdentity: PropTypes.string,
  attribute: PropTypes.shape({
      attribute: PropTypes.shape({
        name: PropTypes.string.isRequired,
        state: PropTypes.string.isRequired,
        value: PropTypes.shape({
          type: PropTypes.string.isRequired,
          isArray: PropTypes.bool.isRequired,
          values: PropTypes.arrayOf(PropTypes.string).isRequired
      })
      })
    }).isRequired
}

export default withApollo(Attribute);
