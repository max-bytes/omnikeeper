import gql from 'graphql-tag';
import { Fragments } from './fragments';

export const queries = {
    
    OIAContexts: gql`
        query {
            manage_oiacontexts {
                id
                name
                config
            }
        }
    `,
    ODataAPIContexts: gql`
        query {
            manage_odataapicontexts {
                id
                config
            }
        }
    `,
    Predicates: gql`
        query {
            manage_predicates {
                ...FullPredicate
            }
        }
        ${Fragments.fullPredicate}
    `,
    RecursiveTraits: gql`
        query {
            manage_recursiveTraits {
                ...RecursiveTrait
            }
        }
        ${Fragments.recursiveTrait}
    `,
    Layers: gql`
        query {
            manage_layers {
                ...FullLayer
            }
        }
        ${Fragments.fullLayer}
    `,
    LayerStatistics: gql`
        query($layerID: Long!) {
            manage_layerStatistics(layerID: $layerID) {
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
    
    BaseConfiguration: gql`
    query {
        manage_baseConfiguration
      }
    `,
    CacheKeys: gql`
    query {
        manage_cacheKeys
      }
    `,
    DebugCurrentUserClaims: gql`
    query {
        manage_debugCurrentUserClaims
      }
    `,
    Version: gql`
    query {
        manage_version {
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
    query {
        manage_plugins {
            name
            version
            informationalVersion
            managementEndpoint
        }
    }
    `
};