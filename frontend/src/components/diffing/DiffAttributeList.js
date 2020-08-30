import React from 'react';
import Attribute from 'components/Attribute';
import { Flipper, Flipped } from 'react-flip-toolkit'
import _ from 'lodash';
import { Accordion, Icon } from 'semantic-ui-react'
import { onAppear, onExit } from 'utils/animation';
import { Container, Row, Col } from 'react-bootstrap';
import { MissingLabel, CompareLabel, EmptyLabel, stateBasedBackgroundColor } from './DiffUtilComponents';
import {useAttributeSegmentsToggler} from 'utils/useAttributeSegmentsToggler'

// TODO: consider merging with ExplorerAttributeList?
function DiffAttributeList(props) {

  // TODO: does not work with nested groups yet
  const nestedAttributes = _.groupBy(_.map(props.attributes, (leftRight, name) => ({leftRight, name})), (t) => {
    const splits = t.name.split('.');
    if (splits.length <= 1) return "";
    else return splits.slice(0, -1).join(".");
  });

  const [toggleSegment, isSegmentActive, toggleExpandCollapseAll] = useAttributeSegmentsToggler(_.keys(nestedAttributes));

  if (_.size(props.attributes) === 0)
    return EmptyLabel();

  const attributeAccordionItems = [];
  _.forEach(nestedAttributes, (na, key) => {

    var sortedAttributes = [...na];
    sortedAttributes.sort((a,b) => {
      return a.name.localeCompare(b.name);
    });

    const title = (key === "") ? "__base" : key;

    const ret = (<div key={key}>
      <Accordion.Title active={isSegmentActive(key)} onClick={(_, { index }) => toggleSegment(index)} index={key}>
        <Icon name='dropdown' /> {title}
      </Accordion.Title>
      <Accordion.Content active={isSegmentActive(key)}>
        <Flipper flipKey={sortedAttributes.map(a => a.layerStackIDs).join(' ')}>
          {sortedAttributes.map((a, index) => {
            const state = a.leftRight.compareResult.state;
            return (<Flipped key={a.name} flipId={a.name} onAppear={onAppear} onExit={onExit}>
              <div style={{padding: '5px 0px', backgroundColor: ((index % 2 === 1) ? '#00000009' : '#00000000')}}>
              
                <Container fluid>
                  <Row style={{backgroundColor: stateBasedBackgroundColor(state)}}>
                    <Col xs={'auto'}>
                      <div style={{display: 'flex', width: '220px', minHeight: '38px', alignItems: 'center', justifyContent: 'flex-end'}}>
                        <span className={"pr-1"} style={{whiteSpace: 'nowrap'}}>{a.name}</span>
                      </div>
                    </Col>
                    <Col>
                      {a.leftRight.left && <Attribute controlIdSuffix={'left'} attribute={a.leftRight.left} hideNameLabel={true} isEditable={false} />}
                      {!a.leftRight.left && <MissingLabel /> }
                    </Col>
                    <Col xs={1}>
                      <CompareLabel state={state} />
                    </Col>
                    <Col>
                      {a.leftRight.right && <Attribute controlIdSuffix={'right'} attribute={a.leftRight.right} hideNameLabel={true} isEditable={false} />}
                      {!a.leftRight.right && <MissingLabel /> }
                    </Col>
                  </Row>
                </Container>
              </div>
            </Flipped>);
          })}
        </Flipper>
      </Accordion.Content>
    </div>);

    attributeAccordionItems[key] = ret;
  });

  // sort associative array
  let attributeAccordionItemsSorted = [];
  _.forEach(Object.keys(attributeAccordionItems).sort(), (value) => {
    attributeAccordionItemsSorted[value] = attributeAccordionItems[value];
  })

  return (
    <>
       {/* <div className={"d-flex align-items-end flex-column mb-2"} >
            <Button
                size={"tiny"}
                onClick={() => toggleExpandCollapseAll()}
            >
                Expand/Collapse All
            </Button>
        </div> */}
        <Accordion styled exclusive={false} fluid>
            {_.values(attributeAccordionItemsSorted)}
        </Accordion>
    </>
  );
}


export default DiffAttributeList;