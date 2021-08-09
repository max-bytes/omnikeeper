import gql from 'graphql-tag';
import { Fragments } from './fragments';

export const mutations = {

  CREATE_LAYER: gql`
  mutation($layer: CreateLayerInputType!) {
    manage_createLayer(layer: $layer) {
      ...FullLayer
    }
  }
  ${Fragments.fullLayer}
`,

  UPDATE_LAYER: gql`
  mutation($layer: UpdateLayerInputType!) {
    manage_updateLayer(layer: $layer) {
      ...FullLayer
    }
  }
  ${Fragments.fullLayer}
  `,

  CREATE_OIACONTEXT: gql`
    mutation($oiaContext: CreateOIAContextInputType!) {
      manage_createOIAContext(oiaContext: $oiaContext) {
        id
        name
        config
      }
    }
  `,
  UPDATE_OIACONTEXT: gql`
  mutation($oiaContext: UpdateOIAContextInputType!) {
    manage_updateOIAContext(oiaContext: $oiaContext) {
      id
      name
      config
    }
  }
  `,
  DELETE_OIACONTEXT: gql`
  mutation($oiaID: Long!) {
    manage_deleteOIAContext(oiaID: $oiaID)
  }
  `,
  
  TRUNCATE_LAYER: gql`
  mutation($id: Long!) {
    manage_truncateLayer(id: $id)
  }
  `,

  UPSERT_ODATAAPICONTEXT: gql`
    mutation($odataAPIContext: UpsertODataAPIContextInputType!) {
      manage_upsertODataAPIContext(odataAPIContext: $odataAPIContext) {
        id
        config
      }
    }
  `,
  DELETE_ODATAAPICONTEXT: gql`
  mutation($id: String!) {
    manage_deleteODataAPIContext(id: $id)
  }
  `,
  
  SET_BASECONFIGURATION: gql`
  mutation($baseConfiguration: String!) {
    manage_setBaseConfiguration(baseConfiguration: $baseConfiguration)
  }
  `,

  UPSERT_PREDICATE: gql`
  mutation($predicate: UpsertPredicateInputType!) {
    manage_upsertPredicate(predicate: $predicate) {
        ...FullPredicate
    }
  }
  ${Fragments.fullPredicate}
  `,

  REMOVE_PREDICATE: gql`
  mutation($predicateID: String!) {
    manage_removePredicate(predicateID: $predicateID)
  }
  `,

  UPSERT_RECURSIVE_TRAIT: gql`
  mutation($trait: UpsertRecursiveTraitInputType!) {
    manage_upsertRecursiveTrait(trait: $trait) {
        ...RecursiveTrait
    }
  }
  ${Fragments.recursiveTrait}
  `,

  REMOVE_RECURSIVE_TRAIT: gql`
  mutation($id: String!) {
    manage_removeRecursiveTrait(id: $id)
  }
  `,
};