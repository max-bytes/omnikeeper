import React from 'react';
import PropTypes from 'prop-types'
import Attribute from './Attribute';
import { Flipper, Flipped } from 'react-flip-toolkit'
import _ from 'lodash';
import { Accordion, Button, Icon } from 'semantic-ui-react'
import { onAppear, onExit } from '../utils/animation';
import {useAttributeSegmentsToggler} from 'utils/useAttributeSegmentsToggler'

function ExplorerAttributeList(props) {

  // TODO: does not work with nested groups yet
  const nestedAttributes = _.groupBy(props.mergedAttributes, (mergedAttribute) => {
    const splits = mergedAttribute.attribute.name.split('.');
    if (splits.length <= 1) return "";
    else return splits.slice(0, -1).join(".");
  });

  const [toggleSegment, isSegmentActive, toggleExpandCollapseAll] = useAttributeSegmentsToggler(_.keys(nestedAttributes));

  const attributeAccordionItems = [];
  _.forEach(nestedAttributes, (na, key) => {
    var sortedAttributes = [...na];
    sortedAttributes.sort((a,b) => {
      return a.attribute.name.localeCompare(b.attribute.name);
    });

    const title = (key === "") ? "__base" : key;

    const ret = (<div key={key}>
      <Accordion.Title active={isSegmentActive(key)} onClick={(_, { index }) => toggleSegment(index)} index={key}>
        <Icon name='dropdown' /> {title}
      </Accordion.Title>
      <Accordion.Content active={isSegmentActive(key)}>
        <Flipper flipKey={sortedAttributes.map(a => a.layerStackIDs).join(' ')}>
          {sortedAttributes.map((a, index) => {
            var isEditable = props.isEditable && props.visibleAndWritableLayers.some(l => l.id === a.layerStackIDs[a.layerStackIDs.length - 1]);
            return (<Flipped key={a.attribute.name} flipId={a.attribute.name} onAppear={onAppear} onExit={onExit}>
              <Attribute visibleLayers={props.visibleLayers} style={{padding: '5px 0px', backgroundColor: ((index % 2 === 1) ? '#00000009' : '#00000000')}} 
                attribute={a} ciIdentity={props.ciIdentity} isEditable={isEditable} />
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