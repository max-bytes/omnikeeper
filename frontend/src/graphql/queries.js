import gql from 'graphql-tag';
import { Fragments } from './fragments';

export const queries = {
    CIList: gql`
        query ciList {
            ciids
        }
    `,
    PredicateList: gql`
        query predicateList {
            predicates {
                id
                wordingFrom
                wordingTo
            }
        }
    `,
    CITypeList: gql`
        query citypes {
            citypes {
                id
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
    `,
    Layers: gql`
    {
      layers {
        id
        name
        sort @client
        visibility @client
        color @client
      }
    }
    `,
    Changesets: gql`
        query changesets($from: DateTimeOffset!, $to:DateTimeOffset!, $ciid: String, $layers:[String]!) {
            changesets(from: $from, to:$to, ciid:$ciid, layers: $layers) {
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