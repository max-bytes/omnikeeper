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
        ${Fragments.traitAttribute}
        ${Fragments.traitRelation}
    `,
    Generators: gql`
        query {
            manage_generators {
                ...Generator
            }
        }
        ${Fragments.generator}
    `,
    AuthRoles: gql`
        query {
            manage_authRoles {
                ...AuthRole
            }
        }
        ${Fragments.authRole}
    `,
    CLConfigs: gql`
        query {
            manage_clConfigs {
                ...CLConfig
            }
        }
        ${Fragments.clConfig}
    `,
    AvailablePermissions: gql`
        query {
            manage_availablePermissions
        }
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
        query($layerID: String!) {
            manage_layerStatistics(layerID: $layerID) {
                numActiveAttributes
                numAttributeChangesHistory
                numActiveRelations
                numRelationChangesHistory
                numLayerChangesetsHistory
                latestChange
                layer {
                    id
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
    DebugCurrentUser: gql`
    query {
        manage_debugCurrentUser
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