import { useQuery, useMutation } from '@apollo/client';
import React from 'react';
import RelatedCI from './RelatedCI';
import {Row, Col} from 'antd';
import AddNewRelation from './AddNewRelation';
import { Flipper, Flipped } from 'react-flip-toolkit'
import { onAppear, onExit } from '../utils/animation';
import { queries } from '../graphql/queries'
import { mutations } from '../graphql/mutations'
import { ErrorView } from './ErrorView';
import { useExplorerLayers } from '../utils/layers';
import { useSelectedTime } from '../utils/useSelectedTime';
import _ from 'lodash';

function CIRelations(props) {

  const { data: visibleAndWritableLayers } = useExplorerLayers(true, true);
  const { data: visibleLayers } = useExplorerLayers(true);

  const perPredicateLimit = 100;

  const { loading: loadingCI, error: errorCI, data: dataCI, refetch: refetchCI } = useQuery(queries.FullCI, {
    variables: { ciid: props.ciIdentity, layers: visibleLayers.map(l => l.id), timeThreshold: props.timeThreshold, includeRelated: perPredicateLimit, includeAttributes: false }
  });

  const { loading: loadingPredicates, error: errorPredicates, data: dataPredicates } = useQuery(queries.PredicateList, { variables: {} });
  
  // reload when nonce changes
  const selectedTime = useSelectedTime();
  React.useEffect(() => { if (selectedTime.refreshNonceCI) refetchCI({fetchPolicy: 'network-only'}); }, [selectedTime, refetchCI]);

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


  if (dataCI && dataPredicates) {
    var sortedRelatedCIs = [...dataCI.ci.related];
    sortedRelatedCIs.sort((a,b) => {
      const predicateCompare = a.predicateID?.localeCompare(b.predicateID) ?? 0;
      if (predicateCompare !== 0)
        return predicateCompare;
      const targetCINameCompare = a.ci.name?.localeCompare(b.ci.name) ?? 0;
      if (targetCINameCompare !== 0)
        return targetCINameCompare;
      return a.ci.id.localeCompare(b.ci.id);
    });

    return (<>
    <Row>
      <Col span={24}>
        <AddNewRelation isEditable={props.isEditable} perPredicateLimit={perPredicateLimit} visibleLayers={visibleLayers.map(l => l.id)} visibleAndWritableLayers={visibleAndWritableLayers} ciIdentity={props.ciIdentity}></AddNewRelation>
      </Col>
    </Row>
    <Row>
      <Col span={24}>
        <Flipper flipKey={sortedRelatedCIs.map(r => r.layerStackIDs).join(' ')}>
          {sortedRelatedCIs.map(r => {
            const isLayerWritable = visibleAndWritableLayers.some(l => l.id === r.layerID);

            const onRemove = (props.isEditable && isLayerWritable) ? 
            (() => {
              removeRelation({ variables: { fromCIID: r.fromCIID, toCIID: r.toCIID, includeRelated: perPredicateLimit,
                predicateID: r.predicateID, layerID: r.layerID, layers: visibleLayers.map(l => l.id) } })
              .then(d => setSelectedTimeThreshold({ variables: { newTimeThreshold: null, isLatest: true, refreshTimeline: true }}));
            })
            : null;

            return (<Flipped key={r.predicateID + "_" + r.ci.id + "_" + r.isForwardRelation} flipId={r.predicateID} onAppear={onAppear} onExit={onExit}>
                <RelatedCI related={r} predicates={dataPredicates.predicates} onRemove={onRemove}></RelatedCI>
              </Flipped>);
          })}
        </Flipper>
        { _.size(sortedRelatedCIs) >= 100 && <>(Showing first {perPredicateLimit} relations per predicate")</>}
      </Col>
    </Row>
    </>);
  }
  else if (loadingCI || loadingPredicates) return <p>Loading</p>;
  else if (errorCI) return <ErrorView error={errorCI}/>;
  else if (errorPredicates) return <ErrorView error={errorPredicates}/>;
  else return <p>?</p>;
}

export default CIRelations;