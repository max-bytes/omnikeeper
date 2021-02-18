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
            color
        }
    }
  `,

  attribute: gql`
    fragment FullAttribute on CIAttributeType {
        id
        name
        changesetID
        state
        origin {
            type
        }
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
        layerhash
        atTime {
            time
            isLatest
        }
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
        relationID
        ci {
            ...CompactCI
        }
        fromCIID
        toCIID
        predicateID
        predicateWording
        layerID
        changesetID
        origin { type }
        layerStackIDs
        layerStack {
            id
            name
            color
        }
        isForwardRelation
    }
  `,
  fullLayer: gql`
  fragment FullLayer on LayerType {
    id
    name
    color
    state
    writable
    brainName
    onlineInboundAdapterName
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
        changesetID
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
