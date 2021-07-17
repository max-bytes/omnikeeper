import React from 'react';
import Attribute from 'components/Attribute';
import { Flipper, Flipped } from 'react-flip-toolkit'
import _ from 'lodash';
import { Collapse } from "antd";
import { onAppear, onExit } from 'utils/animation';
import { Row, Col } from "antd";
import { MissingLabel, CompareLabel, EmptyLabel, stateBasedBackgroundColor } from './DiffUtilComponents';
import {useAttributeSegmentsToggler} from 'utils/useAttributeSegmentsToggler'

const { Panel } = Collapse;

// TODO: consider merging with ExplorerAttributeList?
function DiffAttributeList(props) {

  // TODO: does not work with nested groups yet
  const nestedAttributes = _.groupBy(_.map(props.attributes, (leftRight, name) => ({leftRight, name})), (t) => {
    const splits = t.name.split('.');
    if (splits.length <= 1) return "__base";
    else return splits.slice(0, -1).join(".");
  });

  const [setOpenAttributeSegments, isSegmentActive /*, toggleExpandCollapseAll*/] = useAttributeSegmentsToggler(_.keys(nestedAttributes));

  if (_.size(props.attributes) === 0)
    return EmptyLabel();

  const attributeAccordionItems = [];
  const activeKeys = [];
  _.forEach(nestedAttributes, (na, key) => {

    var sortedAttributes = [...na];
    sortedAttributes.sort((a,b) => {
      return a.name.localeCompare(b.name);
    });

    const title = key;

    const ret = (
        <Panel header={title} key={key}>
          <Flipper flipKey={sortedAttributes.map(a => a.layerStackIDs).join(' ')}>
            {sortedAttributes.map((a, index) => {
                const state = a.leftRight.compareResult.state;
                return (<Flipped key={a.name} flipId={a.name} onAppear={onAppear} onExit={onExit}>
                <div style={{padding: '5px 0px', backgroundColor: ((index % 2 === 1) ? '#00000009' : '#00000000')}}>
                
                <div style={{ width: "100%" }}>
                  <Row style={{ backgroundColor: stateBasedBackgroundColor(state), display: "flex", justifyContent: "space-evenly" }}>
                    <Col xs={10} lg={3}>
                      <div style={{display: 'flex', minHeight: '38px', alignItems: 'center', justifyContent: 'flex-end'}}>
                        <span style={{whiteSpace: 'nowrap', paddingRight: "0.25rem"}}>{a.name}</span>
                      </div>
                    </Col>
                    
                    <Col xs={14} lg={9}>
                      {a.leftRight.left && <Attribute controlIdSuffix={'left'} attribute={a.leftRight.left} hideNameLabel={true} isEditable={false} />}
                      {!a.leftRight.left && <MissingLabel /> }
                    </Col>
                    
                    <Col xs={10} lg={1}>
                      <CompareLabel state={state} />
                    </Col>

                    <Col xs={14} lg={9}>
                      {a.leftRight.right && <Attribute controlIdSuffix={'right'} attribute={a.leftRight.right} hideNameLabel={true} isEditable={false} />}
                      {!a.leftRight.right && <MissingLabel /> }
                    </Col>      
                  </Row >
                </div>
              </div>
            </Flipped>);
         })}
        </Flipper>
      </Panel>
    );

    attributeAccordionItems[key] = ret;
    const panelKey = key;
    if (isSegmentActive(key)) activeKeys.push(panelKey);
  });

  // sort associative array
  let attributeAccordionItemsSorted = [];
  _.forEach(Object.keys(attributeAccordionItems).sort(), (value) => {
    attributeAccordionItemsSorted[value] = attributeAccordionItems[value];
  })

  return (
    <Collapse activeKey={activeKeys} style={{ marginTop: "10px" }} onChange={(keys) => setOpenAttributeSegments(keys)}>
        {_.values(attributeAccordionItemsSorted)}
    </Collapse>
  );
}


export default DiffAttributeList;