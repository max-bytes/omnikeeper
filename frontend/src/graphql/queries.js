import gql from 'graphql-tag';
import { Fragments } from './fragments';

export const queries = {
    CIs: gql`
        query cis($layers: [String]!) {
            cis(layers: $layers) {
                ...FullCI
            }
        }
        ${Fragments.ci}
        ${Fragments.attribute}
    `,

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
    CI: gql`
        query ci($identity: String!, $layers: [String]!, $timeThreshold: DateTimeOffset) {
            ci(identity: $identity, layers: $layers, timeThreshold: $timeThreshold) {
                ...FullCI
            }
        }
        ${Fragments.ci}
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