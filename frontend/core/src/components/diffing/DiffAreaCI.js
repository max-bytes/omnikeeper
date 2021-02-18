// import React, {useState} from 'react';
// import _ from 'lodash';
// import DiffAttributeList from 'components/diffing/DiffAttributeList';
// import DiffRelationList from 'components/diffing/DiffRelationList';
// import DiffEffectiveTraitsList from './DiffEffectiveTraitsList';
// import { Tab } from 'semantic-ui-react'

// function compareAttributes(attributeA, attributeB) {
//   if (!attributeA && attributeB) {
//     return {state: 'unequal'};
//   } else if (attributeA && !attributeB) {
//     return {state: 'unequal'};
//   } else {
//     var va = attributeA.attribute.value;
//     var vb = attributeB.attribute.value;
//     let similarAtBest = false;
//     if (va.isArray !== vb.isArray) similarAtBest = true;
//     if (va.type !== vb.type) similarAtBest = true;
//     if (va.values.length !== vb.values.length) similarAtBest = true;

//     for(let i = 0;i < _.min([va.values.length, vb.values.length]);i++) {
//       const ia = va.values[i];
//       const ib = vb.values[i];
//       if (ia !== ib) return {state: 'unequal'}; // TODO: is a simple string comparison enough?
//     }

//     return (similarAtBest) ? {state: 'similar'} : {state: 'equal'};
//   }
// }

// function compareRelations(relationA, relationB) {
//   if (!relationA && relationB) {
//     return {state: 'unequal'};
//   } else if (relationA && !relationB) {
//     return {state: 'unequal'};
//   } else {
//     return {state: 'equal'}; // TODO?
//   }
// }
// function compareEffectiveTraits(etA, etB) {
//   if (!etA && etB) {
//     return {state: 'unequal'};
//   } else if (etA && !etB) {
//     return {state: 'unequal'};
//   } else {
//     return {state: 'equal'}; // TODO: treat different attributes/relations/... as being only similar, not equal
//   }
// }


// function Attributes(props) {
//   var leftD = _.keyBy(props.leftAttributes, a => a.attribute.name);
//   var rightD = _.keyBy(props.rightAttributes, a => a.attribute.name);

//   var keys = _.union(_.keys(leftD), _.keys(rightD));

//   let mergedAttributes = {};
//   for(const key of keys) {
//     mergedAttributes[key] = { left: leftD[key], right: rightD[key], compareResult: compareAttributes(leftD[key], rightD[key]) };
//   }
//   if (!props.showEqual) {
//     mergedAttributes = _.pickBy(mergedAttributes, (v, k) => v.compareResult.state !== 'equal');
//   }
//   return <DiffAttributeList attributes={mergedAttributes} />;
// }

// function Relations(props) {
//   var leftD = _.keyBy(props.leftRelated, r => `${r.predicateID}-${r.ci.id}`);
//   var rightD = _.keyBy(props.rightRelated, r => `${r.predicateID}-${r.ci.id}`);

//   var keys = _.union(_.keys(leftD), _.keys(rightD));

//   let mergedRelations = {};
//   for(const key of keys) {
//     mergedRelations[key] = { key: key, left: leftD[key], right: rightD[key], compareResult: compareRelations(leftD[key], rightD[key]) };
//   }
//   if (!props.showEqual) {
//     mergedRelations = _.pickBy(mergedRelations, (v, k) => v.compareResult.state !== 'equal');
//   }
//   return <DiffRelationList relations={mergedRelations} />;
// }

// function EffectiveTraits(props) {
//   var leftD = _.keyBy(props.leftEffectiveTraits, r => `${r.underlyingTrait.name}`);
//   var rightD = _.keyBy(props.rightEffectiveTraits, r => `${r.underlyingTrait.name}`);

//   var keys = _.union(_.keys(leftD), _.keys(rightD));

//   let mergedETs = {};
//   for(const key of keys) {
//     mergedETs[key] = { key: key, left: leftD[key], right: rightD[key], compareResult: compareEffectiveTraits(leftD[key], rightD[key]) };
//   }
//   if (!props.showEqual) {
//     mergedETs = _.pickBy(mergedETs, (v, k) => v.compareResult.state !== 'equal');
//   }
//   return <DiffEffectiveTraitsList effectiveTraits={mergedETs} />;
// }

// export function DiffAreaCI(props) {
  
//   const [selectedTab, setSelectedTab] = useState(0);

//   if (!props.leftCI || !props.rightCI) {
//     return <div style={{minHeight: '200px'}}>&nbsp;</div>;
//   } else {
    
//     const panes = [
//       { menuItem: 'Attributes', render: () => <Tab.Pane>
//         <Attributes leftAttributes={props.leftCI.mergedAttributes} rightAttributes={props.rightCI.mergedAttributes} showEqual={props.showEqual} />
//       </Tab.Pane> },
//       { menuItem: 'Relations', render: () => <Tab.Pane>
//         <Relations leftRelated={props.leftCI.related} rightRelated={props.rightCI.related} showEqual={props.showEqual} />
//       </Tab.Pane> },
//       { menuItem: 'Effective Traits', render: () => <Tab.Pane>
//         <EffectiveTraits leftEffectiveTraits={props.leftCI.effectiveTraits} rightEffectiveTraits={props.rightCI.effectiveTraits} showEqual={props.showEqual} />
//       </Tab.Pane> },
//     ]

//     return <Tab activeIndex={selectedTab} onTabChange={(e, {activeIndex}) => setSelectedTab(activeIndex)} panes={panes} />;
//   }
// }
