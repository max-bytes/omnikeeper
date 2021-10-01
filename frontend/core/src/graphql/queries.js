import gql from 'graphql-tag';
import { Fragments } from './fragments';

export const queries = {
    PredicateList: gql`
        query {
            predicates {
                ...FullPredicate
            }
        }
        ${Fragments.fullPredicate}
    `,

    AdvancedSearchFullCIs: gql`
        query($searchString: String!, $ciids: [Guid], $withEffectiveTraits: [String]!, $withoutEffectiveTraits: [String]!, $layers: [String]!) {
            advancedSearchFullCIs(searchString: $searchString, ciids: $ciids, sortByCIName: true,
                withEffectiveTraits: $withEffectiveTraits, withoutEffectiveTraits: $withoutEffectiveTraits, layers: $layers) {
                id
                name
            }
        }
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
    ActiveTrait: gql`
        query($id: String!) {
            activeTrait(id: $id) {
                id
                origin {
                    type
                    info
                }
                ancestorTraits
                requiredAttributes
                optionalAttributes
                requiredRelations
                optionalRelations
            }
        }
    `,

    FullCI: gql`
        query($ciid: Guid!, $layers: [String]!, $timeThreshold: DateTimeOffset, $includeAttributes: Boolean = true, $includeRelated: Boolean = true) {
            ci(ciid: $ciid, layers: $layers, timeThreshold: $timeThreshold) {
                ...FullCI
            }
        }
        ${Fragments.outgoingMergedRelation}
        ${Fragments.incomingMergedRelation}
        ${Fragments.fullCI}
        ${Fragments.mergedAttribute}
        ${Fragments.attribute}
    `,
    FullCIs: gql`
        query($ciids: [Guid], $layers: [String]!, $timeThreshold: DateTimeOffset, $includeAttributes: Boolean = true, $includeRelated: Boolean = true) {
            cis(ciids: $ciids, layers: $layers, timeThreshold: $timeThreshold) {
                ...FullCI
            }
        }
        ${Fragments.outgoingMergedRelation}
        ${Fragments.incomingMergedRelation}
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
            changeset(id: $id, layers: []) {
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
        query($id: Guid!, $layers:[String]!) {
            changeset(id: $id, layers: $layers) {
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
                dataOrigin {
                    type
                }

                attributes {
                    ...FullAttribute
                }
                removedAttributes {
                    ...FullAttribute
                }
                relations {
                    id
                    fromCIID
                    toCIID
                    fromCIName
                    toCIName
                    predicateID
                    changesetID
                }
                removedRelations {
                    id
                    fromCIID
                    toCIID
                    fromCIName
                    toCIName
                    predicateID
                    changesetID
                }
            }
        }
        ${Fragments.attribute}
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
    Statistics: gql`
        query {
            statistics {
                cis
                activeAttributes
                activeRelations
                attributeChanges
                relationChanges
                changesets
                layers
                predicates
                traits
                generators
            }
        }
    `,
};