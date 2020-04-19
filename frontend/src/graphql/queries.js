import gql from 'graphql-tag';
import { Fragments } from './fragments';

export const queries = {
    CIList: gql`
        query ciList {
            ciids
        }
    `,
    PredicateList: gql`
        query predicateList($stateFilter: AnchorStateFilter!) {
            predicates(stateFilter: $stateFilter) {
                ...FullPredicate
            }
        }
        ${Fragments.fullPredicate}
    `,
    CITypeList: gql`
        query citypes {
            citypes {
                id
                state
            }
        }
    `,
    FullCI: gql`
        query ci($identity: String!, $layers: [String]!, $timeThreshold: DateTimeOffset, $includeAttributes: Boolean = true, $includeRelated: Boolean = true) {
            ci(identity: $identity, layers: $layers, timeThreshold: $timeThreshold) {
                ...FullCI
            }
        }
        ${Fragments.relatedCI}
        ${Fragments.fullCI}
        ${Fragments.mergedAttribute}
        ${Fragments.attribute}
        ${Fragments.relation}
        ${Fragments.fullPredicate}
    `,
    Layers: gql`
    query layers {
        layers {
            ...FullLayer
        }
    }
    ${Fragments.fullLayer}
    `,
    Changesets: gql`
        query changesets($from: DateTimeOffset!, $to:DateTimeOffset!, $ciid: String, $layers:[String]!, $limit: Int) {
            changesets(from: $from, to:$to, ciid:$ciid, layers: $layers, limit: $limit) {
                id
                user {
                    username
                    type
                }
                timestamp
            }
        }`,
    Changeset: gql`
        query changeset($id: Long!) {
            changeset(id: $id) {
                id
                timestamp
                user {
                    username
                    type
                }
            }
        }`,
    SelectedTimeThreshold: gql`
        query SelectedTimeThreshold {
            selectedTimeThreshold @client
          }
      `
};