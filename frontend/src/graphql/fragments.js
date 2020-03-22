import gql from 'graphql-tag';

export const Fragments = {
  mergedAttribute: gql`
    fragment FullMergedAttribute on MergedCIAttributeType {
        attribute {
            ...FullAttribute
        }
        layerStackIDs
        layerStack {
            id
            name
            visibility @client
            color @client
        }
    }
  `,

  attribute: gql`
    fragment FullAttribute on CIAttributeType {
        id
        name
        changesetID
        state
        value {
            type
            value
        }
    }
  `,
  ci: gql`
    fragment FullCI on CIType {
        identity
        layerhash
        atTime
        type {
            id
        }
        related {
            relation {
                ...FullRelation
            }
            ciid
            isForward
        }
        mergedAttributes {
            ...FullMergedAttribute
        }
    }
  `,
  relation: gql`
    fragment FullRelation on RelationType {
        id
        fromCIID
        toCIID
        predicate {
            id,
            wordingFrom
            wordingTo
        }
        layerID
        layerStackIDs
        changesetID
        layerStack {
            id
            name
            visibility @client
            color @client
        }
    }
  `,
};
