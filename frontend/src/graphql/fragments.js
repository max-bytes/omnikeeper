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
        atTime {
            time
            isLatest
        }
        type {
            id
        }
        effectiveTraits { 
            underlyingTrait {name}
            attributes { 
                ...FullMergedAttribute
            }
            dependentTraits
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
        related(perPredicateLimit: $includeRelated) {
            ...RelatedCI
        }
        mergedAttributes @include(if: $includeAttributes) {
            ...FullMergedAttribute
        }
    }
  `,
  relatedCI: gql`
  fragment RelatedCI on CompactRelatedCIType {
        ci {
            ...CompactCI
        }
        fromCIID
        toCIID
        predicateID
        predicateWording
        layerID
        changesetID
        layerStackIDs
        layerStack {
            id
            name
            visibility @client
            color @client
        }
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
  // TODO: needed?
  fullRelation: gql`
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
    constraints {
        preferredTraitsTo
        preferredTraitsFrom
    },
    labelWordingFrom @client,
    labelWordingTo @client
  }
  `,
  directedPredicate: gql`
  fragment DirectedPredicate on DirectedPredicateType {
    predicateID
    wording
    predicateState
    forward
  }
  `
};
