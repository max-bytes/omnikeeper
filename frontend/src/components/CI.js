import React, { useState } from 'react';
import PropTypes from 'prop-types'
import RelatedCI from './RelatedCI';
import {Row, Col} from 'react-bootstrap';
import AddNewAttribute from './AddNewAttribute';
import AttributeList from './AttributeList';
import AddNewRelation from './AddNewRelation';
import TemplateErrors from './TemplateErrors';
import CIRelations from './CIRelations';
import { Flipper, Flipped } from 'react-flip-toolkit'
import { Tab } from 'semantic-ui-react'
import { onAppear, onExit } from '../utils/animation';

function CI(props) {

  const [selectedTab, setSelectedTab] = useState(0);
  const [createNewAttribute, setCreateNewAttribute] = useState(undefined);

  const panes = [
    { menuItem: 'Attributes', render: () => <Tab.Pane>
      <Row>
        <Col>
          <AddNewAttribute prefilled={createNewAttribute} isEditable={props.isEditable} layers={props.layers} ciIdentity={props.ci.identity}></AddNewAttribute>
        </Col>
      </Row>
      <Row>
        <Col>
        <AttributeList mergedAttributes={props.ci.mergedAttributes} isEditable={props.isEditable} layers={props.layers} ciIdentity={props.ci.identity}></AttributeList>
        </Col>
      </Row>
    </Tab.Pane> },
    { menuItem: 'Relations', render: () => <Tab.Pane>
      <CIRelations visibleLayers={props.visibleLayers} timeThreshold={props.timeThreshold} related={props.ci.related} isEditable={props.isEditable} layers={props.layers} ciIdentity={props.ci.identity} />
    </Tab.Pane> },
  ]

  return (<div style={{margin: "10px 10px"}}>
    <h3>CI {props.ci.identity} - type: {props.ci.type.id}</h3>
    <TemplateErrors templateErrors={props.ci.templateErrors} 
      onCreateNewAttribute={(attributeName, attributeType) => {
        setCreateNewAttribute({name: attributeName, type: attributeType, value: '', layer: props.layers[0]}); // TODO: correct layer
      }}
      onOverwriteAttribute={(attributeName, attributeType) => {
        // find current value and layer
        var currentAttribute = props.ci.mergedAttributes.find(a => a.attribute.name === attributeName);
        console.log(props.ci.mergedAttributes);
        if (currentAttribute) {
          // TODO: get current correct layer
          const layerID = currentAttribute.layerStackIDs[currentAttribute.layerStackIDs.length - 1];
          const layer = props.layers.find(l => l.id === layerID);
          const newValues = {name: attributeName, type: attributeType, value: currentAttribute.attribute.value.value, layer: layer};
          setCreateNewAttribute(newValues);
        }
      }}
      />
    <Tab activeIndex={selectedTab} onTabChange={(e, {activeIndex}) => setSelectedTab(activeIndex)} panes={panes} />
  </div>);
}

CI.propTypes = {
  isEditable: PropTypes.bool.isRequired,
  layers: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.number.isRequired,
      name: PropTypes.string.isRequired,
      visibility: PropTypes.bool.isRequired
    }).isRequired
  ).isRequired,
  ci: PropTypes.shape({
    identity: PropTypes.string.isRequired,
    type: PropTypes.shape({
      id: PropTypes.string.isRequired
    }).isRequired,
    templateErrors: PropTypes.shape({
      attributeErrors: PropTypes.arrayOf(PropTypes.shape({
        attributeName: PropTypes.string.isRequired,
        errors: PropTypes.arrayOf(PropTypes.string).isRequired
      })).isRequired
    }).isRequired,
    attributes: PropTypes.arrayOf(
      PropTypes.shape({
        attribute: PropTypes.shape({
          name: PropTypes.string.isRequired,
          state: PropTypes.string.isRequired,
          value: PropTypes.shape({
            type: PropTypes.string.isRequired,
            value: PropTypes.string.isRequired
          })
        })
      })
    )
  }).isRequired
}

export default CI;