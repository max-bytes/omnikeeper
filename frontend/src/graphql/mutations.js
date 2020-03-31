import gql from 'graphql-tag';
import { Fragments } from './fragments';

export const mutations = {
    INSERT_CI_ATTRIBUTE: gql`
    mutation InsertCIAttribute($ciIdentity: String!, $name: String!, $layerID: Long!, $value: AttributeValueGenericInputType!) {
      mutate(insertAttributes: [
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
    mutation RemoveCIAttribute($ciIdentity: String!, $name: String!, $layerID: Long!, $includeAttributes: Boolean = false, $includeRelated: Boolean = false) {
      mutate(removeAttributes: [
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
  mutation InsertRelation($fromCIID: String!, $toCIID: String!, $predicateID: String!, $layerID: Long!) {
    mutate(insertRelations: [
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
mutation RemoveRelation($fromCIID: String!, $toCIID: String!, $predicateID: String!, $layerID: Long!, $includeAttributes: Boolean = false, $includeRelated: Boolean = false) {
  mutate(removeRelations: [
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