import React from 'react';
import _ from 'lodash';
import DiffAttributeList from 'components/diffing/DiffAttributeList';
import DiffRelationList from 'components/diffing/DiffRelationList';
import DiffEffectiveTraitsList from './DiffEffectiveTraitsList';
import { Tabs, Typography } from 'antd'
import { CIID } from 'utils/uuidRenderers';

const { TabPane } = Tabs;
const { Title } = Typography;

export function DiffArea(props) {
  
  if (!props.leftCIs || !props.rightCIs) {
    return <div style={{minHeight: '200px'}}>&nbsp;</div>;
  } else {

    var leftD = _.keyBy(props.leftCIs, a => a.id);
    var rightD = _.keyBy(props.rightCIs, a => a.id);

    var keys = _.union(_.keys(leftD), _.keys(rightD));

    let mergedCIs = {};
    for(const key of keys) {
      const leftName = leftD[key]?.name;
      const rightName = rightD[key]?.name;
      mergedCIs[key] = { left: leftD[key], right: rightD[key], name: leftName ?? rightName ?? "[UNNAMED]" };
    }

    mergedCIs = _.mapValues(mergedCIs, m => {
      var leftD = _.keyBy(m.left?.mergedAttributes, a => a.attribute.name);
      var rightD = _.keyBy(m.right?.mergedAttributes, a => a.attribute.name);

      var keys = _.union(_.keys(leftD), _.keys(rightD));

      let mergedAttributes = {};
      for(const key of keys) {
        mergedAttributes[key] = { left: leftD[key], right: rightD[key], compareResult: compareAttributes(leftD[key], rightD[key]) };
      }
      if (!props.showEqual) {
        mergedAttributes = _.pickBy(mergedAttributes, (v, k) => v.compareResult.state !== 'equal');
      }
      return {...m, mergedAttributes };
    });

    mergedCIs = _.mapValues(mergedCIs, m => {
      var leftD = _.keyBy(m.left?.incomingMergedRelations, r => `${r.relation.predicateID}-${r.relation.fromCIID}`);
      var rightD = _.keyBy(m.right?.incomingMergedRelations, r => `${r.relation.predicateID}-${r.relation.fromCIID}`);

      var keys = _.union(_.keys(leftD), _.keys(rightD));

      let mergedRelations = {};
      for(const key of keys) {
        mergedRelations[key] = { key: key, left: leftD[key], right: rightD[key], compareResult: compareRelations(leftD[key], rightD[key]) };
      }
      if (!props.showEqual) {
        mergedRelations = _.pickBy(mergedRelations, (v, k) => v.compareResult.state !== 'equal');
      }
      return {...m, incomingMergedRelations: mergedRelations };
    });

    mergedCIs = _.mapValues(mergedCIs, m => {
      var leftD = _.keyBy(m.left?.outgoingMergedRelations, r => `${r.relation.predicateID}-${r.relation.toCIID}`);
      var rightD = _.keyBy(m.right?.outgoingMergedRelations, r => `${r.relation.predicateID}-${r.relation.toCIID}`);

      var keys = _.union(_.keys(leftD), _.keys(rightD));

      let mergedRelations = {};
      for(const key of keys) {
        mergedRelations[key] = { key: key, left: leftD[key], right: rightD[key], compareResult: compareRelations(leftD[key], rightD[key]) };
      }
      if (!props.showEqual) {
        mergedRelations = _.pickBy(mergedRelations, (v, k) => v.compareResult.state !== 'equal');
      }
      return {...m, outgoingMergedRelations: mergedRelations };
    });

    // TODO: outgoing relations
    
    mergedCIs = _.mapValues(mergedCIs, m => {
      var leftD = _.keyBy(m.left?.effectiveTraits, r => `${r.underlyingTrait.id}`);
      var rightD = _.keyBy(m.right?.effectiveTraits, r => `${r.underlyingTrait.id}`);

      var keys = _.union(_.keys(leftD), _.keys(rightD));

      let mergedETs = {};
      for(const key of keys) {
        mergedETs[key] = { key: key, left: leftD[key], right: rightD[key], compareResult: compareEffectiveTraits(leftD[key], rightD[key]) };
      }
      if (!props.showEqual) {
        mergedETs = _.pickBy(mergedETs, (v, k) => v.compareResult.state !== 'equal');
      }
      return {...m, mergedETs };
    });

    const panes = (<>
      <TabPane tab="Attributes" key="attributes">
        {_.map(mergedCIs, (m, ciid) => {
          if (!props.showEqual && _.size(m.mergedAttributes) === 0)
            return <div key={ciid}></div>;
          return <div key={ciid} style={{marginTop: '1.5rem'}}>
            <Title level={5} style={{marginBottom: 0}}>{m.name} - <CIID id={ciid} link={true} /></Title>
            <DiffAttributeList attributes={m.mergedAttributes} />
          </div>;
        })}
      </TabPane>
      <TabPane tab="Outgoing Relations" key="outgoingRelations">
        {_.map(mergedCIs, (m, ciid) => {
          if (!props.showEqual && _.size(m.outgoingMergedRelations) === 0)
            return <div key={ciid}></div>;
          return <div key={ciid} style={{marginTop: '1.5rem'}}>
            <Title level={5} style={{marginBottom: 0}}>{m.name} - <CIID id={ciid} link={true} /></Title>
            <DiffRelationList relations={m.outgoingMergedRelations} areOutgoingRelations={true} />
          </div>;
        })}
      </TabPane>
      <TabPane tab="Incoming Relations" key="incomingRelations">
        {_.map(mergedCIs, (m, ciid) => {
          if (!props.showEqual && _.size(m.incomingMergedRelations) === 0)
            return <div key={ciid}></div>;
          return <div key={ciid} style={{marginTop: '1.5rem'}}>
            <Title level={5} style={{marginBottom: 0}}>{m.name} - <CIID id={ciid} link={true} /></Title>
            <DiffRelationList relations={m.incomingMergedRelations} areOutgoingRelations={false} />
          </div>;
        })}
      </TabPane>
      <TabPane tab="Effective Traits" key="effectiveTraits">
        {_.map(mergedCIs, (m, ciid) => {
          if (!props.showEqual && _.size(m.mergedETs) === 0)
            return <div key={ciid}></div>;
          return <div key={ciid} style={{marginTop: '1.5rem'}}>
            <Title level={5} style={{marginBottom: 0}}>{m.name} - <CIID id={ciid} link={true} /></Title>
            <DiffEffectiveTraitsList effectiveTraits={m.mergedETs} />
          </div>
        })}
      </TabPane>
    </>)

    return <Tabs defaultActiveKey={"attributes"} style={{padding: "1rem"}}>{panes}</Tabs>;
  }
}

function compareAttributes(attributeA, attributeB) {
  if (!attributeA && attributeB) {
    return {state: 'unequal'};
  } else if (attributeA && !attributeB) {
    return {state: 'unequal'};
  } else {
    var va = attributeA.attribute.value;
    var vb = attributeB.attribute.value;
    let similarAtBest = false;
    if (va.isArray !== vb.isArray) similarAtBest = true;
    if (va.type !== vb.type) similarAtBest = true;
    if (va.values.length !== vb.values.length) similarAtBest = true;

    for(let i = 0;i < _.min([va.values.length, vb.values.length]);i++) {
      const ia = va.values[i];
      const ib = vb.values[i];
      if (ia !== ib) return {state: 'unequal'}; // TODO: is a simple string comparison enough?
    }

    return (similarAtBest) ? {state: 'similar'} : {state: 'equal'};
  }
}

function compareRelations(relationA, relationB) {
  if (!relationA && relationB) {
    return {state: 'unequal'};
  } else if (relationA && !relationB) {
    return {state: 'unequal'};
  } else {
    return {state: 'equal'}; // TODO?
  }
}
function compareEffectiveTraits(etA, etB) {
  if (!etA && etB) {
    return {state: 'unequal'};
  } else if (etA && !etB) {
    return {state: 'unequal'};
  } else {
    if (etA.underlyingTrait.id === etB.underlyingTrait.id) {
      return {state: 'equal'}; // TODO: treat different attributes/relations/... as being only similar, not equal
    } else {
      return {state: 'unequal'};
    }
  }
}
