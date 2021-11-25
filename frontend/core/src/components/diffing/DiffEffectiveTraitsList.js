import React from 'react';
import { Row, Col} from 'antd';
import { Flipper, Flipped } from 'react-flip-toolkit'
import _ from 'lodash';
import { onAppear, onExit } from 'utils/animation';
import { MissingLabel, CompareLabel, EmptyLabel, stateBasedBackgroundColor } from './DiffUtilComponents';

function EffectiveTrait(props) {
  const {traitID} = props;
  return <div style={{minHeight: '38px', display: 'flex', justifyContent: 'center', alignItems: 'center'}}>
    {traitID}
  </div>; // TODO: show attributes/dependent traits/...
}

function DiffEffectiveTraitsList(props) {

  const {effectiveTraits} = props;

  if (_.size(effectiveTraits) === 0)
    return EmptyLabel();

  return (<>
  <Row>
      <Col span={24}>
    <Flipper flipKey={_.map(effectiveTraits, r => r.traitID).join(' ')}>
    {_.map(effectiveTraits, r => {
        var state = r.status;
        return (
        <Flipped key={r.traitID} flipId={r.traitID} onAppear={onAppear} onExit={onExit}>
            <div style={{ width: "100%" }}>
            <Row style={{ backgroundColor: stateBasedBackgroundColor(state), display: "flex", justifyContent: "space-evenly" }} >
                <Col flex="1 1 0">
                    {r.leftHasTrait && <EffectiveTrait traitID={r.traitID} />}
                    {!r.leftHasTrait && <MissingLabel /> }
                </Col>
                <Col> 
                    <CompareLabel state={state} />
                </Col>
                <Col flex="1 1 0">
                    {r.rightHasTrait && <EffectiveTrait traitID={r.traitID} />}
                    {!r.rightHasTrait && <MissingLabel /> }
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