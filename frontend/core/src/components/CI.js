import React, { useState } from 'react';
import PropTypes from 'prop-types'
import { Row, Col } from "antd";
import AddNewAttribute from './AddNewAttribute';
import ExplorerAttributeList from './ExplorerAttributeList';
import TemplateErrors from './TemplateErrors';
import CIRelations from './CIRelations';
import EffectiveTraits from './EffectiveTraits';
import { Tabs } from 'antd'
import { useExplorerLayers } from '../utils/layers';
import { CIID } from 'utils/uuidRenderers';

const { TabPane } = Tabs;

function CI(props) {

  const [createNewAttribute, setCreateNewAttribute] = useState(undefined);
  const { data: visibleAndWritableLayers } = useExplorerLayers(true, true);
  const { data: visibleLayers } = useExplorerLayers(true);
    
  const panes = (<>
    <TabPane tab="Attributes" key="attributes">
      <Row>
        <Col span={24}>
          <AddNewAttribute prefilled={createNewAttribute} isEditable={props.isEditable} ciIdentity={props.ci.id}></AddNewAttribute>
        </Col>
      </Row>
      <Row>
        <Col span={24}>
          <ExplorerAttributeList mergedAttributes={props.ci.mergedAttributes} isEditable={props.isEditable} 
            ciIdentity={props.ci.id} visibleAndWritableLayers={visibleAndWritableLayers} visibleLayers={visibleLayers} />
        </Col>
      </Row>
    </TabPane>

    <TabPane tab="Relations" key="relations">
      <CIRelations timeThreshold={props.timeThreshold} isEditable={props.isEditable} ciIdentity={props.ci.id} />
    </TabPane>
    <TabPane tab="Effective Traits" key="effectiveTraits">
      <EffectiveTraits timeThreshold={props.timeThreshold} traits={props.ci.effectiveTraits} ciIdentity={props.ci.id} />
    </TabPane>
  </>)

  return (<div style={{margin: "10px 10px"}}>
    <h2>CI "{props.ci.name ?? "[UNNAMED]"}" <CIID id={props.ci.id} link={false} /></h2>
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
    <Tabs defaultActiveKey={"attributes"} style={{padding: "1rem"}}>{panes}</Tabs>
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