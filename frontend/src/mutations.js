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
        insertedAttributes {
          ...FullAttribute
        }
        affectedCIs {
          ...FullCI
        }
      }
    }
    ${Fragments.attribute}
    ${Fragments.ci}
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
        removedAttributes {
          ...FullAttribute
        }
        affectedCIs {
          ...FullCI
        }
      }
    }
    ${Fragments.attribute}
    ${Fragments.ci}
  `
};