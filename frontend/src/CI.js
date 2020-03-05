import React from 'react';
import PropTypes from 'prop-types'
import Attribute from './Attribute';
import RelatedCI from './RelatedCI';
import {Row, Col} from 'react-bootstrap';
import AddNewAttribute from './AddNewAttribute';
import AddNewRelation from './AddNewRelation';
import { Flipper, Flipped } from 'react-flip-toolkit'
import { Tab } from 'semantic-ui-react'

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

  var sortedAttributes = [...props.ci.attributes];
  sortedAttributes.sort((a,b) => {
    return a.name.localeCompare(b.name);
  });
  
  var sortedRelatedCIs = [...props.ci.related];
  sortedRelatedCIs.sort((a,b) => {
    const predicateCompare = a.relation.predicate.localeCompare(b.relation.predicate);
    if (predicateCompare !== 0)
      return predicateCompare;
    return a.ci.identity.localeCompare(b.ci.identity);
  });

  const panes = [
    { menuItem: 'Attributes', render: () => <Tab.Pane>
      <Row>
        <Col>
          <AddNewAttribute isEditable={props.isEditable} layers={props.layers} ciIdentity={props.ci.identity}></AddNewAttribute>
        </Col>
      </Row>
      <Row>
        <Col>
        <Flipper flipKey={sortedAttributes.map(a => a.layerStackIDs).join(' ')}>
          {sortedAttributes.map(a => (
            <Flipped key={a.name} flipId={a.name} onAppear={onAppear} onExit={onExit}>
              <Attribute attribute={a} ciIdentity={props.ci.identity} layers={props.layers} isEditable={props.isEditable}></Attribute>
            </Flipped>
          ))}
        </Flipper>
        </Col>
      </Row>
    </Tab.Pane> },
    { menuItem: 'Relations', render: () => <Tab.Pane>
      <Row>
        <Col>
          <AddNewRelation isEditable={props.isEditable} layers={props.layers} ciIdentity={props.ci.identity}></AddNewRelation>
        </Col>
      </Row>
      <Row>
        <Col>
          <Flipper flipKey={sortedRelatedCIs.map(r => r.relation.layerStackIDs).join(' ')}>
            {sortedRelatedCIs.map(r => (
              <Flipped key={r.relation.id} flipId={r.relation.predicate} onAppear={onAppear} onExit={onExit}>
                <RelatedCI related={r} ciIdentity={props.ci.identity} layers={props.layers} isEditable={props.isEditable}></RelatedCI>
              </Flipped>
            ))}
          </Flipper>
        </Col>
      </Row>
    </Tab.Pane> },
  ]

  return (<div style={{margin: "10px 10px"}}>
    <h3>CI {props.ci.identity}</h3>
    <Tab panes={panes} />
  </div>);
}

CI.propTypes = {
  isEditable: PropTypes.bool.isRequired,
  layers: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.number.isRequired,
      name: PropTypes.string.isRequired,
      visibility: PropTypes.bool.isRequired
    }).isRequired
  ).isRequired,
  ci: PropTypes.shape({
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