import gql from 'graphql-tag';

export const Fragments = {
  attribute: gql`
    fragment FullAttribute on CIAttributeType {
        name
        layerID
        layer {
            id
            name
            visibility @client
            color @client
        }
        layerStackIDs
        layerstack {
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
        id
        layerhash
        related(layers: $layers) {
            relation {
                predicate
            }
            ci {
                identity
            }
        }
        attributes {
            ...FullAttribute
        }
    }
  `
};
