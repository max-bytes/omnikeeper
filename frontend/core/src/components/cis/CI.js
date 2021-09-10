import React, { useState } from 'react';
import PropTypes from 'prop-types'
import { Row, Col } from "antd";
import AddNewAttribute from './AddNewAttribute';
import ExplorerAttributeList from './ExplorerAttributeList';
import CIRelations from './CIRelations';
import EffectiveTraits from './EffectiveTraits';
import { Tabs } from 'antd'
import { useExplorerLayers } from 'utils/layers';
import { CIID } from 'utils/uuidRenderers';
import CountBadge from 'components/CountBadge';

const { TabPane } = Tabs;

function CI(props) {

  const [createNewAttribute, setCreateNewAttribute] = useState(undefined);
  const { data: visibleAndWritableLayers } = useExplorerLayers(true, true);
  const { data: visibleLayers } = useExplorerLayers(true);

  const panes = (<>
    <TabPane tab={<CountBadge count={props.ci.mergedAttributes.length}>Attributes</CountBadge>} key="attributes">
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

    <TabPane tab={<CountBadge count={props.ci.related.length}>Relations</CountBadge>} key="relations">
      <CIRelations relatedCIs={props.ci.related} isEditable={props.isEditable} ciIdentity={props.ci.id} />
    </TabPane>
    <TabPane tab={<CountBadge count={props.ci.effectiveTraits.length}>Effective Traits</CountBadge>} key="effectiveTraits">
      <EffectiveTraits timeThreshold={props.timeThreshold} traits={props.ci.effectiveTraits} ciIdentity={props.ci.id} />
    </TabPane>
  </>)

  return (<div style={{margin: "10px 10px"}}>
    <h2>CI "{props.ci.name ?? "[UNNAMED]"}" <CIID id={props.ci.id} link={false} /></h2>
    <Tabs defaultActiveKey={"attributes"} style={{padding: "1rem"}}>{panes}</Tabs>
  </div>);
}

CI.propTypes = {
  isEditable: PropTypes.bool.isRequired,
  ci: PropTypes.shape({
    id: PropTypes.string.isRequired,
    name: PropTypes.string,
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