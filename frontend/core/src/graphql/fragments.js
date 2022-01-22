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
        value {
            type
            isArray
            values
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
            outgoingTraitRelations {
              identifier
              relations {
                ...OutgoingMergedRelation
              }
            }
            incomingTraitRelations {
              identifier
              relations {
                ...IncomingMergedRelation
              }
            }
        }
        mergedAttributes @include(if: $includeAttributes) {
            ...FullMergedAttribute
        }
        outgoingMergedRelations @include(if: $includeRelated) {
            ...OutgoingMergedRelation
        }
        incomingMergedRelations @include(if: $includeRelated) {
            ...IncomingMergedRelation
        }
    }
  `,
  fullLayer: gql`
  fragment FullLayer on LayerDataType {
    id
    description
    color
    state
    writable
    clConfigID
    onlineInboundAdapterName
    generators
  }
  `,
  outgoingMergedRelation: gql`
    fragment OutgoingMergedRelation on MergedRelationType {
        relation {
          id
          fromCIID
          toCIID
          toCIName
          predicateID
          changesetID
        }
        layerStackIDs
        layerID
        layerStack {
            id
            description
            color
        }
    }
  `,
  incomingMergedRelation: gql`
  fragment IncomingMergedRelation on MergedRelationType {
      relation {
        id
        fromCIID
        toCIID
        fromCIName
        predicateID
        changesetID
      }
      layerStackIDs
      layerID
      layerStack {
          id
          description
          color
      }
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
    optionalRelations,
    requiredTraits
  }
  `,
  generator: gql`
  fragment Generator on GeneratorType {
    id,
    attributeName,
    attributeValueTemplate
  }
  `,
  authRole: gql`
  fragment AuthRole on AuthRoleType {
    id,
    permissions
  }
  `,
  clConfig: gql`
  fragment CLConfig on CLConfigType {
    id,
    clBrainReference,
    clBrainConfig
  }
  `,
};
