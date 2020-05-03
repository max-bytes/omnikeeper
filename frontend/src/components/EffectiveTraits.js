import React from 'react';
import ReactJson from 'react-json-view'
import _ from 'lodash';

function EffectiveTraits(props) {
  return <div>
    <p>TODO: make pretty</p>
      {props.traits.map((t, index) => {
      return (<div key={index}>
        
        <h3>{t.underlyingTrait.name}</h3>
        {t.attributes.map(a => (<div key={a.attribute.name}>
          <div>{a.attribute.name}:</div>
          <ReactJson name={false} src={_.pick(a.attribute.value, ['type', 'isArray', 'values'])} enableClipboard={false} />
          </div>))}
        </div>);
      })}
    </div>;
}

export default EffectiveTraits;