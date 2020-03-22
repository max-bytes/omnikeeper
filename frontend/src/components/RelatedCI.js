import React from "react";
import { withApollo } from 'react-apollo';
import { Button } from 'react-bootstrap';
import { mutations } from '../graphql/mutations';
import { useMutation } from '@apollo/react-hooks';
import LayerStackIcons from "./LayerStackIcons";
import Form from 'react-bootstrap/Form';
import { Link  } from 'react-router-dom'
import ChangesetPopup from "./ChangesetPopup";

function RelatedCI(props) {

  let visibleLayers = props.layers.filter(l => l.visibility).map(l => l.name);

  // TODO: loading
  const [removeRelation] = useMutation(mutations.REMOVE_RELATION, { 
    update: (cache, data) => {
      /* HACK: find a better way to deal with cache invalidation! We would like to invalidate the affected CIs, which 
      translates to multiple entries in the cache, because each CI can be cached multiple times for each layerhash
      */
      data.data.mutate.affectedCIs.forEach(ci => {
        var id = props.client.cache.identify(ci);
        console.log("Evicting: " + id);
        cache.evict(id);
      });
    }
  });
  const [setSelectedTimeThreshold] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);

  // const otherCIButton = <Button variant="link" onClick={() => setSelectedCI({variables: { newSelectedCI: props.related.ci.identity }})}>{props.related.ci.identity}</Button>;
  const otherCIButton = <Link to={"/explorer/" + props.related.ciid}>{props.related.ciid}</Link>;

  let written;
  if (props.related.isForward) {
    written = <span>{`This CI "${props.related.relation.predicate.wordingFrom}" `}{otherCIButton}</span>;
  } else {
    written = <span>{`This CI "${props.related.relation.predicate.wordingTo}" `}{otherCIButton}</span>;
  }

  let removeButton;
  if (props.isEditable) {
    removeButton = <Button variant="danger" onClick={e => {
      e.preventDefault();
      removeRelation({ variables: { layers: visibleLayers, fromCIID: props.related.relation.fromCIID, toCIID: props.related.relation.toCIID, predicateID: props.related.relation.predicate.id, layerID: props.related.relation.layerID } })
      .then(d => setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true }}));
    }}>Remove</Button>;
  }

  return (
    <div style={{margin: "5px"}}>
      <Form inline onSubmit={e => e.preventDefault()}>
        <LayerStackIcons layerStack={props.related.relation.layerStack}></LayerStackIcons>
        <ChangesetPopup changesetID={props.related.relation.changesetID} />
        <Form.Group controlId={`value:${props.related.relation.predicate.id}`} style={{flexGrow: 1}}>
          <Form.Label className={"pr-1"} style={{flexBasis: '400px', justifyContent: 'flex-start', whiteSpace: 'nowrap'}}>{written}</Form.Label>
          
          {removeButton}
        </Form.Group>
      </Form>
    </div>
  );
}

RelatedCI.propTypes = {
  // TODO
}

export default withApollo(RelatedCI);