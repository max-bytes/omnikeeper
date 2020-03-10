import React, { useState } from 'react';
import PropTypes from 'prop-types'
import Attribute from './Attribute';
import { Flipper, Flipped } from 'react-flip-toolkit'
import _ from 'lodash';
import { Accordion, Icon } from 'semantic-ui-react'
import { onAppear, onExit } from '../utils/animation';

function AttributeList(props) {
  // TODO: does not work with nested groups yet
  const nestedAttributes = _.groupBy(props.attributes, (attribute) => {
    const splits = attribute.name.split('.');
    if (splits.length <= 1) return "";
    else return splits.slice(0, -1).join(".");
  });
  let index = 0;
  const [openAttributeSegments, setOpenAttributeSegments] = useState(_.range(_.size(nestedAttributes)));

  // TODO: switch from index based to key based
  const attributeAccordionItems = _.map(nestedAttributes, (na, key) => {
    var sortedAttributes = [...na];
    sortedAttributes.sort((a,b) => {
      return a.name.localeCompare(b.name);
    });

    const title = (key === "") ? "__uncategorized" : key;

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
          {sortedAttributes.map(a => (
            <Flipped key={a.name} flipId={a.name} onAppear={onAppear} onExit={onExit}>
              <Attribute attribute={a} ciIdentity={props.ciIdentity} layers={props.layers} isEditable={props.isEditable}></Attribute>
            </Flipped>
          ))}
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
    layers: PropTypes.arrayOf(
        PropTypes.shape({
        id: PropTypes.number.isRequired,
        name: PropTypes.string.isRequired,
        visibility: PropTypes.bool.isRequired
        }).isRequired
    ).isRequired,
    ciIdentity: PropTypes.string.isRequired,
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
    ).isRequired
}

export default AttributeList;