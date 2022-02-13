import React from 'react';
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

  const { data: visibleAndWritableLayers } = useExplorerLayers(true, true);
  const { data: visibleLayers } = useExplorerLayers(true);

  const panes = (<>
    <TabPane tab={<CountBadge count={props.ci.mergedAttributes.length}>Attributes</CountBadge>} key="attributes">
      <Row>
        <Col span={24}>
          <AddNewAttribute isEditable={props.isEditable} ciIdentity={props.ci.id}></AddNewAttribute>
        </Col>
      </Row>
      <Row>
        <Col span={24}>
          <ExplorerAttributeList mergedAttributes={props.ci.mergedAttributes} isEditable={props.isEditable} 
            ciIdentity={props.ci.id} visibleAndWritableLayers={visibleAndWritableLayers} visibleLayers={visibleLayers} />
        </Col>
      </Row>
    </TabPane>

    <TabPane tab={<CountBadge count={props.ci.outgoingMergedRelations.length}>Outgoing Relations</CountBadge>} key="outgoingRelations">
      <CIRelations mergedRelations={props.ci.outgoingMergedRelations} isEditable={props.isEditable} areOutgoingRelations={true} ciIdentity={props.ci.id} />
    </TabPane>
    <TabPane tab={<CountBadge count={props.ci.incomingMergedRelations.length}>Incoming Relations</CountBadge>} key="incomingRelations">
      <CIRelations mergedRelations={props.ci.incomingMergedRelations} isEditable={props.isEditable} areOutgoingRelations={false} ciIdentity={props.ci.id} />
    </TabPane>
    <TabPane tab={<CountBadge count={props.ci.effectiveTraits.length}>Effective Traits</CountBadge>} key="effectiveTraits">
      <EffectiveTraits timeThreshold={props.timeThreshold} traits={props.ci.effectiveTraits} ciIdentity={props.ci.id} />
    </TabPane>
  </>)

  return (
    <>
      <h2>{props.ci.name ?? "[UNNAMED]"} - <CIID id={props.ci.id} link={false} copyable={true} /></h2>
      <Tabs defaultActiveKey={"attributes"} style={{flex: "1"}}>{panes}</Tabs>
    </>
  );
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