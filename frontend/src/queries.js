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
    CI: gql`
        query ci($identity: String!, $layers: [String]!) {
            ci(identity: $identity, layers: $layers) {
                ...FullCI
            }
        }
        ${Fragments.ci}
        ${Fragments.attribute}
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
  `
};