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
    PredicateList: gql`
        query predicateList {
            predicates {
                ...FullPredicate
            }
        }
        ${Fragments.fullPredicate}
    `,

    AdvancedSearchCIs: gql`
        query advancedSearchCIs($searchString: String!, $withEffectiveTraits: [String]!, $withoutEffectiveTraits: [String]!, $layers: [String]!) {
            advancedSearchCIs(searchString: $searchString, withEffectiveTraits: $withEffectiveTraits, withoutEffectiveTraits: $withoutEffectiveTraits, layers: $layers) {
                ...CompactCI
            }
        }
        ${Fragments.compactCI}
    `,
    ActiveTraits: gql`
        query activeTraits {
            activeTraits {
                id
                origin {
                    type
                    info
                }
            }
        }
    `,
    RecursiveTraits: gql`
        query recursiveTraits {
            recursiveTraits {
                ...RecursiveTrait
            }
        }
        ${Fragments.recursiveTrait}
    `,

    FullCI: gql`
        query ci($ciid: Guid!, $layers: [String]!, $timeThreshold: DateTimeOffset, $includeAttributes: Boolean = true, $includeRelated: Int = 50) {
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
        query cis($ciids: [Guid], $layers: [String]!, $timeThreshold: DateTimeOffset, $includeAttributes: Boolean = true, $includeRelated: Int = 50) {
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
        query layers {
            layers {
                ...FullLayer
            }
        }
        ${Fragments.fullLayer}
    `,
    OIAContexts: gql`
        query oiacontexts {
            oiacontexts {
                id
                name
                config
            }
        }
    `,
    ODataAPIContexts: gql`
        query odataapicontexts {
            odataapicontexts {
                id
                config
            }
        }
    `,
    LayerStatistics: gql`
        query layerStatistics($layerID: Long!) {
            layerStatistics(layerID: $layerID) {
                numActiveAttributes
                numAttributeChangesHistory
                numActiveRelations
                numRelationChangesHistory
                numLayerChangesetsHistory
                layer {
                    name
                }
            }
        }
    `,
    Changesets: gql`
        query changesets($from: DateTimeOffset!, $to:DateTimeOffset!, $ciids: [Guid], $layers:[String]!, $limit: Int) {
            changesets(from: $from, to: $to, ciids: $ciids, layers: $layers, limit: $limit) {
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
        query changeset($id: Guid!) {
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
            layerID @client
            sortOffset @client
            visible @client
        }
    }`,
    BaseConfiguration: gql`
    query baseConfiguration {
        baseConfiguration
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
    `,
    Version: gql`
    query version {
        version {
            coreVersion
            loadedPlugins {
                name
                version
                informationalVersion
            }
        }
    }
    `,
    Plugins: gql`
    query plugins {
        plugins {
            name
            version
            informationalVersion
            managementEndpoint
        }
    }
    `
};