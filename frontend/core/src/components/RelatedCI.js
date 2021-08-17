import React from "react";
import LayerStackIcons from "./LayerStackIcons";
import { Form, Button } from 'antd';
import { Link  } from 'react-router-dom'
import OriginPopup from "./OriginPopup";
import _ from 'lodash';

export default function RelatedCI(props) {

  const {predicates, related, onRemove, alignRight} = props;

  const predicate = _.find(predicates, p => p.id === related.predicateID);
  const predicateWording = predicate ? 
    <i>{predicate.wordingFrom}</i>
  : <i style={{textDecorationStyle: 'dashed', textDecorationColor: 'red', textDecorationThickness: '1px', textDecorationLine: 'underline'}}>{related.predicateID}</i>;

  const otherCIButton = <Link to={"/explorer/" + related.ci.id}>{related.ci.name ?? "[UNNAMED]"}</Link>;

  const written = (related.isForwardRelation) ?
    <span>{`This CI `}{predicateWording}{` `}{otherCIButton}</span> :
    <span>{otherCIButton}{` `}{predicateWording}{` this CI`}</span>
    ;

  return (
    <div style={{margin: "5px", float: alignRight ? "right" : "unset" }}>
      <Form layout="inline" style={{flexFlow: 'nowrap', alignItems: 'center'}}>
        <LayerStackIcons layerStack={related.layerStack}></LayerStackIcons>
        <OriginPopup changesetID={related.changesetID} />
        <Form.Item style={{flexBasis: '600px', justifyContent: 'flex-start', paddingRight: "0.25rem"}}>{written}</Form.Item>
        {onRemove && <Button type="danger" size="small" onClick={e => onRemove()}>Remove</Button>}
      </Form>
    </div>
  );
}
