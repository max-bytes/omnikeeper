import React from 'react';
import EditableAttributeValue from './EditableAttributeValue';
import { Col, Row, Collapse } from 'antd';
import Text from 'antd/lib/typography/Text';
import RelatedCIText from './RelatedCIText';
import { queries } from 'graphql/queries';
import { useQuery } from '@apollo/client';
import TraitID from 'utils/TraitID';

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
  const { traitRelation, predicates, isOutgoing } = props;
  return <Row>
      <Col span={6}>{traitRelation.identifier}</Col>
      <Col style={{ flexGrow: 1 }}>
        {traitRelation.relations.length <= 0 && <Text disabled>None</Text>}
        {traitRelation.relations.map(mergedRelation => <div key={mergedRelation.relation.id}>
            <RelatedCIText relation={mergedRelation.relation} predicates={predicates} isOutgoingRelation={isOutgoing} />
          </div>)}
      </Col>
    </Row>;
}

function TraitRelations(props) {
  const { traitRelations, predicates, isOutgoing } = props;
  if (traitRelations.length <= 0)
    return <Text disabled>None</Text>;
  return <>
    {traitRelations.map(r => <TraitRelation key={r.identifier} traitRelation={r} predicates={predicates} isOutgoing={isOutgoing} />)}
  </>;
}

function EffectiveTraits(props) {

  const {traits} = props;

  const sortedTraits = traits.sort((a,b) => a.underlyingTrait.id.localeCompare(b.underlyingTrait.id));

  const { data: dataPredicates } = useQuery(queries.PredicateList, { variables: {} });

  return <Collapse defaultActiveKey={[]} ghost>
      {sortedTraits.map((t, index) => {
        return <Panel header={t.underlyingTrait.id} key={index}>
          <h4 style={{margin: '0px', paddingLeft: '15px'}}>Underlying Trait:</h4>
            <div style={{paddingLeft: '30px'}}>
              <TraitID id={t.underlyingTrait.id} link={true} />
            </div>
          <h4 style={{margin: '0px', paddingLeft: '15px'}}>Trait Attributes:</h4>
          <div style={{paddingLeft: '30px'}}>
            <TraitAttributes traitAttributes={t.traitAttributes} />
          </div>
          <h4 style={{margin: '0px', paddingLeft: '15px'}}>Outgoing Trait Relations:</h4>
          <div style={{paddingLeft: '30px'}}>
            <TraitRelations traitRelations={t.outgoingTraitRelations} predicates={dataPredicates?.predicates} isOutgoing={true} />
          </div>
          <h4 style={{margin: '0px', paddingLeft: '15px'}}>Incoming Trait Relations:</h4>
          <div style={{paddingLeft: '30px'}}>
            <TraitRelations traitRelations={t.incomingTraitRelations} predicates={dataPredicates?.predicates} isOutgoing={false} />
          </div>
        </Panel>;
      })}
    </Collapse>;
}

export default EffectiveTraits;