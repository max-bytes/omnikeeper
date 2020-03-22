import React, { useState } from 'react';
import PropTypes from 'prop-types'
import RelatedCI from './RelatedCI';
import {Row, Col} from 'react-bootstrap';
import AddNewAttribute from './AddNewAttribute';
import AttributeList from './AttributeList';
import AddNewRelation from './AddNewRelation';
import { Flipper, Flipped } from 'react-flip-toolkit'
import { Tab } from 'semantic-ui-react'
import { onAppear, onExit } from '../utils/animation';

function CI(props) {

  var sortedRelatedCIs = [...props.ci.related];
  sortedRelatedCIs.sort((a,b) => {
    const predicateCompare = a.relation.predicate.id.localeCompare(b.relation.predicate.id);
    if (predicateCompare !== 0)
      return predicateCompare;
    return a.ciid.localeCompare(b.ciid);
  });

  const [selectedTab, setSelectedTab] = useState(0);

  const panes = [
    { menuItem: 'Attributes', render: () => <Tab.Pane>
      <Row>
        <Col>
          <AddNewAttribute isEditable={props.isEditable} layers={props.layers} ciIdentity={props.ci.identity}></AddNewAttribute>
        </Col>
      </Row>
      <Row>
        <Col>
        <AttributeList mergedAttributes={props.ci.mergedAttributes} isEditable={props.isEditable} layers={props.layers} ciIdentity={props.ci.identity}></AttributeList>
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
              <Flipped key={r.relation.id} flipId={r.relation.predicateID} onAppear={onAppear} onExit={onExit}>
                <RelatedCI related={r} ciIdentity={props.ci.identity} layers={props.layers} isEditable={props.isEditable}></RelatedCI>
              </Flipped>
            ))}
          </Flipper>
        </Col>
      </Row>
    </Tab.Pane> },
  ]

  return (<div style={{margin: "10px 10px"}}>
    <h3>CI {props.ci.identity} - type: {props.ci.type.id}</h3>
    <Tab activeIndex={selectedTab} onTabChange={(e, {activeIndex}) => setSelectedTab(activeIndex)} panes={panes} />
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
    type: PropTypes.shape({
      id: PropTypes.string.isRequired
    }).isRequired,
    attributes: PropTypes.arrayOf(
      PropTypes.shape({
        attribute: PropTypes.shape({
          name: PropTypes.string.isRequired,
          state: PropTypes.string.isRequired,
          value: PropTypes.shape({
            type: PropTypes.string.isRequired,
            value: PropTypes.string.isRequired
          })
        })
      })
    )
  }).isRequired
}

export default CI;