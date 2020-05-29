import React, { useState } from 'react';
import Attribute from 'components/Attribute';
import { Flipper, Flipped } from 'react-flip-toolkit'
import _ from 'lodash';
import { Accordion, Button, Icon } from 'semantic-ui-react'
import { onAppear, onExit } from 'utils/animation';
import { Container, Row, Col } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faEquals } from '@fortawesome/free-solid-svg-icons'
import { faNotEqual } from '@fortawesome/free-solid-svg-icons'
import { faWaveSquare } from '@fortawesome/free-solid-svg-icons'

function MissingAttribute() {
  return <div style={{display: 'flex', minHeight: '38px', alignItems: 'center', justifyContent: 'center'}}>
    <span style={{color: 'red', fontWeight: 'bold'}}>Missing</span>
  </div>;
}

// TODO: move
export function useAttributeSegmentsToggler(segmentNames) {
  const [openAttributeSegments, setOpenAttributeSegmentsState] = useState(localStorage.getItem('openAttributeSegments') ? JSON.parse(localStorage.getItem('openAttributeSegments')) : [] );
  const setOpenAttributeSegments = (openAttributeSegments) => {
    setOpenAttributeSegmentsState(openAttributeSegments);
    localStorage.setItem('openAttributeSegments', JSON.stringify(openAttributeSegments));
  }
  const [expanded, setExpanded] = useState(false);
  const toggleExpandCollapseAll = () => {
    const newOpenAttributeSegments = expanded ? [] : segmentNames;
    setOpenAttributeSegments(newOpenAttributeSegments);
    setExpanded(!expanded);
  };
  const toggleSegment = (index) => {
    if (openAttributeSegments.indexOf(index) === -1)
      setOpenAttributeSegments([...openAttributeSegments, index]);
    else
      setOpenAttributeSegments(openAttributeSegments.filter(i => i !== index));
  };
  const isSegmentActive = (key) => openAttributeSegments.includes(key);

  return [toggleSegment, isSegmentActive, toggleExpandCollapseAll];
}

// TODO: consider merging with ExplorerAttributeList?
function DiffAttributeList(props) {
  // TODO: does not work with nested groups yet
  const nestedAttributes = _.groupBy(_.map(props.attributes, (leftRight, name) => ({leftRight, name})), (t) => {
    const splits = t.name.split('.');
    if (splits.length <= 1) return "";
    else return splits.slice(0, -1).join(".");
  });

  const [toggleSegment, isSegmentActive, toggleExpandCollapseAll] = useAttributeSegmentsToggler(_.keys(nestedAttributes));

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

            return (<Flipped key={a.name} flipId={a.name} onAppear={onAppear} onExit={onExit}>
              <div style={{padding: '5px 0px', backgroundColor: ((index % 2 === 1) ? '#00000009' : '#00000000')}}>

                <Container fluid>
                  <Row style={{backgroundColor: (() => { switch (a.leftRight.compareResult.state) {
                          case 'equal': return '#ddffdd';
                          case 'similar': return '#ffffdd';
                          default: return '#ffdddd';
                        }})()}}>
                    <Col xs={'auto'}>
                      <div style={{display: 'flex', width: '220px', minHeight: '38px', alignItems: 'center', justifyContent: 'flex-end'}}>
                        <span className={"pr-1"} style={{whiteSpace: 'nowrap'}}>{a.name}</span>
                      </div>
                    </Col>
                    <Col>
                      {a.leftRight.left && <Attribute controlIdSuffix={'left'} attribute={a.leftRight.left} hideNameLabel={true} isEditable={false} />}
                      {!a.leftRight.left && <MissingAttribute /> }
                    </Col>
                    <Col xs={1}>
                      <div style={{display: 'flex', minHeight: '38px', alignItems: 'center', justifyContent: 'center'}}>
                        {(() => { switch (a.leftRight.compareResult.state) {
                          case 'equal': return <span style={{color: 'green', fontWeight: 'bold'}}><FontAwesomeIcon icon={faEquals} size="lg" /></span>;
                          case 'similar': return <span style={{color: 'orange', fontWeight: 'bold'}}><FontAwesomeIcon icon={faWaveSquare} size="lg" /></span>;
                          default: return <span style={{color: 'red', fontWeight: 'bold'}}><FontAwesomeIcon icon={faNotEqual} size="lg" /></span>;
                        }})()}
                      </div>
                    </Col>
                    <Col>
                      {a.leftRight.right && <Attribute controlIdSuffix={'right'} attribute={a.leftRight.right} hideNameLabel={true} isEditable={false} />}
                      {!a.leftRight.right && <MissingAttribute /> }
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
       <div className={"d-flex align-items-end flex-column mb-2"} >
            <Button
                size={"tiny"}
                onClick={() => toggleExpandCollapseAll()}
            >
                Expand/Collapse All
            </Button>
        </div>
        <Accordion styled exclusive={false} fluid>
            {_.values(attributeAccordionItemsSorted)}
        </Accordion>
    </>
  );
}


export default DiffAttributeList;