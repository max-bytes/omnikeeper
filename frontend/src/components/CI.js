import React, { useState } from 'react';
import PropTypes from 'prop-types'
import {Row, Col} from 'react-bootstrap';
import AddNewAttribute from './AddNewAttribute';
import ExplorerAttributeList from './ExplorerAttributeList';
import TemplateErrors from './TemplateErrors';
import CIRelations from './CIRelations';
import EffectiveTraits from './EffectiveTraits';
import { Tab } from 'semantic-ui-react'
import { useExplorerLayers } from '../utils/layers';

function CI(props) {

  const [selectedTab, setSelectedTab] = useState(0);
  const [createNewAttribute, setCreateNewAttribute] = useState(undefined);
  const { data: visibleAndWritableLayers } = useExplorerLayers(true, true);
  const { data: visibleLayers } = useExplorerLayers(true);
    
  const panes = [
    { menuItem: 'Attributes', render: () => <Tab.Pane>
      <Row>
        <Col>
          <AddNewAttribute prefilled={createNewAttribute} isEditable={props.isEditable} ciIdentity={props.ci.id}></AddNewAttribute>
        </Col>
      </Row>
      <Row>
        <Col>
          <ExplorerAttributeList mergedAttributes={props.ci.mergedAttributes} isEditable={props.isEditable} 
            ciIdentity={props.ci.id} visibleAndWritableLayers={visibleAndWritableLayers} visibleLayers={visibleLayers} />
        </Col>
      </Row>
    </Tab.Pane> },
    { menuItem: 'Relations', render: () => <Tab.Pane>
      <CIRelations timeThreshold={props.timeThreshold} related={props.ci.related} isEditable={props.isEditable} ciIdentity={props.ci.id} />
    </Tab.Pane> },
    { menuItem: 'Effective Traits', render: () => <Tab.Pane>
      <EffectiveTraits timeThreshold={props.timeThreshold} traits={props.ci.effectiveTraits} ciIdentity={props.ci.id} />
    </Tab.Pane> },
  ]

  return (<div style={{margin: "10px 10px"}}>
    <h3>CI "{props.ci.name ?? "[UNNAMED]"}" ({props.ci.id})</h3>
    <TemplateErrors templateErrors={props.ci.templateErrors} 
      onCreateNewAttribute={(attributeName, attributeType) => {
        setCreateNewAttribute({name: attributeName, type: attributeType, value: '', layer: visibleAndWritableLayers[0]}); // TODO: correct layer
      }}
      onOverwriteAttribute={(attributeName, attributeType) => {
        // find current value and layer
        var currentAttribute = props.ci.mergedAttributes.find(a => a.attribute.name === attributeName);
        if (currentAttribute) {
          // TODO: get current correct layer
          const layerID = currentAttribute.layerStackIDs[currentAttribute.layerStackIDs.length - 1];
          const layer = visibleAndWritableLayers.find(l => l.id === layerID);
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
  ci: PropTypes.shape({
    id: PropTypes.string.isRequired,
    name: PropTypes.string,
    templateErrors: PropTypes.shape({
      attributeErrors: PropTypes.arrayOf(PropTypes.shape({
        attributeName: PropTypes.string.isRequired,
        errors: PropTypes.arrayOf(PropTypes.object).isRequired
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