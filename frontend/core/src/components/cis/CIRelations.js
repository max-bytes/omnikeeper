import { useQuery, useMutation } from '@apollo/client';
import React, { useEffect, useState, useCallback } from 'react';
import RelatedCI from './RelatedCI';
import {Row, Col} from 'antd';
import AddNewRelation from './AddNewRelation';
import { Flipper, Flipped } from 'react-flip-toolkit'
import { onAppear, onExit } from 'utils/animation';
import { queries } from 'graphql/queries'
import { mutations } from 'graphql/mutations'
import { ErrorView } from 'components/ErrorView';
import { useExplorerLayers } from 'utils/layers';

function CIRelations(props) {

  const {mergedRelations, ciIdentity, isEditable, areOutgoingRelations } = props;

  const { data: visibleAndWritableLayers } = useExplorerLayers(true, true);
  const { data: visibleLayers } = useExplorerLayers(true);

  const { loading: loadingPredicates, error: errorPredicates, data: dataPredicates } = useQuery(queries.PredicateList, { variables: {} });
  
  // TODO: loading
  const [removeRelation] = useMutation(mutations.REMOVE_RELATION, { 
    update: (cache, data) => {
      /* HACK: find a better way to deal with cache invalidation! We would like to invalidate the affected CIs, which 
      translates to multiple entries in the cache, because each CI can be cached multiple times for each layerhash
      */
      // data.data.mutate.affectedCIs.forEach(ci => {
      //   var id = props.client.cache.identify(ci);
      //   console.log("Evicting: " + id);
      //   cache.evict(id);
      // });
    }
  });
  const [setSelectedTimeThreshold] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);

  
  const sortingFunc = useCallback((a,b) => {
    const predicateCompare = a.relation.predicateID?.localeCompare(b.relation.predicateID) ?? 0;
    if (predicateCompare !== 0)
      return predicateCompare;
    const fromCIIDCompare = a.relation.fromCIID.localeCompare(b.relation.fromCIID);
    if (fromCIIDCompare !== 0)
      return fromCIIDCompare;
    return a.relation.toCIID.localeCompare(b.relation.toCIID)
  }, []);

  const [sortedRelations, setSortedRelations] = useState(null);
  useEffect(() => {
    const tmp = [...mergedRelations];
    tmp.sort(sortingFunc);
    setSortedRelations(tmp);
  }, [mergedRelations, setSortedRelations, sortingFunc]);

  if (sortedRelations && dataPredicates) {
    return (<>
    <Row>
      <Col span={24}>
        <AddNewRelation isOutgoingRelation={areOutgoingRelations} isEditable={isEditable} visibleLayers={visibleLayers.map(l => l.id)} visibleAndWritableLayers={visibleAndWritableLayers} ciIdentity={ciIdentity}></AddNewRelation>
      </Col>
    </Row>
    <Row>
      <Col span={24}>
        <Flipper flipKey={sortedRelations.map(r => r.layerStackIDs).join(' ')}>
          {sortedRelations.map(r => {
            const isLayerWritable = visibleAndWritableLayers.some(l => l.id === r.layerID);

            const onRemove = (isEditable && isLayerWritable) ? 
            (() => {
              removeRelation({ variables: { fromCIID: r.relation.fromCIID, toCIID: r.relation.toCIID,
                predicateID: r.relation.predicateID, layerID: r.layerID, layers: visibleLayers.map(l => l.id) } })
              .then(d => setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true, refreshTimeline: true, refreshCI: true }}));
            })
            : null;

            return (<Flipped key={r.relation.predicateID + "_" + r.relation.fromCIID + "_" + r.relation.toCIID} flipId={r.relation.predicateID} onAppear={onAppear} onExit={onExit}>
                <RelatedCI mergedRelation={r} predicates={dataPredicates.predicates} onRemove={onRemove} isOutgoingRelation={areOutgoingRelations} />
              </Flipped>);
          })}
        </Flipper>
      </Col>
    </Row>
    </>);
  }
  else if (loadingPredicates) return <p>Loading</p>;
  else if (errorPredicates) return <ErrorView error={errorPredicates}/>;
  else return <p>?</p>;
}

export default CIRelations;