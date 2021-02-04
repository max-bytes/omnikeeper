import React from 'react';
import PropTypes from 'prop-types'
import Attribute from './Attribute';
import { Flipper, Flipped } from 'react-flip-toolkit'
import _ from 'lodash';
import { Collapse, Button } from "antd";
import { onAppear, onExit } from '../utils/animation';
import {useAttributeSegmentsToggler} from 'utils/useAttributeSegmentsToggler'

const { Panel } = Collapse;

function ExplorerAttributeList(props) {

  // TODO: does not work with nested groups yet
  const nestedAttributes = _.groupBy(props.mergedAttributes, (mergedAttribute) => {
    const splits = mergedAttribute.attribute.name.split('.');
    if (splits.length <= 1) return "";
    else return splits.slice(0, -1).join(".");
  });

  const [toggleSegment, isSegmentActive, toggleExpandCollapseAll] = useAttributeSegmentsToggler(_.keys(nestedAttributes));

  const attributeAccordionItems = [];
  const defaultActiveKeys = [];
  _.forEach(nestedAttributes, (na, key) => {
    var sortedAttributes = [...na];
    sortedAttributes.sort((a,b) => {
      return a.attribute.name.localeCompare(b.attribute.name);
    });

    const title = (key === "") ? "__base" : key;

    const ret = (
        <Panel header={<div key={key} onClick={() => toggleSegment(key)}>{title}</div>} key={key}>
          <Flipper flipKey={sortedAttributes.map(a => a.layerStackIDs).join(' ')}>
            {sortedAttributes.map((a, index) => {
              var isEditable = props.isEditable && props.visibleAndWritableLayers.some(l => l.id === a.layerStackIDs[a.layerStackIDs.length - 1]);
              return (<Flipped key={a.attribute.name} flipId={a.attribute.name} onAppear={onAppear} onExit={onExit}>
                <Attribute visibleLayers={props.visibleLayers} style={{padding: '5px 0px', backgroundColor: ((index % 2 === 1) ? '#00000009' : '#00000000')}} 
                  attribute={a} ciIdentity={props.ciIdentity} isEditable={isEditable} />
              </Flipped>);
            })}
          </Flipper>
        </Panel>
    );

    attributeAccordionItems[key] = ret;
    const panelKey = (key === "") ? "0" : key; // AntDesign doesn't accept an empty string as a key for 'Panel', so it sets it as "0" instead -> Set defaultActiveKeys-entry also to "0", to make the 'defaultActiveKey'-prop work.
    if (isSegmentActive(key)) defaultActiveKeys.push(panelKey);
  });

  // sort associative array
  let attributeAccordionItemsSorted = [];
  _.forEach(Object.keys(attributeAccordionItems).sort(), (value) => {
    attributeAccordionItemsSorted[value] = attributeAccordionItems[value];
  })

  return (
    <>
       <div className={"d-flex align-items-end flex-column"} style={{ position: "absolute", right: 0, top: "-38px" }}>
            <Button size="small" onClick={() => toggleExpandCollapseAll()}> {/* TODO: Expand/Collapse doesn't work anymore */}
                Expand/Collapse All
            </Button>
        </div>
        <Collapse defaultActiveKey={defaultActiveKeys}>
            {_.values(attributeAccordionItemsSorted)}
        </Collapse>
    </>
  );
}

ExplorerAttributeList.propTypes = {
    isEditable: PropTypes.bool.isRequired,
    ciIdentity: PropTypes.string,
    mergedAttributes: PropTypes.arrayOf(
      PropTypes.shape({
        attribute: PropTypes.shape({
          name: PropTypes.string.isRequired,
          state: PropTypes.string.isRequired,
          value: PropTypes.shape({
              type: PropTypes.string.isRequired,
              isArray: PropTypes.bool.isRequired,
              values: PropTypes.arrayOf(PropTypes.string).isRequired
          })
        })
      })
    ).isRequired
}

export default ExplorerAttributeList;