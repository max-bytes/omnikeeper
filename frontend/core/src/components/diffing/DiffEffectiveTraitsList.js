import React from 'react';
import { Row, Col} from 'antd';
import { Flipper, Flipped } from 'react-flip-toolkit'
import _ from 'lodash';
import { onAppear, onExit } from 'utils/animation';
import { MissingLabel, CompareLabel, EmptyLabel, stateBasedBackgroundColor } from './DiffUtilComponents';

function EffectiveTrait(props) {
  return <div style={{minHeight: '38px', display: 'flex', justifyContent: 'center', alignItems: 'center'}}>
    {props.effectiveTrait?.underlyingTrait.id}
  </div>; // TODO: show attributes/dependent traits/...
}

function DiffEffectiveTraitsList(props) {

  if (_.size(props.effectiveTraits) === 0)
    return EmptyLabel();

  return (<>
  <Row>
      <Col span={24}>
    <Flipper flipKey={_.map(props.effectiveTraits, r => r.key).join(' ')}>
    {_.map(props.effectiveTraits, r => {
        var state = r.compareResult.state;
        return (
        <Flipped key={r.key} flipId={r.key} onAppear={onAppear} onExit={onExit}>
            <div style={{ width: "100%" }}>
            <Row style={{ backgroundColor: stateBasedBackgroundColor(state), display: "flex", justifyContent: "space-evenly" }} >
                <Col>
                    {r.left && <EffectiveTrait effectiveTrait={r.left} />}
                    {!r.left && <MissingLabel /> }
                </Col>
                <Col>
                    <CompareLabel state={state} />
                </Col>
                <Col>
                    {r.right && <EffectiveTrait effectiveTrait={r.left} />}
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

export default DiffEffectiveTraitsList;