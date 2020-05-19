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
    SearchCIs: gql`
        query searchCIs($searchString: String!) {
            searchCIs(searchString: $searchString) {
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
    Traits: gql`
    query traits {
        traits
      }
    `,
};