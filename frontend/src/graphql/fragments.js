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
            isArray
            values
        }
    }
  `,
  compactCI: gql`
    fragment CompactCI on CompactCIType {
        id
        name
        type {id}
    }
  `,
  fullCI: gql`
    fragment FullCI on MergedCIType {
        id
        name
        layerhash
        atTime
        type {
            id
        }
        effectiveTraits { 
            underlyingTrait {name}
            attributes { 
                ...FullMergedAttribute
            }
        }
        templateErrors {
          attributeErrors {
                attributeName
                errors {
                    __typename
                    ... on TemplateErrorAttributeMissingType {errorMessage, type}
                    ... on TemplateErrorAttributeWrongTypeType {errorMessage, correctTypes}
                    ... on TemplateErrorAttributeWrongMultiplicityType {errorMessage, correctIsArray}
                    ... on TemplateErrorAttributeGenericType {errorMessage}
                }
            }
            relationErrors {
                predicateID
                errors {
                    __typename
                    ... on TemplateErrorRelationGenericType {errorMessage}
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
        ciName
    }
  `,
  fullLayer: gql`
  fragment FullLayer on LayerType {
    id
    name
    state
    writable
    brainName
    sort @client
    visibility @client
    color @client
  }
  `,
  relation: gql`
    fragment FullRelation on RelationType {
        id
        fromCIID
        toCIID
        predicate {
            ...FullPredicate
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
  fullPredicate: gql`
  fragment FullPredicate on PredicateType {
    id,
    wordingFrom
    wordingTo,
    state,
    labelWordingFrom @client,
    labelWordingTo @client
  }
  `
};
