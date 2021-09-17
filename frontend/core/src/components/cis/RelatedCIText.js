import React from "react";
import { Link  } from 'react-router-dom'
import _ from 'lodash';

export default function RelatedCIText(props) {
  const {predicates, relation, isOutgoingRelation} = props;
  const predicate = (predicates) ? _.find(predicates, p => p.id === relation.predicateID) : undefined;
  const predicateWording = predicate ? 
    <i>{predicate.wordingFrom}</i>
  : <i style={{textDecorationStyle: 'dashed', textDecorationColor: 'red', textDecorationThickness: '1px', textDecorationLine: 'underline'}}>{relation.predicateID}</i>;

  const otherCIID = (isOutgoingRelation) ? relation.toCIID : relation.fromCIID;
  const otherCIName = ((isOutgoingRelation) ? relation.toCIName : relation.fromCIName) ?? "[UNNAMED]";
  const otherCIButton = <Link to={"/explorer/" + otherCIID}>{otherCIName}</Link>; // TODO: merged with CIID component(?)

  const written = (isOutgoingRelation) ?
    <span>{`This CI `}{predicateWording}{` `}{otherCIButton}</span> :
    <span>{otherCIButton}{` `}{predicateWording}{` this CI`}</span>
    ;
  return written;
}
