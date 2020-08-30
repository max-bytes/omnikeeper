import React, {useState} from 'react';
import _ from 'lodash';
import DiffAttributeList from 'components/diffing/DiffAttributeList';
import DiffRelationList from 'components/diffing/DiffRelationList';
import DiffEffectiveTraitsList from './DiffEffectiveTraitsList';
import { Tab, Header } from 'semantic-ui-react'

export function DiffArea(props) {
  
  const [selectedTab, setSelectedTab] = useState(0);

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
      var leftD = _.keyBy(m.left?.related, r => `${r.predicateID}-${r.ci.id}`);
      var rightD = _.keyBy(m.right?.related, r => `${r.predicateID}-${r.ci.id}`);

      var keys = _.union(_.keys(leftD), _.keys(rightD));

      let mergedRelations = {};
      for(const key of keys) {
        mergedRelations[key] = { key: key, left: leftD[key], right: rightD[key], compareResult: compareRelations(leftD[key], rightD[key]) };
      }
      if (!props.showEqual) {
        mergedRelations = _.pickBy(mergedRelations, (v, k) => v.compareResult.state !== 'equal');
      }
      return {...m, mergedRelations };
    });

    
    mergedCIs = _.mapValues(mergedCIs, m => {
      var leftD = _.keyBy(m.left?.effectiveTraits, r => `${r.underlyingTrait.name}`);
      var rightD = _.keyBy(m.right?.effectiveTraits, r => `${r.underlyingTrait.name}`);

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

    const panes = [
      { menuItem: 'Attributes', render: () => <Tab.Pane>
        {_.map(mergedCIs, (m, ciid) => {
          if (!props.showEqual && _.size(m.mergedAttributes) === 0)
            return <div key={ciid}></div>;
          return <div key={ciid} style={{marginTop: '1.5rem'}}>
            <Header as='h4' style={{marginBottom: 0}}>{m.name} - {ciid}</Header>
            <DiffAttributeList attributes={m.mergedAttributes} />
          </div>;
        })}
      </Tab.Pane> },
      { menuItem: 'Relations', render: () => <Tab.Pane>
        {_.map(mergedCIs, (m, ciid) => {
          if (!props.showEqual && _.size(m.mergedRelations) === 0)
            return <div key={ciid}></div>;
          return <div key={ciid} style={{marginTop: '1.5rem'}}>
            <Header as='h4' style={{marginBottom: 0}}>{m.name} - {ciid}</Header>
            <DiffRelationList relations={m.mergedRelations} />
          </div>;
        })}
      </Tab.Pane> },
      { menuItem: 'Effective Traits', render: () => <Tab.Pane>
        {_.map(mergedCIs, (m, ciid) => {
          if (!props.showEqual && _.size(m.mergedETs) === 0)
            return <div key={ciid}></div>;
          return <div key={ciid} style={{marginTop: '1.5rem'}}>
            <Header as='h4' style={{marginBottom: 0}}>{m.name} - {ciid}</Header>
            <DiffEffectiveTraitsList effectiveTraits={m.mergedETs} />
          </div>
        })}
      </Tab.Pane> },
    ]

    return <Tab activeIndex={selectedTab} onTabChange={(e, {activeIndex}) => setSelectedTab(activeIndex)} panes={panes} />;
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
    return {state: 'equal'}; // TODO: treat different attributes/relations/... as being only similar, not equal
  }
}
