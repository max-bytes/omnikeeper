import React from "react";
import LayerStackIcons from "components/LayerStackIcons";
import { Form, Button } from 'antd';
import OriginPopup from "components/OriginPopup";
import RelatedCIText from "./RelatedCIText";

export default function RelatedCI(props) {

  const {predicates, related, onRemove, alignRight} = props;

  return (
    <div style={{margin: "5px", float: alignRight ? "right" : "unset" }}>
      <Form layout="inline" style={{flexFlow: 'nowrap', alignItems: 'center'}}>
        <LayerStackIcons layerStack={related.layerStack}></LayerStackIcons>
        <OriginPopup changesetID={related.changesetID} />
        <Form.Item style={{flexBasis: '600px', justifyContent: 'flex-start', paddingRight: "0.25rem"}}>
          <RelatedCIText predicates={predicates} related={related} />
        </Form.Item>
        {onRemove && <Button type="danger" size="small" onClick={e => onRemove()}>Remove</Button>}
      </Form>
    </div>
  );
}
