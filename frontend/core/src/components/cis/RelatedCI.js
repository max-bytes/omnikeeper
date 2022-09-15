import React from "react";
import LayerStackIcons from "components/LayerStackIcons";
import { Form, Button } from 'antd';
import OriginPopup from "components/OriginPopup";
import { RelatedCIText } from "./RelatedCIText";

export default function RelatedCI(props) {

  const {mergedRelation, onRemove, isOutgoingRelation} = props;

  return (
    <Form layout="inline" style={{flexFlow: 'nowrap', alignItems: 'center'}}>
      <LayerStackIcons layerStack={mergedRelation.layerStack}></LayerStackIcons>
      <OriginPopup changesetID={mergedRelation.relation.changesetID} />
      <Form.Item style={{flexGrow: 1, justifyContent: 'flex-start', paddingRight: "0.25rem", overflow: 'hidden',
        width: '0px' /* HACK: for whatever weird reason, this works */
        }}>
        <RelatedCIText relation={mergedRelation.relation} isOutgoingRelation={isOutgoingRelation} /> 
      </Form.Item>
      {onRemove && <Button type="danger" size="small" onClick={e => onRemove()}>Remove</Button>}
    </Form>
  );
}
