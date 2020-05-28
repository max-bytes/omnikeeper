import { useQuery } from '@apollo/client';
import React from 'react';
import RelatedCI from './RelatedCI';
import {Row, Col} from 'react-bootstrap';
import AddNewRelation from './AddNewRelation';
import { Flipper, Flipped } from 'react-flip-toolkit'
import { onAppear, onExit } from '../utils/animation';
import { queries } from '../graphql/queries'
import { ErrorView } from './ErrorView';
import { useExplorerLayers } from '../utils/layers';

function CIRelations(props) {

  const { data: visibleAndWritableLayers } = useExplorerLayers(true, true);
  const { data: visibleLayers } = useExplorerLayers(true);

  const perPredicateLimit = 100;

  const { loading: loadingCI, error: errorCI, data: dataCI } = useQuery(queries.FullCI, {
    variables: { identity: props.ciIdentity, layers: visibleLayers.map(l => l.name), timeThreshold: props.timeThreshold, includeRelated: perPredicateLimit, includeAttributes: false }
  });

  if (dataCI) {
    var sortedRelatedCIs = [...dataCI.ci.related];
    sortedRelatedCIs.sort((a,b) => {
      const predicateCompare = a.predicateID.localeCompare(b.predicateID);
      if (predicateCompare !== 0)
        return predicateCompare;
      return a.predicateWording.localeCompare(b.predicateWording);
    });

    // var sortedRelatedCIs = dataCI.ci.related;
  
    return (<>
    <Row>
      <Col>
        <AddNewRelation isEditable={props.isEditable} perPredicateLimit={perPredicateLimit} visibleLayers={visibleLayers.map(l => l.name)} visibleAndWritableLayers={visibleAndWritableLayers} ciIdentity={props.ciIdentity}></AddNewRelation>
      </Col>
    </Row>
    <Row>
      <Col>
        <Flipper flipKey={sortedRelatedCIs.map(r => r.layerStackIDs).join(' ')}>
          {sortedRelatedCIs.map(r => {
            var isLayerWritable = visibleAndWritableLayers.some(l => l.id === r.layerID);

            return (<Flipped key={r.predicateID + "_" + r.ci.id} flipId={r.predicateID} onAppear={onAppear} onExit={onExit}>
                <RelatedCI related={r} perPredicateLimit={perPredicateLimit} ciIdentity={props.ciIdentity} isEditable={props.isEditable && isLayerWritable}></RelatedCI>
              </Flipped>);
          })}
        </Flipper>
        (Showing first {perPredicateLimit} relations per predicate)
      </Col>
    </Row>
    </>);
  }
  else if (loadingCI) return <p>Loading</p>;
  else if (errorCI) return <ErrorView error={errorCI}/>;
  else return <p>?</p>;
}

export default CIRelations;