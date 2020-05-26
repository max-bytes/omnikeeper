import React, {useState} from 'react';
import _ from 'lodash';
import DiffAttributeList from 'components/diffing/DiffAttributeList';
import { Tab } from 'semantic-ui-react'

function compare(attributeA, attributeB) {
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

export function DiffArea(props) {
  
  const [selectedTab, setSelectedTab] = useState(0);

  if (!props.leftCI || !props.rightCI) {
    return <div style={{minHeight: '200px'}}>&nbsp;</div>;
  } else {
    var leftD = _.keyBy(props.leftCI.mergedAttributes, a => a.attribute.name);
    var rightD = _.keyBy(props.rightCI.mergedAttributes, a => a.attribute.name);

    var keys = _.union(_.keys(leftD), _.keys(rightD));

    let merged = {};
    for(const key of keys) {
      merged[key] = { left: leftD[key], right: rightD[key], compareResult: compare(leftD[key], rightD[key]) };
    }

    if (!props.showEqual) {
      merged = _.pickBy(merged, (v, k) => v.compareResult.state !== 'equal');
    }

    if (_.size(merged) === 0)
      return (<div style={{display: 'flex', justifyContent: 'center', marginLeft: '220px', padding: '20px', fontSize: '1.4rem', fontWeight: 'bold'}}>Empty</div>);


    const panes = [
      { menuItem: 'Attributes', render: () => <Tab.Pane>
        <DiffAttributeList attributes={merged} />
      </Tab.Pane> },
      { menuItem: 'Relations', render: () => <Tab.Pane>
        TODO
      </Tab.Pane> },
      { menuItem: 'Effective Traits', render: () => <Tab.Pane>
        TODO
      </Tab.Pane> },
    ]

    return <Tab activeIndex={selectedTab} onTabChange={(e, {activeIndex}) => setSelectedTab(activeIndex)} panes={panes} />;
  }
}
