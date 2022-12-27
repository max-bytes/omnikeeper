import gql from 'graphql-tag';
import { Fragments } from './fragments';

export const mutations = {

  UPSERT_LAYERDATA: gql`
  mutation($layer: UpsertLayerInputDataType!) {
    manage_upsertLayerData(layer: $layer) {
      ...FullLayer
    }
  }
  ${Fragments.fullLayer}
  `,

  CREATE_LAYER: gql`
  mutation($id: String!) {
    manage_createLayer(id: $id) {
      ...FullLayer
    }
  }
  ${Fragments.fullLayer}
  `,
  
  TRUNCATE_LAYER: gql`
  mutation($id: String!) {
    manage_truncateLayer(id: $id)
  }
  `,

  UPSERT_ODATAAPICONTEXT: gql`
  mutation ($id: String!, $odataAPIContext: TE_Upsert_Input___meta__config__odata_context!) {
    upsertSingleByFilter_m__meta__config__odata_context(
      layers: ["__okconfig"]
      writeLayer: "__okconfig"
      input: $odataAPIContext
      filter: {id: {exact: $id}}
    ) {
      entity {
        id
        config
      }
    }
  }
  `,
  DELETE_ODATAAPICONTEXT: gql`
  mutation($id: String!) {
    deleteSingleByFilter_m__meta__config__odata_context(
      layers: ["__okconfig"]
      writeLayer: "__okconfig"
      filter: {id: {exact: $id}}
    )
  }
  `,
  
  SET_BASECONFIGURATION: gql`
  mutation($baseConfiguration: String!) {
    manage_setBaseConfiguration(baseConfiguration: $baseConfiguration)
  }
  `,

  UPSERT_RECURSIVE_TRAIT: gql`
  mutation($trait: UpsertRecursiveTraitInputType!) {
    manage_upsertRecursiveTrait(trait: $trait) {
        ...RecursiveTrait
    }
  }
  ${Fragments.recursiveTrait}
  ${Fragments.traitAttribute}
  ${Fragments.traitRelation}
  `,
  REMOVE_RECURSIVE_TRAIT: gql`
  mutation($id: String!) {
    manage_removeRecursiveTrait(id: $id)
  }
  `,

  UPSERT_GENERATOR: gql`
  mutation($generator: UpsertGeneratorInputType!) {
    manage_upsertGenerator(generator: $generator) {
        ...Generator
    }
  }
  ${Fragments.generator}
  `,
  REMOVE_GENERATOR: gql`
  mutation($id: String!) {
    manage_removeGenerator(id: $id)
  }
  `,

  UPSERT_AUTH_ROLE: gql`
  mutation($authRole: UpsertAuthRoleInputType!) {
    manage_upsertAuthRole(authRole: $authRole) {
        ...AuthRole
    }
  }
  ${Fragments.authRole}
  `,

  REMOVE_AUTH_ROLE: gql`
  mutation($id: String!) {
    manage_removeAuthRole(id: $id)
  }
  `,
  
  UPSERT_CL_CONFIG: gql`
  mutation($config: UpsertCLConfigInputType!) {
    manage_upsertCLConfig(config: $config) {
        ...CLConfig
    }
  }
  ${Fragments.clConfig}
  `,

  REMOVE_CL_CONFIG: gql`
  mutation($id: String!) {
    manage_removeCLConfig(id: $id)
  }
  `,

  UPSERT_VALIDATOR_CONTEXT: gql`
  mutation($context: UpsertValidatorContextInputType!) {
    manage_upsertValidatorContext(context: $context) {
        ...ValidatorContext
    }
  }
  ${Fragments.validatorContext}
  `,

  REMOVE_VALIDATOR_CONTEXT: gql`
  mutation($id: String!) {
    manage_removeValidatorContext(id: $id)
  }
  `,
};