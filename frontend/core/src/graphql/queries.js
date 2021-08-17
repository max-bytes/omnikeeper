import gql from 'graphql-tag';
import { Fragments } from './fragments';

export const queries = {
    CIList: gql`
        query($layers: [String]!) {
            compactCIs(layers: $layers) {
              ...CompactCI
            }
          }
        ${Fragments.compactCI}
    `,
    PredicateList: gql`
        query {
            predicates {
                ...FullPredicate
            }
        }
        ${Fragments.fullPredicate}
    `,

    AdvancedSearchCIs: gql`
        query($searchString: String!, $withEffectiveTraits: [String]!, $withoutEffectiveTraits: [String]!, $layers: [String]!) {
            advancedSearchCIs(searchString: $searchString, withEffectiveTraits: $withEffectiveTraits, withoutEffectiveTraits: $withoutEffectiveTraits, layers: $layers) {
                ...CompactCI
            }
        }
        ${Fragments.compactCI}
    `,
    ActiveTraits: gql`
        query {
            activeTraits {
                id
                origin {
                    type
                    info
                }
            }
        }
    `,

    FullCI: gql`
        query($ciid: Guid!, $layers: [String]!, $timeThreshold: DateTimeOffset, $includeAttributes: Boolean = true, $includeRelated: Int = 50) {
            ci(ciid: $ciid, layers: $layers, timeThreshold: $timeThreshold) {
                ...FullCI
            }
        }
        ${Fragments.compactCI}
        ${Fragments.relatedCI}
        ${Fragments.fullCI}
        ${Fragments.mergedAttribute}
        ${Fragments.attribute}
    `,
    FullCIs: gql`
        query($ciids: [Guid], $layers: [String]!, $timeThreshold: DateTimeOffset, $includeAttributes: Boolean = true, $includeRelated: Int = 50) {
            cis(ciids: $ciids, layers: $layers, timeThreshold: $timeThreshold) {
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
        query {
            layers {
                ...FullLayer
            }
        }
        ${Fragments.fullLayer}
    `,
    ChangesetsForCI: gql`
        query($from: DateTimeOffset!, $to:DateTimeOffset!, $layers:[String]!, $ciids: [Guid], $limit: Int) {
            changesets(from: $from, to: $to, layers: $layers, ciids: $ciids, limit: $limit) {
                id
                user {
                    username
                    displayName
                    type
                }
                layer {
                    id
                    color
                }
                timestamp
            }
        }`,
    Changesets: gql`
            query($from: DateTimeOffset!, $to:DateTimeOffset!, $layers:[String]!) {
                changesets(from: $from, to: $to, layers: $layers) {
                    id
                    user {
                        username
                        displayName
                        type
                    }
                    layer {
                        id
                        color
                    }
                    timestamp
                    statistics {
                        numAttributeChanges
                        numRelationChanges
                    }
                }
            }`,
    BasicChangeset: gql`
        query($id: Guid!) {
            changeset(id: $id) {
                id
                timestamp
                user {
                    username
                    displayName
                    type
                }
                dataOrigin {
                    type
                }
            }
        }`,
    FullChangeset: gql`
        query($id: Guid!) {
            changeset(id: $id) {
                id
                timestamp
                user {
                    username
                    displayName
                    type
                }
                layer {
                    id
                    color
                }
                attributes {
                    ...FullAttribute
                }
                relations {
                    ...Relation
                }
            }
        }
        ${Fragments.attribute}
        ${Fragments.relation}
        `,
    SelectedTimeThreshold: gql`
        query {
            selectedTimeThreshold @client
          }
      `,
    LayerSettings: gql`
    query {
        layerSettings {
            layerID @client
            sortOffset @client
            visible @client
        }
    }`,
};