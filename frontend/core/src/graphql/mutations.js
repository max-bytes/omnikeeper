import gql from 'graphql-tag';
import { Fragments } from './fragments';

export const mutations = {
  INSERT_CI_ATTRIBUTE: gql`
    mutation($ciIdentity: Guid!, $name: String!, $layerID: String!, $value: AttributeValueDTOInputType!, $layers: [String]!, $includeAttributes: Boolean = true, $includeRelated: Boolean = true) {
      mutateCIs(insertAttributes: [
        {
          ci: $ciIdentity,
          name: $name,
          value: $value
        }
      ], writeLayer: $layerID, readLayers: $layers) {
        __typename
        affectedCIs {
          ...FullCI
        }
      }
    }
    ${Fragments.outgoingMergedRelation}
    ${Fragments.incomingMergedRelation}
    ${Fragments.mergedAttribute}
    ${Fragments.attribute}
    ${Fragments.fullCI}
  `,
  REMOVE_CI_ATTRIBUTE: gql`
    mutation($ciIdentity: Guid!, $name: String!, $layerID: String!, $layers: [String]!, $includeAttributes: Boolean = true, $includeRelated: Boolean = true) {
      mutateCIs(removeAttributes: [
        {
          ci: $ciIdentity,
          name: $name
        }
      ], writeLayer: $layerID, readLayers: $layers) {
        affectedCIs {
          ...FullCI
        }
      }
    }
    ${Fragments.outgoingMergedRelation}
    ${Fragments.incomingMergedRelation}
    ${Fragments.mergedAttribute}
    ${Fragments.attribute}
    ${Fragments.fullCI}
  `,

  // TODO: we should think about how a mutation can return all that has been updated, but does not need to return the FullCI
  INSERT_RELATION: gql`
    mutation($fromCIID: Guid!, $toCIID: Guid!, $predicateID: String!, $layerID: String!, $layers: [String]!, $includeAttributes: Boolean = true, $includeRelated: Boolean = true) {
      mutateCIs(insertRelations: [
        {
          fromCIID: $fromCIID,
          toCIID: $toCIID,
          predicateID: $predicateID
        }
      ], writeLayer: $layerID, readLayers: $layers) {
        affectedCIs {
          ...FullCI
        }
      }
    }
    ${Fragments.outgoingMergedRelation}
    ${Fragments.incomingMergedRelation}
    ${Fragments.mergedAttribute}
    ${Fragments.attribute}
    ${Fragments.fullCI}
  `,

  REMOVE_RELATION: gql`
  mutation($fromCIID: Guid!, $toCIID: Guid!, $predicateID: String!, $layerID: String!, $layers: [String]!, $includeAttributes: Boolean = true, $includeRelated: Boolean = true) {
    mutateCIs(removeRelations: [
      {
        fromCIID: $fromCIID,
        toCIID: $toCIID,
        predicateID: $predicateID
      }
    ], writeLayer: $layerID, readLayers: $layers) {
      affectedCIs {
        ...FullCI
      }
    }
  }
  ${Fragments.outgoingMergedRelation}
  ${Fragments.incomingMergedRelation}
  ${Fragments.mergedAttribute}
  ${Fragments.attribute}
  ${Fragments.fullCI}
  `,

  CREATE_CI: gql`
    mutation($name: String!, $layerIDForName: String!) {
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