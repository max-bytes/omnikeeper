import React, { useState } from 'react';
import PropTypes from 'prop-types'
import Attribute from './Attribute';
import { Flipper, Flipped } from 'react-flip-toolkit'
import _ from 'lodash';
import { Accordion, Icon } from 'semantic-ui-react'
import { onAppear, onExit } from '../utils/animation';
import { useLayers } from '../utils/useLayers';

function AttributeList(props) {
  const { data: visibleAndWritableLayers } = useLayers(true, true);

  // TODO: does not work with nested groups yet
  const nestedAttributes = _.groupBy(props.mergedAttributes, (mergedAttribute) => {
    const splits = mergedAttribute.attribute.name.split('.');
    if (splits.length <= 1) return "";
    else return splits.slice(0, -1).join(".");
  });
  let index = 0;
  const [openAttributeSegments, setOpenAttributeSegments] = useState(_.range(_.size(nestedAttributes)));

  // TODO: switch from index based to key based
  const attributeAccordionItems = _.map(nestedAttributes, (na, key) => {
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

    const active = openAttributeSegments.indexOf(index) !== -1;

    const ret = (<div key={index}><Accordion.Title active={active} onClick={onTitleClick} index={index}>
        <Icon name='dropdown' /> {title}
      </Accordion.Title>
      <Accordion.Content active={active}>
        <Flipper flipKey={sortedAttributes.map(a => a.layerStackIDs).join(' ')}>
          {sortedAttributes.map((a, index) => {
            var isLayerWritable = visibleAndWritableLayers.some(l => l.id === a.layerStackIDs[a.layerStackIDs.length - 1]);
            return (<Flipped key={a.attribute.name} flipId={a.attribute.name} onAppear={onAppear} onExit={onExit}>
              <Attribute style={{padding: '5px 0px', backgroundColor: ((index % 2 === 1) ? '#00000009' : '#00000000')}} attribute={a} ciIdentity={props.ciIdentity} isEditable={props.isEditable && isLayerWritable}></Attribute>
            </Flipped>);
          })}
        </Flipper>
      </Accordion.Content></div>);

      index++;

      return ret;
  });

  return (
    <Accordion styled exclusive={false} fluid>
        {attributeAccordionItems}
    </Accordion>
  );
}

AttributeList.propTypes = {
    isEditable: PropTypes.bool.isRequired,
    ciIdentity: PropTypes.string.isRequired,
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