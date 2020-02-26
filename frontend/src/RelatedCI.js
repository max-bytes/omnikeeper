import React from "react";
import { withApollo } from 'react-apollo';

function RelatedCI(props) {
  return (
    <div style={{margin: "5px"}}>
      -> {props.related.relation.predicate} -> {props.related.ci.identity}
    </div>
  );
}

RelatedCI.propTypes = {
  // TODO
}

export default withApollo(RelatedCI);