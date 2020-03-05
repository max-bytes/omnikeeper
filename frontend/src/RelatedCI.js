import React from "react";
import { withApollo } from 'react-apollo';
import { Button } from 'react-bootstrap';
import { mutations } from './mutations';
import { useMutation } from '@apollo/react-hooks';
import LayerStackIcons from "./LayerStackIcons";
import Form from 'react-bootstrap/Form';

function RelatedCI(props) {

  let visibleLayers = props.layers.filter(l => l.visibility).map(l => l.name);

  // TODO: loading
  const [setSelectedCI, { loading }] = useMutation(mutations.SET_SELECTED_CI);
  const [removeRelation, { _ }] = useMutation(mutations.REMOVE_RELATION, { 
    refetchQueries: ['changesets', 'ci'], awaitRefetchQueries: true,
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
  const [setSelectedTimeThreshold, { loadingTime }] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);

  const otherCIButton = <a href="#" onClick={() => setSelectedCI({variables: { newSelectedCI: props.related.ci.identity }})}>{props.related.ci.identity}</a>;

  let written;
  if (props.related.isForward) {
    written = <span>{`This CI "${props.related.relation.predicate}`}" {otherCIButton}</span>;
  } else {
    written = <span>{otherCIButton} "{`${props.related.relation.predicate}" this CI`}</span>;
  }

  return (
    <div style={{margin: "5px"}}>
      <Form inline>
        <LayerStackIcons layerStack={props.related.relation.layerStack}></LayerStackIcons>
        <Form.Group controlId={`value:${props.related.relation.predicate}`} style={{flexGrow: 1}}>
          <Form.Label className={"pr-1"} style={{flexBasis: '400px', justifyContent: 'flex-start', whiteSpace: 'nowrap'}}>{written}</Form.Label>
          
          <Button variant="danger" onClick={e => {
            e.preventDefault();
            removeRelation({ variables: { layers: visibleLayers, fromCIID: props.related.relation.fromCIID, toCIID: props.related.relation.toCIID, predicate: props.related.relation.predicate, layerID: props.related.relation.layerID } })
            .then(d => setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true }}));
          }}>Remove</Button>
        </Form.Group>
      </Form>
    </div>
  );
}

RelatedCI.propTypes = {
  // TODO
}

export default withApollo(RelatedCI);