import { useQuery } from '@apollo/client';
import React from 'react';
import RelatedCI from './RelatedCI';
import {Row, Col} from 'react-bootstrap';
import AddNewRelation from './AddNewRelation';
import { Flipper, Flipped } from 'react-flip-toolkit'
import { onAppear, onExit } from '../utils/animation';
import { queries } from '../graphql/queries'
import { ErrorView } from './ErrorView';

function CIRelations(props) {

  const { loading: loadingCI, error: errorCI, data: dataCI } = useQuery(queries.FullCI, {
    variables: { identity: props.ciIdentity, layers: props.visibleLayers, timeThreshold: props.timeThreshold, includeRelated: true, includeAttributes: false }
  });

  if (dataCI) {
    var sortedRelatedCIs = [...dataCI.ci.related];
    sortedRelatedCIs.sort((a,b) => {
      const predicateCompare = a.relation.predicate.id.localeCompare(b.relation.predicate.id);
      if (predicateCompare !== 0)
        return predicateCompare;
      return a.ciid.localeCompare(b.ciid);
    });
  
    return (<>
    <Row>
      <Col>
        <AddNewRelation isEditable={props.isEditable} visibleAndWritableLayers={props.visibleAndWritableLayers} ciIdentity={props.ciIdentity}></AddNewRelation>
      </Col>
    </Row>
    <Row>
      <Col>
        <Flipper flipKey={sortedRelatedCIs.map(r => r.relation.layerStackIDs).join(' ')}>
          {sortedRelatedCIs.map(r => {
            var isLayerWritable = props.visibleAndWritableLayers.some(l => l.id === r.relation.layerID);

            return (<Flipped key={r.relation.id} flipId={r.relation.predicateID} onAppear={onAppear} onExit={onExit}>
                <RelatedCI related={r} ciIdentity={props.ciIdentity} isEditable={props.isEditable && isLayerWritable}></RelatedCI>
              </Flipped>);
          })}
        </Flipper>
      </Col>
    </Row>
    </>);
  }
  else if (loadingCI) return <p>Loading</p>;
  else if (errorCI) return <ErrorView error={errorCI}/>;
  else return <p>?</p>;
}

export default CIRelations;