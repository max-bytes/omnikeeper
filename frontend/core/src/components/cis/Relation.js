import React from "react";
import { Form, Typography } from 'antd';
import _ from 'lodash';
import LayerIcon from "components/LayerIcon";
import OriginPopup from "components/OriginPopup";
import { CIID } from "utils/uuidRenderers";
import { Link } from "react-router-dom";

const { Text } = Typography;

export default function Relation(props) {

  const {predicates, relation, layer, removed } = props;

  const isMask = relation.mask;
  const maskStyle = (isMask) ? {border: '1px dashed black', background: '#f0f0f0', opacity: '0.7'} : {};
  const maskText = (isMask) ? ` [MASK]` : ``;

  // TODO: mergable with RelatedCIText?
  const predicate = _.find(predicates, p => p.id === relation.predicateID);
  const predicateWording = predicate ? 
    <i>{predicate.wordingFrom}</i>
  : <i style={{textDecorationStyle: 'dashed', textDecorationColor: 'red', textDecorationThickness: '1px', textDecorationLine: 'underline'}}>{relation.predicateID}</i>;

  // TODO: mergable with CIID component(?)
  const fromCILink = (relation.fromCIName) ? <Link to={"/explorer/" + relation.fromCIID}>{relation.fromCIName}</Link> : <CIID id={relation.fromCIID} link={true} />;
  const toCILink = (relation.toCIName) ? <Link to={"/explorer/" + relation.toCIID}>{relation.toCIName}</Link> : <CIID id={relation.toCIID} link={true} />;

  const written = <span style={maskStyle}>{fromCILink}{` `}{predicateWording}{` `}{toCILink}{maskText}</span>;
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
