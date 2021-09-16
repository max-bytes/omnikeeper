import React from "react";
import LayerStackIcons from "components/LayerStackIcons";
import { Form, Button } from 'antd';
import OriginPopup from "components/OriginPopup";
import RelatedCIText from "./RelatedCIText";

export default function RelatedCI(props) {

  const {predicates, mergedRelation, onRemove, alignRight, isOutgoingRelation} = props;

  return (
    <div style={{margin: "5px", float: alignRight ? "right" : "unset" }}>
      <Form layout="inline" style={{flexFlow: 'nowrap', alignItems: 'center'}}>
        <LayerStackIcons layerStack={mergedRelation.layerStack}></LayerStackIcons>
        <OriginPopup changesetID={mergedRelation.relation.changesetID} />
        <Form.Item style={{flexBasis: '600px', justifyContent: 'flex-start', paddingRight: "0.25rem"}}>
          <RelatedCIText predicates={predicates} relation={mergedRelation.relation} isOutgoingRelation={isOutgoingRelation} /> 
        </Form.Item>
        {onRemove && <Button type="danger" size="small" onClick={e => onRemove()}>Remove</Button>}
      </Form>
    </div>
  );
}
