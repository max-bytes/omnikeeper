import React from 'react';
import { Row, Col} from 'antd';
import { Flipper, Flipped } from 'react-flip-toolkit'
import RelatedCI from 'components/RelatedCI';
import _ from 'lodash';
import { onAppear, onExit } from 'utils/animation';
import { MissingLabel, CompareLabel, EmptyLabel, stateBasedBackgroundColor } from './DiffUtilComponents';

function DiffRelationList(props) {

  if (_.size(props.relations) === 0)
    return EmptyLabel();

  return (<>
  <Row>
    <Col span={24}>
      <Flipper flipKey={_.map(props.relations, r => r.key).join(' ')}>
        {_.map(props.relations, r => {
          var state = r.compareResult.state;
          return (
            <Flipped key={r.key} flipId={r.key} onAppear={onAppear} onExit={onExit}>
              <div style={{ width: "100%" }}>
                <Row style={{ backgroundColor: stateBasedBackgroundColor(state), display: "flex", justifyContent: "space-evenly" }}>
                    <Col>
                        {r.left && <RelatedCI related={r.left} alignRight></RelatedCI>}
                        {!r.left && <MissingLabel /> }
                    </Col>
                    <Col>
                        <CompareLabel state={state} />
                    </Col>
                    <Col>
                        {r.right && <RelatedCI related={r.right}></RelatedCI>}
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