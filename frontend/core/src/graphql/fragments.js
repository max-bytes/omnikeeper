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
            description
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
            underlyingTrait {id}
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
        layerID
        changesetID
        origin { type }
        layerStackIDs
        layerStack {
            id
            description
            color
        }
        isForwardRelation
    }
  `,
  fullLayer: gql`
  fragment FullLayer on LayerType {
    id
    description
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
    constraints {
        preferredTraitsTo
        preferredTraitsFrom
    },
    labelWordingFrom @client,
    labelWordingTo @client
  }
  `,
  recursiveTrait: gql`
  fragment RecursiveTrait on RecursiveTraitType {
    id,
    requiredAttributes,
    optionalAttributes,
    requiredRelations,
    requiredTraits
  }
  `,
  authRole: gql`
  fragment AuthRole on AuthRoleType {
    id,
    permissions
  }
  `,
};
