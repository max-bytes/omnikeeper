import React from 'react';

function Traits(props) {
  return <div>{props.traits.map(t => {
    return <li key={t.underlyingTrait.name}>{t.underlyingTrait.name}</li>;
  })}</div>;
}

export default Traits;