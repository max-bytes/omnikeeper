import gql from 'graphql-tag';
import { Fragments } from './fragments';

export const queries = {
    CIList: gql`
        query ciList($layers: [String]!) {
            compactCIs(layers: $layers) {
              ...CompactCI
            }
          }
        ${Fragments.compactCI}
    `,
    ValidRelationTargetCIs: gql`
        query validRelationTargetCIs($layers: [String]!, $predicateID: String!, $forward: Boolean!) {
            validRelationTargetCIs(layers: $layers, predicateID: $predicateID, forward: $forward) {
              ...CompactCI
            }
          }
        ${Fragments.compactCI}
    `,
    PredicateList: gql`
        query predicateList($stateFilter: AnchorStateFilter!) {
            predicates(stateFilter: $stateFilter) {
                ...FullPredicate
            }
        }
        ${Fragments.fullPredicate}
    `,
    DirectedPredicateList: gql`
        query predicateList($preferredForCI: Guid!, $layersForEffectiveTraits: [String]!) {
            directedPredicates(preferredForCI: $preferredForCI, layersForEffectiveTraits: $layersForEffectiveTraits) {
                ...DirectedPredicate
            }
        }
        ${Fragments.directedPredicate}
    `,
    CITypeList: gql`
        query citypes {
            citypes {
                id
                state
            }
        }
    `,

    SimpleSearchCIs: gql`
        query simpleSearchCIs($searchString: String!) {
            simpleSearchCIs(searchString: $searchString) {
                ...CompactCI
            }
        }
        ${Fragments.compactCI}
    `,
    AdvancedSearchCIs: gql`
        query advancedSearchCIs($searchString: String!, $withEffectiveTraits: [String]!, $layers: [String]!) {
            advancedSearchCIs(searchString: $searchString, withEffectiveTraits: $withEffectiveTraits, layers: $layers) {
                ...CompactCI
            }
        }
        ${Fragments.compactCI}
    `,

    FullCI: gql`
        query ci($identity: Guid!, $layers: [String]!, $timeThreshold: DateTimeOffset, $includeAttributes: Boolean = true, $includeRelated: Int = 50) {
            ci(identity: $identity, layers: $layers, timeThreshold: $timeThreshold) {
                ...FullCI
            }
        }
        ${Fragments.compactCI}
        ${Fragments.relatedCI}
        ${Fragments.fullCI}
        ${Fragments.mergedAttribute}
        ${Fragments.attribute}
    `,
    Layers: gql`
    query layers {
        layers {
            ...FullLayer
        }
    }
    ${Fragments.fullLayer}
    `,
    Changesets: gql`
        query changesets($from: DateTimeOffset!, $to:DateTimeOffset!, $ciid: Guid, $layers:[String]!, $limit: Int) {
            changesets(from: $from, to:$to, ciid:$ciid, layers: $layers, limit: $limit) {
                id
                user {
                    username
                    displayName
                    type
                }
                timestamp
            }
        }`,
    Changeset: gql`
        query changeset($id: Long!) {
            changeset(id: $id) {
                id
                timestamp
                user {
                    username
                    displayName
                    type
                }
            }
        }`,
    SelectedTimeThreshold: gql`
        query SelectedTimeThreshold {
            selectedTimeThreshold @client
          }
      `,
    LayerSettings: gql`
    query {
        layerSettings {
            layerID
            sortOffset
            visible
        }
    }`,
    Traits: gql`
    query traits {
        traits
      }
    `,
    CacheKeys: gql`
    query cacheKeys {
        cacheKeys
      }
    `,
    DebugCurrentUserClaims: gql`
    query debugCurrentUserClaims {
        debugCurrentUserClaims
      }
    `
};