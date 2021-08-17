import React from "react";
import { Form, Typography } from 'antd';
import { Link  } from 'react-router-dom'
import _ from 'lodash';
import LayerIcon from "./LayerIcon";
import OriginPopup from "./OriginPopup";
import { CIID } from "utils/uuidRenderers";

const { Text } = Typography;

export default function Relation(props) {

  const {predicates, relation, layer} = props;

  const predicate = _.find(predicates, p => p.id === relation.predicateID);
  const predicateWording = predicate ? 
    <i>{predicate.wordingFrom}</i>
  : <i style={{textDecorationStyle: 'dashed', textDecorationColor: 'red', textDecorationThickness: '1px', textDecorationLine: 'underline'}}>{relation.predicateID}</i>;

  const fromCIButton = <CIID id={relation.fromCIID} link={true} />;
  const toCIButton = <CIID id={relation.toCIID} link={true} />;

  const removed = relation.state === 'REMOVED';
  const written = <span>{fromCIButton}{` `}{predicateWording}{` `}{toCIButton}</span>;
  const wrapped = (removed) ? <Text delete>{written}</Text> : written;

  return (
    <div style={{margin: "5px"}}>
      <Form layout="inline" style={{flexFlow: 'nowrap', alignItems: 'center'}}>
        <LayerIcon layer={layer} />
        <OriginPopup changesetID={relation.changesetID} />
        <Form.Item style={{flexBasis: '800px', justifyContent: 'flex-start', paddingRight: "0.25rem"}}>{wrapped}</Form.Item>
      </Form>
    </div>
  );
}
