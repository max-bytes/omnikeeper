import React from 'react';
import EditableAttributeValue from './EditableAttributeValue';
import { Col, Row, Collapse } from 'antd';
import Text from 'antd/lib/typography/Text';
import RelatedCIText from './RelatedCIText';
import { queries } from 'graphql/queries';
import { useQuery } from '@apollo/client';

const { Panel } = Collapse;

function TraitAttribute(props) {
  const { traitAttribute } = props;
  const attributeValue = traitAttribute.mergedAttribute.attribute.value;
  return <Row>
      <Col span={6}>{traitAttribute.identifier}</Col>
      <Col style={{ flexGrow: 1 }}>
        <EditableAttributeValue isEditable={false} values={attributeValue.values} type={attributeValue.type} isArray={attributeValue.isArray} />
      </Col>
    </Row>;
}

function TraitAttributes(props) {
  const { traitAttributes } = props;
  if (traitAttributes.length <= 0)
    return <Text disabled>No trait attributes</Text>;
  return <>
    {traitAttributes.map(a => <TraitAttribute key={a.identifier} traitAttribute={a} />)}
  </>;
}

function TraitRelation(props) {
  const { traitRelation, predicates } = props;
  return <Row>
      <Col span={6}>{traitRelation.identifier}</Col>
      <Col style={{ flexGrow: 1 }}>
        {traitRelation.relatedCIs.map(related => <RelatedCIText key={related.relationID} related={related} predicates={predicates} />)}
      </Col>
    </Row>;
}

function TraitRelations(props) {
  const { traitRelations, predicates } = props;
  if (traitRelations.length <= 0)
    return <Text disabled>No trait relations</Text>;
  return <>
    {traitRelations.map(r => <TraitRelation key={r.identifier} traitRelation={r} predicates={predicates} />)}
  </>;
}

function EffectiveTraits(props) {

  const { data: dataPredicates } = useQuery(queries.PredicateList, { variables: {} });

  return <Collapse defaultActiveKey={[]} ghost>
      {props.traits.map((t, index) => {
        return <Panel header={t.underlyingTrait.id} key={index}>
          <h4 style={{margin: '0px', paddingLeft: '15px'}}>Trait Attributes:</h4>
          <div style={{paddingLeft: '30px'}}>
            <TraitAttributes traitAttributes={t.traitAttributes} />
          </div>
          <h4 style={{margin: '0px', paddingLeft: '15px'}}>Trait Relations:</h4>
          <div style={{paddingLeft: '30px'}}>
            <TraitRelations traitRelations={t.traitRelations} predicates={dataPredicates?.predicates} />
          </div>
        </Panel>;
      })}
    </Collapse>;
}

export default EffectiveTraits;