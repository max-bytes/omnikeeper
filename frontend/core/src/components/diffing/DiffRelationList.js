import React from 'react';
import { Row, Col} from 'antd';
import { Flipper, Flipped } from 'react-flip-toolkit'
import RelatedCI from 'components/cis/RelatedCI';
import _ from 'lodash';
import { onAppear, onExit } from 'utils/animation';
import { MissingLabel, CompareLabel, EmptyLabel, stateBasedBackgroundColor } from './DiffUtilComponents';

function DiffRelationList(props) {

  const {relations, areOutgoingRelations} = props;

  if (_.size(props.relations) === 0)
    return EmptyLabel();

  const keyGen = (relation) => `r_${relation.predicateID}_${(areOutgoingRelations) ? relation.toCIID : relation.fromCIID}`;

  return (<>
  <Row wrap={false}>
    <Col span={24}>
      <Flipper flipKey={_.map(relations, r => keyGen(r)).join(' ')}>
        {_.map(relations, r => {
          var state = r.status;
          return (
            <Flipped key={keyGen(r)} flipId={keyGen(r)} onAppear={onAppear} onExit={onExit}>
              <div style={{ width: "100%" }}>
                <Row style={{ backgroundColor: stateBasedBackgroundColor(state), display: "flex", justifyContent: "space-evenly" }}>
                    <Col flex="1 1 0">
                        {r.left && <RelatedCI mergedRelation={r.left} isOutgoingRelation={areOutgoingRelations}></RelatedCI>}
                        {!r.left && <MissingLabel /> }
                    </Col>
                    <Col>
                        <CompareLabel state={state} />
                    </Col>
                    <Col flex="1 1 0">
                        {r.right && <RelatedCI mergedRelation={r.right} isOutgoingRelation={areOutgoingRelations}></RelatedCI>}
                        {!r.right && <MissingLabel /> }
                    </Col>
                </Row>
              </div>
            </Flipped>
          );
        })}
      </Flipper>
    </Col>
  </Row>
  </>);
}

export default DiffRelationList;