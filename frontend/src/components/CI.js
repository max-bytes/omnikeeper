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
    const predicateCompare = a.relation.predicate.localeCompare(b.relation.predicate);
    if (predicateCompare !== 0)
      return predicateCompare;
    return a.ci.identity.localeCompare(b.ci.identity);
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
        <AttributeList attributes={props.ci.attributes} isEditable={props.isEditable} layers={props.layers} ciIdentity={props.ci.identity}></AttributeList>
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