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
import { useSelectedTime } from '../utils/useSelectedTime';
import _ from 'lodash';

function CIRelations(props) {

  const { data: visibleAndWritableLayers } = useExplorerLayers(true, true);
  const { data: visibleLayers } = useExplorerLayers(true);

  const perPredicateLimit = 100;

  const { loading: loadingCI, error: errorCI, data: dataCI, refetch: refetchCI } = useQuery(queries.FullCI, {
    variables: { identity: props.ciIdentity, layers: visibleLayers.map(l => l.name), timeThreshold: props.timeThreshold, includeRelated: perPredicateLimit, includeAttributes: false }
  });
  
  // reload when nonce changes
  const selectedTime = useSelectedTime();
  React.useEffect(() => { if (selectedTime.refreshNonceCI) refetchCI({fetchPolicy: 'network-only'}); }, [selectedTime, refetchCI]);


  if (dataCI) {
    var sortedRelatedCIs = [...dataCI.ci.related];
    sortedRelatedCIs.sort((a,b) => {
      const predicateCompare = a.predicateID?.localeCompare(b.predicateID) ?? 0;
      if (predicateCompare !== 0)
        return predicateCompare;
      const predicateWordingCompare = a.predicateWording?.localeCompare(b.predicateWording) ?? 0;
      if (predicateWordingCompare !== 0)
        return predicateWordingCompare;
      const targetCINameCompare = a.ci.name?.localeCompare(b.ci.name) ?? 0;
      if (targetCINameCompare !== 0)
        return targetCINameCompare;
      return a.ci.id.localeCompare(b.ci.id);
    });

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

            return (<Flipped key={r.predicateID + "_" + r.ci.id + "_" + r.isForwardRelation} flipId={r.predicateID} onAppear={onAppear} onExit={onExit}>
                <RelatedCI related={r} perPredicateLimit={perPredicateLimit} isEditable={props.isEditable && isLayerWritable}></RelatedCI>
              </Flipped>);
          })}
        </Flipper>
        { _.size(sortedRelatedCIs) >= 100 && <>(Showing first {perPredicateLimit} relations per predicate")</>}
      </Col>
    </Row>
    </>);
  }
  else if (loadingCI) return <p>Loading</p>;
  else if (errorCI) return <ErrorView error={errorCI}/>;
  else return <p>?</p>;
}

export default CIRelations;