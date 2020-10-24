import gql from 'graphql-tag';
import { Fragments } from './fragments';

export const mutations = {
    INSERT_CI_ATTRIBUTE: gql`
    mutation InsertCIAttribute($ciIdentity: Guid!, $name: String!, $layerID: Long!, $value: AttributeValueDTOInputType!, $layers: [String]!, $includeAttributes: Boolean = true, $includeRelated: Int = 0) {
      mutateCIs(insertAttributes: [
        {
          ci: $ciIdentity,
          name: $name,
          layerID: $layerID,
          value: $value
        }
      ], layers: $layers) {
        __typename
        affectedCIs {
          ...FullCI
        }
      }
    }
    ${Fragments.relatedCI}
    ${Fragments.mergedAttribute}
    ${Fragments.attribute}
    ${Fragments.fullCI}
    ${Fragments.compactCI}
  `,
  REMOVE_CI_ATTRIBUTE: gql`
    mutation RemoveCIAttribute($ciIdentity: Guid!, $name: String!, $layerID: Long!, $layers: [String]!, $includeAttributes: Boolean = true, $includeRelated: Int = 0) {
      mutateCIs(removeAttributes: [
        {
          ci: $ciIdentity,
          name: $name,
          layerID: $layerID
        }
      ], layers: $layers) {
        affectedCIs {
          ...FullCI
        }
      }
    }
    ${Fragments.relatedCI}
    ${Fragments.mergedAttribute}
    ${Fragments.attribute}
    ${Fragments.fullCI}
    ${Fragments.compactCI}
  `,

  // HACK, TODO: includeRelated makes no sense here, but its what we have for now
  // we should think about how a mutation can return all that has been updated, but does not need to return the FullCI
  INSERT_RELATION: gql`
  mutation InsertRelation($fromCIID: Guid!, $toCIID: Guid!, $predicateID: String!, $layerID: Long!, $layers: [String]!, $includeAttributes: Boolean = true, $includeRelated: Int = 50) {
    mutateCIs(insertRelations: [
      {
        fromCIID: $fromCIID,
        toCIID: $toCIID,
        predicateID: $predicateID,
        layerID: $layerID
      }
    ], layers: $layers) {
      affectedCIs {
        ...FullCI
      }
    }
  }
  ${Fragments.relatedCI}
  ${Fragments.mergedAttribute}
  ${Fragments.attribute}
  ${Fragments.compactCI}
  ${Fragments.fullCI}
`,

REMOVE_RELATION: gql`
mutation RemoveRelation($fromCIID: Guid!, $toCIID: Guid!, $predicateID: String!, $layerID: Long!, $layers: [String]!, $includeAttributes: Boolean = true, $includeRelated: Int = 50) {
  mutateCIs(removeRelations: [
    {
      fromCIID: $fromCIID,
      toCIID: $toCIID,
      predicateID: $predicateID,
      layerID: $layerID
    }
  ], layers: $layers) {
    affectedCIs {
      ...FullCI
    }
  }
}
${Fragments.relatedCI}
${Fragments.mergedAttribute}
${Fragments.attribute}
${Fragments.compactCI}
${Fragments.fullCI}
`,

CREATE_CI: gql`
    mutation CreateCI($name: String!, $layerIDForName: Long!) {
      createCIs(cis: [
        {
          name: $name,
          layerIDForName: $layerIDForName
        }
      ]) {
        __typename
        ciids
      }
    }
  `,

  CREATE_LAYER: gql`
  mutation CreateLayer($layer: CreateLayerInputType!) {
    createLayer(layer: $layer) {
      ...FullLayer
    }
  }
  ${Fragments.fullLayer}
`,

  UPDATE_LAYER: gql`
  mutation UpdateLayer($layer: UpdateLayerInputType!) {
    updateLayer(layer: $layer) {
      ...FullLayer
    }
  }
  ${Fragments.fullLayer}
  `,

  CREATE_OIACONTEXT: gql`
    mutation CreateOIAContext($oiaContext: CreateOIAContextInputType!) {
      createOIAContext(oiaContext: $oiaContext) {
        id
        name
        config
      }
    }
  `,
  UPDATE_OIACONTEXT: gql`
  mutation UpdateOIAContext($oiaContext: UpdateOIAContextInputType!) {
    updateOIAContext(oiaContext: $oiaContext) {
      id
      name
      config
    }
  }
  `,
  DELETE_OIACONTEXT: gql`
  mutation DeleteOIAContext($oiaID: Long!) {
    deleteOIAContext(oiaID: $oiaID)
  }
  `,

  UPSERT_ODATAAPICONTEXT: gql`
    mutation UpsertODataAPIContext($odataAPIContext: UpsertODataAPIContextInputType!) {
      upsertODataAPIContext(odataAPIContext: $odataAPIContext) {
        id
        config
      }
    }
  `,
  DELETE_ODATAAPICONTEXT: gql`
  mutation DeleteODataAPIContext($id: String!) {
    deleteODataAPIContext(id: $id)
  }
  `,
  
  SET_BASECONFIGURATION: gql`
  mutation SetBaseConfiguration($baseConfiguration: String!) {
    setBaseConfiguration(baseConfiguration: $baseConfiguration)
  }
  `,

  SET_TRAITSET: gql`
  mutation SetTraitSet($traitSet: String!) {
    setTraitSet(traitSet: $traitSet)
  }
  `,

  UPSERT_PREDICATE: gql`
  mutation UpsertPredicate($predicate: UpsertPredicateInputType!) {
    upsertPredicate(predicate: $predicate) {
        ...FullPredicate
    }
  }
  ${Fragments.fullPredicate}
`,

  SET_LAYER_SETTINGS: gql`
    mutation SetLayerSettings($layerSettings: [LayerSettings]) {
      setLayerSettings(layerSettings: $layerSettings) @client
    }
  `,

  SET_SELECTED_TIME_THRESHOLD: gql`
  mutation SetSelectedTimeThreshold($newTimeThreshold: DateTimeOffset, $isLatest: Bool, $refreshTimeline: Bool = false, $refreshCI: bool = false) {
    setSelectedTimeThreshold(newTimeThreshold: $newTimeThreshold, isLatest: $isLatest, refreshTimeline: $refreshTimeline, refreshCI: $refreshCI) @client
  }
  `
};