import React from 'react';
import PropTypes from 'prop-types'
import Attribute from './Attribute';

function CIs(props) {

  return props.cis.map(ci => (

    <div key={ci.identity} style={{border: "1px solid black", margin: "10px 10px", padding: "0px 5px"}}>
        <b>CI {ci.identity}</b>
        {ci.attributes.map(a => (
          <Attribute attribute={a} ciIdentity={ci.identity} layers={props.layers} key={a.name}></Attribute>
        ))}
    </div>
  ));
}

CIs.propTypes = {
  layers: PropTypes.arrayOf(PropTypes.string).isRequired,
  cis: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.number.isRequired,
      identity: PropTypes.string.isRequired,
      attributes: PropTypes.arrayOf(
        PropTypes.shape({
          name: PropTypes.string.isRequired,
          layerID: PropTypes.number.isRequired,
          state: PropTypes.string.isRequired,
          value: PropTypes.shape({
            __typename: PropTypes.string.isRequired,
            value: PropTypes.oneOfType([
              PropTypes.string,
              PropTypes.number,
              PropTypes.bool
            ]).isRequired
          })
        })
      )
    }).isRequired
  ).isRequired,
}

export default CIs;