import gql from 'graphql-tag';
import { Fragments } from './fragments';

export const mutations = {
    INSERT_CI_ATTRIBUTE: gql`
    mutation InsertCIAttribute($layers: [String]!, $ciIdentity: String!, $name: String!, $layerID: Long!, $value: AttributeValueGenericType!) {
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
    mutation RemoveCIAttribute($layers: [String]!, $ciIdentity: String!, $name: String!, $layerID: Long!) {
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
    ${Fragments.attribute}
    ${Fragments.relation}
    ${Fragments.ci}
  `,

  INSERT_RELATION: gql`
  mutation InsertRelation($layers: [String]!, $fromCIID: String!, $toCIID: String!, $predicate: String!, $layerID: Long!) {
    mutate(layers: $layers, insertRelations: [
      {
        fromCIID: $fromCIID,
        toCIID: $toCIID,
        predicate: $predicate,
        layerID: $layerID
      }
    ]) {
      __typename
    }
  }
`,

REMOVE_RELATION: gql`
mutation RemoveRelation($layers: [String]!, $fromCIID: String!, $toCIID: String!, $predicate: String!, $layerID: Long!) {
  mutate(layers: $layers, removeRelations: [
    {
      fromCIID: $fromCIID,
      toCIID: $toCIID,
      predicate: $predicate,
      layerID: $layerID
    }
  ]) {
    affectedCIs {
      ...FullCI
    }
  }
}
${Fragments.attribute}
${Fragments.relation}
${Fragments.ci}
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
  
  SET_SELECTED_CI: gql`
  mutation SetSelectedCI($newSelectedCI: string) {
    setSelectedCI(newSelectedCI: $newSelectedCI) @client
  }
  `
};