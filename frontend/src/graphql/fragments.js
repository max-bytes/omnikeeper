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
  fullCI: gql`
    fragment FullCI on MergedCIType {
        identity
        layerhash
        atTime
        type {
            id
        }
        templateErrors {
          attributeErrors {
                attributeName
                errors {
                    __typename
                    ... on TemplateErrorAttributeMissingType {errorMessage, type}
                    ... on TemplateErrorAttributeWrongTypeType {errorMessage, correctType}
                }
            }
        }
        related @include(if: $includeRelated) {
            ...RelatedCI
        }
        mergedAttributes @include(if: $includeAttributes) {
            ...FullMergedAttribute
        }
    }
  `,
  relatedCI: gql` 
  fragment RelatedCI on RelatedCIType {
        relation {
            ...FullRelation
        }
        ciid
        isForward
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
