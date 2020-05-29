import React from 'react';
import {Container, Row, Col} from 'react-bootstrap';
import { Flipper, Flipped } from 'react-flip-toolkit'
import RelatedCI from 'components/RelatedCI';
import _ from 'lodash';
import { onAppear, onExit } from 'utils/animation';
import { MissingLabel, CompareLabel, EmptyLabel } from './DiffUtilComponents';

function DiffRelationList(props) {

  if (_.size(props.relations) === 0)
    return EmptyLabel();

  return (<>
  <Row>
    <Col>
      <Flipper flipKey={_.map(props.relations, r => r.key).join(' ')}>
        {_.map(props.relations, r => {
          var state = r.compareResult.state;
          return (<Flipped key={r.key} flipId={r.key} onAppear={onAppear} onExit={onExit}>

                <Container fluid>
                  <Row style={{backgroundColor: (() => { switch (state) {
                          case 'equal': return '#ddffdd';
                          case 'similar': return '#ffffdd';
                          default: return '#ffdddd';
                        }})()}}>
                    <Col xs={'auto'}>
                      <div style={{width: '220px', minHeight: '38px'}}>
                        &nbsp;
                      </div>
                    </Col>
                    <Col>
                      {r.left && <RelatedCI related={r.left} isEditable={false}></RelatedCI>}
                      {!r.left && <MissingLabel /> }
                    </Col>
                    <Col xs={1}>
                      <CompareLabel state={state} />
                    </Col>
                    <Col>
                      {r.right && <RelatedCI related={r.right} isEditable={false}></RelatedCI>}
                      {!r.right && <MissingLabel /> }
                    </Col>
                  </Row>
                </Container>

              
            </Flipped>);
        })}
      </Flipper>
    </Col>
  </Row>
  </>);
}

export default DiffRelationList;