import React from 'react';
import PropTypes from 'prop-types'
import Attribute from './Attribute';
import RelatedCI from './RelatedCI';
import {Row, Col} from 'react-bootstrap';
import AddNewAttribute from './AddNewAttribute';

function CI(props) {
    return (<div key={props.ci.identity} style={{border: "1px solid black", margin: "10px 10px", padding: "0px 5px"}}>
      <h3>CI {props.ci.identity}</h3>
      <Row>
        <Col>
        <b>Relations:</b>
        {props.ci.related.map(r => (
          <RelatedCI related={r} key={r.relation.predicate}></RelatedCI>
        ))}
        </Col>
      </Row>
      <Row>
        <Col>
        <b>Attributes:</b>
        {props.ci.attributes.map(a => (
          <Attribute attribute={a} ciIdentity={props.ci.identity} layers={props.layers} key={a.name}></Attribute>
        ))}
        </Col>
      </Row>
      <Row>
        <Col>
          <AddNewAttribute layers={props.layers} ciIdentity={props.ci.identity}></AddNewAttribute>
        </Col>
      </Row>
    </div>
  );
}

CI.propTypes = {
  layers: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.number.isRequired,
      name: PropTypes.string.isRequired,
      visibility: PropTypes.bool.isRequired
    }).isRequired
  ).isRequired,
  ci: PropTypes.shape({
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
}

export default CI;