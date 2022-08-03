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

    SearchCIs: gql`
        query($searchString: String!, $ciids: [Guid], $withEffectiveTraits: [String]!, $withoutEffectiveTraits: [String]!, $layers: [String]!) {
            cis(searchString: $searchString, ciids: $ciids, sortByCIName: true,
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
                requiredAttributes { ...TraitAttribute}
                optionalAttributes { ...TraitAttribute}
                optionalRelations {...TraitRelation}
            }
        }
        ${Fragments.traitAttribute}
        ${Fragments.traitRelation}
    `,

    FullCI: gql`
        query($ciid: Guid!, $layers: [String]!, $timeThreshold: DateTimeOffset, $includeAttributes: Boolean = true, $includeRelated: Boolean = true) {
            cis(searchString: "", ciids: [$ciid], sortByCIName: false, withEffectiveTraits: [], withoutEffectiveTraits: [], layers: $layers, timeThreshold: $timeThreshold) {
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
                    dataCIID
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
                dataCIID
                ciAttributes {
                    ciid
                    ciName
                    attributes {
                        ...FullAttribute
                    }
                }
                removedCIAttributes {
                    ciid
                    ciName
                    attributes {
                        ...FullAttribute
                    }
                }
                relations {
                    id
                    fromCIID
                    toCIID
                    fromCIName
                    toCIName
                    predicateID
                    changesetID
                    mask
                }
                removedRelations {
                    id
                    fromCIID
                    toCIID
                    fromCIName
                    toCIName
                    predicateID
                    changesetID
                    mask
                }
            }
        }
        ${Fragments.attribute}
        `,
    Issues: gql`
        query {
          traitEntities(layers: ["__okissues"]) {
              m__meta__issue__issue {
                all {
                  latestChange {
                    timestamp
                  }
                  entity {
                    type
                    context
                    group
                    id
                    message
                    affectedCIs {
                      relatedCIID
                    }
                  }
                }
              }
            }
          }          
    `,
    SelectedTimeThreshold: gql`
        query {
            selectedTimeThreshold @client
          }
      `,
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