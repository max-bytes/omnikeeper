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
        ciid
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
            traitAttributes {
              identifier
              mergedAttribute {
                ...FullMergedAttribute
              }
            }
            traitRelations {
              identifier
              relatedCIs {
                ...RelatedCI
              }
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
  relation: gql`
    fragment Relation on RelationType {
        id
        fromCIID
        toCIID
        predicateID
        changesetID
        state
    }
  `,
  fullPredicate: gql`
  fragment FullPredicate on PredicateType {
    id,
    wordingFrom
    wordingTo,
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
    optionalRelations,
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
