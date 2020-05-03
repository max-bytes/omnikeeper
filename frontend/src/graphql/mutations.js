import gql from 'graphql-tag';
import { Fragments } from './fragments';

export const mutations = {
    INSERT_CI_ATTRIBUTE: gql`
    mutation InsertCIAttribute($ciIdentity: Guid!, $name: String!, $layerID: Long!, $value: AttributeValueDTOInputType!, $layers: [String]!, $includeAttributes: Boolean = true, $includeRelated: Boolean = true) {
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
    ${Fragments.fullPredicate}
    ${Fragments.mergedAttribute}
    ${Fragments.attribute}
    ${Fragments.relation}
    ${Fragments.fullCI}
  `,
  REMOVE_CI_ATTRIBUTE: gql`
    mutation RemoveCIAttribute($ciIdentity: Guid!, $name: String!, $layerID: Long!, $includeAttributes: Boolean = true, $includeRelated: Boolean = true) {
      mutateCIs(removeAttributes: [
        {
          ci: $ciIdentity,
          name: $name,
          layerID: $layerID
        }
      ]) {
        affectedCIs {
          ...FullCI
        }
      }
    }
    ${Fragments.relatedCI}
    ${Fragments.fullPredicate}
    ${Fragments.mergedAttribute}
    ${Fragments.attribute}
    ${Fragments.relation}
    ${Fragments.fullCI}
  `,

  INSERT_RELATION: gql`
  mutation InsertRelation($fromCIID: Guid!, $toCIID: Guid!, $predicateID: String!, $layerID: Long!) {
    mutateCIs(insertRelations: [
      {
        fromCIID: $fromCIID,
        toCIID: $toCIID,
        predicateID: $predicateID,
        layerID: $layerID
      }
    ]) {
      __typename
    }
  }
  ${Fragments.relatedCI}
  ${Fragments.fullPredicate}
  ${Fragments.mergedAttribute}
  ${Fragments.attribute}
  ${Fragments.relation}
  ${Fragments.fullCI}
`,

REMOVE_RELATION: gql`
mutation RemoveRelation($fromCIID: Guid!, $toCIID: Guid!, $predicateID: String!, $layerID: Long!, $includeAttributes: Boolean = false, $includeRelated: Boolean = false) {
  mutateCIs(removeRelations: [
    {
      fromCIID: $fromCIID,
      toCIID: $toCIID,
      predicateID: $predicateID,
      layerID: $layerID
    }
  ]) {
    affectedCIs {
      ...FullCI
    }
  }
}
${Fragments.relatedCI}
${Fragments.fullPredicate}
${Fragments.mergedAttribute}
${Fragments.attribute}
${Fragments.relation}
${Fragments.fullCI}
`,

CREATE_CI: gql`
    mutation CreateCI($name: String!, $layerIDForName: Long!, $typeID: String) {
      createCIs(cis: [
        {
          name: $name,
          layerIDForName: $layerIDForName,
          typeID: $typeID
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

  UPSERT_PREDICATE: gql`
  mutation UpsertPredicate($predicate: UpsertPredicateInputType!) {
    upsertPredicate(predicate: $predicate) {
        ...FullPredicate
    }
  }
  ${Fragments.fullPredicate}
`,

  UPSERT_CITYPE: gql`
  mutation UpsertCIType($citype: UpsertCITypeInputType!) {
    upsertCIType(citype: $citype) {
        id
        state
    }
  }
  `,

  TOGGLE_LAYER_VISIBILITY: gql`
  mutation ToggleLayerVisibility($id: Int!) {
    toggleLayerVisibility(id: $id) @client
  }
  `,

  CHANGE_LAYER_SORT_ORDER: gql`
  mutation ChangeLayerSortOrder($id: Int!, $change: Int!) {
    changeLayerSortOrder(id: $id, change: $change) @client
  }
  `,

  SET_SELECTED_TIME_THRESHOLD: gql`
  mutation SetSelectedTimeThreshold($newTimeThreshold: DateTimeOffset, $isLatest: Bool) {
    setSelectedTimeThreshold(newTimeThreshold: $newTimeThreshold, isLatest: $isLatest) @client
  }
  `
};