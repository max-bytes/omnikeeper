import React from "react";
import { mutations } from '../graphql/mutations';
import { useMutation } from '@apollo/client';
import LayerStackIcons from "./LayerStackIcons";
import { Form, Button } from 'antd';
import { Link  } from 'react-router-dom'
import OriginPopup from "./OriginPopup";
import { useExplorerLayers } from '../utils/layers';

function RelatedCI(props) {

  const { data: visibleLayers } = useExplorerLayers(true);
  // TODO: loading
  const [removeRelation] = useMutation(mutations.REMOVE_RELATION, { 
    update: (cache, data) => {
      /* HACK: find a better way to deal with cache invalidation! We would like to invalidate the affected CIs, which 
      translates to multiple entries in the cache, because each CI can be cached multiple times for each layerhash
      */
      // data.data.mutate.affectedCIs.forEach(ci => {
      //   var id = props.client.cache.identify(ci);
      //   console.log("Evicting: " + id);
      //   cache.evict(id);
      // });
    }
  });
  const [setSelectedTimeThreshold] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);

  // const otherCIButton = <Button type="link" onClick={() => setSelectedCI({variables: { newSelectedCI: props.related.ci.identity }})}>{props.related.ci.identity}</Button>;
  const otherCIButton = <Link to={"/explorer/" + props.related.ci.id}>{props.related.ci.name ?? "[UNNAMED]"}</Link>;

  const written = <span>{`This CI "${props.related.predicateWording}" `}{otherCIButton}</span>;

  // move remove functionality into on-prop
  let removeButton;
  if (props.isEditable) {
    removeButton = <Button type="danger" size="small" onClick={e => {
      removeRelation({ variables: { fromCIID: props.related.fromCIID, toCIID: props.related.toCIID, includeRelated: props.perPredicateLimit,
        predicateID: props.related.predicateID, layerID: props.related.layerID, layers: visibleLayers.map(l => l.name) } })
      .then(d => setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true, refreshTimeline: true }}));
    }}>Remove</Button>;
  }

  return (
    <div style={{margin: "5px", float: props.alignRight ? "right" : "unset" }}>
      <Form layout="inline" style={{flexFlow: 'nowrap', alignItems: 'center'}} id={`value:${props.related.predicateID}`}>
        <LayerStackIcons layerStack={props.related.layerStack}></LayerStackIcons>
        <OriginPopup changesetID={props.related.changesetID} originType={props.related.origin.type} />
        <Form.Item style={{flexBasis: '600px', justifyContent: 'flex-start', paddingRight: "0.25rem"}}>{written}</Form.Item>
        {removeButton}
      </Form>
    </div>
  );
}

RelatedCI.propTypes = {
  // TODO
}

export default RelatedCI;