import React, { useState } from 'react';
import PropTypes from 'prop-types'
import Attribute from './Attribute';
import { Flipper, Flipped } from 'react-flip-toolkit'
import _ from 'lodash';
import { Accordion, Button, Icon } from 'semantic-ui-react'
import { onAppear, onExit } from '../utils/animation';

function AttributeList(props) {

  // TODO: does not work with nested groups yet
  const nestedAttributes = _.groupBy(props.mergedAttributes, (mergedAttribute) => {
    const splits = mergedAttribute.attribute.name.split('.');
    if (splits.length <= 1) return "";
    else return splits.slice(0, -1).join(".");
  });

  const [openAttributeSegments, setOpenAttributeSegmentsState] = useState(localStorage.getItem('openAttributeSegments') ? JSON.parse(localStorage.getItem('openAttributeSegments')) : [] );
  const setOpenAttributeSegments = (openAttributeSegments) => {
    setOpenAttributeSegmentsState(openAttributeSegments);
    localStorage.setItem('openAttributeSegments', JSON.stringify(openAttributeSegments));
  }

  const attributeAccordionItems = [];
  _.forEach(nestedAttributes, (na, key) => {
    var sortedAttributes = [...na];
    sortedAttributes.sort((a,b) => {
      return a.attribute.name.localeCompare(b.attribute.name);
    });

    const title = (key === "") ? "__base" : key;

    const onTitleClick = (e, itemProps) => {
      const { index } = itemProps;
      if (openAttributeSegments.indexOf(index) === -1)
        setOpenAttributeSegments([...openAttributeSegments, index]);
      else
        setOpenAttributeSegments(openAttributeSegments.filter(i => i !== index));
    };

    const active = openAttributeSegments.includes(key) ? true : false;

    const ret = (<div key={key}><Accordion.Title active={active} onClick={onTitleClick} index={key}>
        <Icon name='dropdown' /> {title}
      </Accordion.Title>
      <Accordion.Content active={active}>
        <Flipper flipKey={sortedAttributes.map(a => a.layerStackIDs).join(' ')}>
          {sortedAttributes.map((a, index) => {
            var isEditable = props.isEditable && props.visibleAndWritableLayers.some(l => l.id === a.layerStackIDs[a.layerStackIDs.length - 1]);
            return (<Flipped key={a.attribute.name} flipId={a.attribute.name} onAppear={onAppear} onExit={onExit}>
              <Attribute visibleLayers={props.visibleLayers} style={{padding: '5px 0px', backgroundColor: ((index % 2 === 1) ? '#00000009' : '#00000000')}} 
                attribute={a} ciIdentity={props.ciIdentity} isEditable={isEditable} />
            </Flipped>);
          })}
        </Flipper>
      </Accordion.Content></div>);

      attributeAccordionItems[key] = ret;
  });

  // sort associative array
  let attributeAccordionItemsSorted = [];
  _.forEach(Object.keys(attributeAccordionItems).sort(), (value) => {
    attributeAccordionItemsSorted[value] = attributeAccordionItems[value];
  })

  const [expanded, setExpanded] = useState(false);
  const expandeCollapseAll = () => {
    const newOpenAttributeSegments = expanded ? [] : _.keys(attributeAccordionItemsSorted);
    setOpenAttributeSegments(newOpenAttributeSegments);
    setExpanded(!expanded);
  };

  return (
    <>
       <div className={"d-flex align-items-end flex-column mb-2"} >
            <Button
                size={"tiny"}
                onClick={() => expandeCollapseAll()}
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

AttributeList.propTypes = {
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

export default AttributeList;