import gql from 'graphql-tag';

export const Fragments = {
  attribute: gql`
    fragment FullAttribute on CIAttributeType {
        id
        name
        layerID
        changesetID
        layerStackIDs
        layerStack {
            id
            name
            visibility @client
            color @client
        }
        state
        value {
            __typename
            ... on AttributeValueTextType {
                value
            }
            ... on AttributeValueIntegerType {
                value
            }
        }
    }
  `,
  ci: gql`
    fragment FullCI on CIType {
        identity
        layerhash
        atTime
        related {
            relation {
                ...FullRelation
            }
            ci {
                identity
                layerhash
                atTime
            }
            isForward
        }
        attributes {
            ...FullAttribute
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
