import React from 'react';
import PropTypes from 'prop-types'
import Attribute from './Attribute';
import RelatedCI from './RelatedCI';
import {Row, Col} from 'react-bootstrap';
import AddNewAttribute from './AddNewAttribute';
import { Flipper, Flipped } from 'react-flip-toolkit'

function CI(props) {

    /**
   * Thin wrapper around Element.animate() that returns a Promise
   * @param el Element to animate
   * @param keyframes The keyframes to use when animating
   * @param options Either the duration of the animation or an options argument detailing how the animation should be performed
   * @returns A promise that will resolve after the animation completes or is cancelled
   */
  function animate(
    el,
    keyframes,
    options,
  ) {
    return new Promise(resolve => {
        const anim = el.animate(keyframes, options);
        anim.addEventListener("finish", () => resolve());
        anim.addEventListener("cancel", () => resolve());
    });
  }

  async function onAppear(el) {
      await animate(el, [
          {opacity: 0},
          {opacity: 1}
      ], {
          duration: 200
      });
      el.style.opacity = "1";
  }
  async function onExit(el, _idx, onComplete) {
      await animate(el, [
          {opacity: 1},
          {opacity: 0}
      ], {
          duration: 200
      });
      onComplete();
  }


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
        <Flipper flipKey={props.ci.attributes.map(a => a.layerstackIDs).join(' ')}>
          {props.ci.attributes.map(a => (
            <Flipped key={a.name} flipId={a.name} onAppear={onAppear} onExit={onExit}>
              <Attribute attribute={a} ciIdentity={props.ci.identity} layers={props.layers}></Attribute>
            </Flipped>
          ))}
        </Flipper>
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