import React from 'react';
import ReactJson from 'react-json-view'
import _ from 'lodash';

function Attributes(props) {
  return <>
    <h5 style={{margin: '0px', paddingLeft: '15px'}}>Attributes:</h5>
    {props.attributes.map(a => (<div key={a.attribute.name} style={{paddingLeft: '30px'}}>
    <div>{a.attribute.name}</div>
    <ReactJson collapsed={0} name={false} src={_.pick(a.attribute.value, ['type', 'isArray', 'values'])} enableClipboard={false} />
    </div>))}
  </>;
}

// function DependentTraits(props) {
//   return <>
//     <h5 style={{margin: '0px', paddingLeft: '15px'}}>Dependent Traits:</h5>
//     {props.dependentTraits.map(dt => (<div key={dt} style={{paddingLeft: '30px'}}>
//       {dt}
//     </div>))}
//     </>;
// }

function EffectiveTraits(props) {
  return <div>
      {props.traits.map((t, index) => {
        // TODO: show required relations
        return (<div key={index} style={{marginBottom: '30px'}}>
          <h3 style={{margin: '0px'}}>{t.underlyingTrait.name}:</h3>
          
          {t.attributes.length > 0 && <Attributes attributes={t.attributes} />}
          // TODO: relations, etc.
          {/* {t.dependentTraits.length > 0 && <DependentTraits dependentTraits={t.dependentTraits} />} */}
        </div>);
      })}
    </div>;
}

export default EffectiveTraits;