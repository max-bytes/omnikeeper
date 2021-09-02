import React from "react";
import { Link  } from 'react-router-dom'
import _ from 'lodash';

export default function RelatedCIText(props) {
  const {predicates, related} = props;
  const predicate = (predicates) ? _.find(predicates, p => p.id === related.predicateID) : undefined;
  const predicateWording = predicate ? 
    <i>{predicate.wordingFrom}</i>
  : <i style={{textDecorationStyle: 'dashed', textDecorationColor: 'red', textDecorationThickness: '1px', textDecorationLine: 'underline'}}>{related.predicateID}</i>;

  const otherCIButton = <Link to={"/explorer/" + related.ci.id}>{related.ci.name ?? "[UNNAMED]"}</Link>;

  const written = (related.isForwardRelation) ?
    <span>{`This CI `}{predicateWording}{` `}{otherCIButton}</span> :
    <span>{otherCIButton}{` `}{predicateWording}{` this CI`}</span>
    ;
  return written;
}
