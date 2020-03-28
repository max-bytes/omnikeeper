import gql from 'graphql-tag';
import { Fragments } from './fragments';

export const mutations = {
    INSERT_CI_ATTRIBUTE: gql`
    mutation InsertCIAttribute($layers: [String]!, $ciIdentity: String!, $name: String!, $layerID: Long!, $value: AttributeValueGenericInputType!) {
      mutate(layers: $layers, insertAttributes: [
        {
          ci: $ciIdentity,
          name: $name,
          layerID: $layerID,
          value: $value
        }
      ]) {
        __typename
      }
    }
  `,
  REMOVE_CI_ATTRIBUTE: gql`
    mutation RemoveCIAttribute($layers: [String]!, $ciIdentity: String!, $name: String!, $layerID: Long!, $includeAttributes: Boolean = false, $includeRelated: Boolean = false) {
      mutate(layers: $layers, removeAttributes: [
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
    ${Fragments.mergedAttribute}
    ${Fragments.attribute}
    ${Fragments.relation}
    ${Fragments.fullCI}
  `,

  INSERT_RELATION: gql`
  mutation InsertRelation($layers: [String]!, $fromCIID: String!, $toCIID: String!, $predicateID: String!, $layerID: Long!) {
    mutate(layers: $layers, insertRelations: [
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
`,

REMOVE_RELATION: gql`
mutation RemoveRelation($layers: [String]!, $fromCIID: String!, $toCIID: String!, $predicateID: String!, $layerID: Long!, $includeAttributes: Boolean = false, $includeRelated: Boolean = false) {
  mutate(layers: $layers, removeRelations: [
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
${Fragments.mergedAttribute}
${Fragments.attribute}
${Fragments.relation}
${Fragments.fullCI}
`,

CREATE_CI: gql`
    mutation CreateCI($ciIdentity: String!, $typeID: String!) {
      createCIs(cis: [
        {
          identity: $ciIdentity
          typeID: $typeID
        }
      ]) {
        __typename
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
  `,
  
  // SET_SELECTED_CI: gql`
  // mutation SetSelectedCI($newSelectedCI: string) {
  //   setSelectedCI(newSelectedCI: $newSelectedCI) @client
  // }
  // `
};