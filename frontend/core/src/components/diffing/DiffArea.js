import React from 'react';
import _ from 'lodash';
import DiffAttributeList from 'components/diffing/DiffAttributeList';
import DiffRelationList from 'components/diffing/DiffRelationList';
import DiffEffectiveTraitsList from './DiffEffectiveTraitsList';
import { Tabs, Typography } from 'antd'
import { CIID } from 'utils/uuidRenderers';
import CountBadge from 'components/CountBadge';

const { TabPane } = Tabs;
const { Title } = Typography;

export function DiffArea(props) {

  const {diffResults} = props;
  
  if (!diffResults) {
    return <div style={{minHeight: '200px'}}>&nbsp;</div>;
  } else {
    const mergedCIs = diffResults.cis;
    const outgoingRelations = diffResults.outgoingRelations;
    const incomingRelations = diffResults.incomingRelations;
    const effectiveTraits = diffResults.effectiveTraits;

    const panes = (<>
      <TabPane tab={<CountBadge count={_.sumBy(mergedCIs, ci => _.size(ci.attributeComparisons))}>Attributes</CountBadge>} key="attributes">
        {_.map(mergedCIs, (ci) => {
          if (_.size(ci.attributeComparisons) === 0)
            return <div key={ci.ciid}></div>;
          return <div key={ci.ciid} style={{marginTop: '1.5rem'}}>
            <Title level={5} style={{marginBottom: 0}}>{ci.left.name ?? ci.right.name ?? "[UNNAMED]"} - <CIID id={ci.ciid} link={true} /></Title>
            <DiffAttributeList attributes={ci.attributeComparisons} />
          </div>;
        })}
      </TabPane>
      <TabPane tab={<CountBadge count={_.sumBy(outgoingRelations, ci => _.size(ci.relationComparisons))}>Outgoing Relations</CountBadge>} key="outgoingRelations">
        {_.map(outgoingRelations, ci => {
          if (_.size(ci.relationComparisons) === 0)
            return <div key={ci.ciid}></div>;
          return <div key={ci.ciid} style={{marginTop: '1.5rem'}}>
            <Title level={5} style={{marginBottom: 0}}><CIID id={ci.ciid} link={true} /></Title>
            <DiffRelationList relations={ci.relationComparisons} areOutgoingRelations={true} />
          </div>;
        })}
      </TabPane>
      <TabPane tab={<CountBadge count={_.sumBy(incomingRelations, ci => _.size(ci.relationComparisons))}>Incoming Relations</CountBadge>} key="incomingRelations">
        {_.map(incomingRelations, ci => {
          if (_.size(ci.relationComparisons) === 0)
            return <div key={ci.ciid}></div>;
          return <div key={ci.ciid} style={{marginTop: '1.5rem'}}>
            <Title level={5} style={{marginBottom: 0}}><CIID id={ci.ciid} link={true} /></Title>
            <DiffRelationList relations={ci.relationComparisons} areOutgoingRelations={false} />
          </div>;
        })}
      </TabPane>
      <TabPane tab={<CountBadge count={_.sumBy(effectiveTraits, ci => _.size(ci.effectiveTraitComparisons))}>Effective Traits</CountBadge>} key="effectiveTraits">
        {_.map(effectiveTraits, ci => {
          if (_.size(ci.effectiveTraitComparisons) === 0)
            return <div key={ci.ciid}></div>;
          return <div key={ci.ciid} style={{marginTop: '1.5rem'}}>
            <Title level={5} style={{marginBottom: 0}}><CIID id={ci.ciid} link={true} /></Title>
            <DiffEffectiveTraitsList effectiveTraits={ci.effectiveTraitComparisons} />
          </div>
        })}
      </TabPane>
    </>)

    return <Tabs defaultActiveKey={"attributes"} style={{padding: "1rem"}}>{panes}</Tabs>;
  }
}
