import gql from 'graphql-tag';
import { Fragments } from './fragments';

export const mutations = {
  INSERT_CI_ATTRIBUTE: gql`
    mutation($ciIdentity: Guid!, $name: String!, $layerID: Long!, $value: AttributeValueDTOInputType!, $layers: [String]!, $includeAttributes: Boolean = true, $includeRelated: Int = 0) {
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
    mutation($ciIdentity: Guid!, $name: String!, $layerID: Long!, $layers: [String]!, $includeAttributes: Boolean = true, $includeRelated: Int = 0) {
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
    mutation($fromCIID: Guid!, $toCIID: Guid!, $predicateID: String!, $layerID: Long!, $layers: [String]!, $includeAttributes: Boolean = true, $includeRelated: Int = 50) {
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
  mutation($fromCIID: Guid!, $toCIID: Guid!, $predicateID: String!, $layerID: Long!, $layers: [String]!, $includeAttributes: Boolean = true, $includeRelated: Int = 50) {
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
    mutation($name: String!, $layerIDForName: Long!) {
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

  SET_LAYER_SETTINGS: gql`
    mutation($layerSettings: [LayerSettings]) {
      setLayerSettings(layerSettings: $layerSettings) @client
    }
  `,

  SET_SELECTED_TIME_THRESHOLD: gql`
  mutation($newTimeThreshold: DateTimeOffset, $isLatest: Bool, $refreshTimeline: Bool = false, $refreshCI: bool = false) {
    setSelectedTimeThreshold(newTimeThreshold: $newTimeThreshold, isLatest: $isLatest, refreshTimeline: $refreshTimeline, refreshCI: $refreshCI) @client
  }
  `
};