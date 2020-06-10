import React from 'react';
import { queries } from '../graphql/queries'
import { ApolloProvider, ApolloClient, createHttpLink, InMemoryCache,gql,defaultDataIdFromObject  } from '@apollo/client';
import { ApolloProvider as ApolloHooksProvider } from '@apollo/react-hooks'
import { setContext } from "apollo-link-context";
import moment from 'moment'
import env from "@beam-australia/react-env";

// let toHSL = function(string, opts) {
//   var h, s, l;
//   opts = opts || {};
//   opts.hue = opts.hue || [0, 360];
//   opts.sat = opts.sat || [60, 75];
//   opts.lit = opts.lit || [60, 80];

//   var range = function(hash, min, max) {
//       var diff = max - min;
//       var x = ((hash % diff) + diff) % diff;
//       return x + min;
//   }

//   var hash = 0;
//   if (string.length === 0) return hash;
//   for (var i = string.length - 1; i >= 0; i--) {
//       hash = string.charCodeAt(i) + ((hash << 10) - hash);
//       hash = hash & hash;
//   }

//   h = range(hash, opts.hue[0], opts.hue[1]);
//   s = range(hash, opts.sat[0], opts.sat[1]);
//   l = range(hash, opts.lit[0], opts.lit[1]);

//   return `hsl(${h}, ${s}%, ${l}%)`;
// }

function ApolloWrapper({ component: Component, ...rest }) {

    const typeDefs = gql`
        type LayerSettings {
            layerID: Int!
            sortOffset: Int!
            visible: Bool!
        }
        type SelectedTimeThreshold {
            time: DateTimeOffset!
            isLatest: Bool!
        }
        extend type Query {
            selectedTimeThreshold: SelectedTimeThreshold! @client
            layerSettings: [LayerSettings] @client
        }
    `;
    
    const resolvers = {
        Mutation: {
            setSelectedTimeThreshold: (_root, variables, { cache, getCacheKey }) => {
                cache.writeQuery({query: queries.SelectedTimeThreshold, data: {
                    selectedTimeThreshold: {
                        time: variables.newTimeThreshold,
                        isLatest: variables.isLatest,
                        refreshNonceTimeline: (variables.refreshTimeline) ? moment().format('YYYY-MM-DD HH:mm:ss') : null,
                        refreshNonceCI: (variables.refreshCI) ? moment().format('YYYY-MM-DD HH:mm:ss') : null
                    }
                }});
                return null;
            },
            setLayerSettings: (_root, variables, { cache, getCacheKey }) => {
                cache.writeQuery({ query: queries.LayerSettings, data: { layerSettings: variables.layerSettings } });
                return null;
            },
        },
        PredicateType: {
            labelWordingFrom: (obj, args, context, info) => {
                var stateStr = "";
                if (obj.state !== 'ACTIVE') stateStr = " (DEPRECATED)";
                return obj.wordingFrom + stateStr;
            },
            labelWordingTo: (obj, args, context, info) => {
                var stateStr = "";
                if (obj.state !== 'ACTIVE') stateStr = " (DEPRECATED)";
                return obj.wordingTo + stateStr;
            }
        }
    };

    var cache = new InMemoryCache({
        typePolicies: {
            // RelationType: {
            //     keyFields: false
            // },
            // RelatedCIType: {
            //     keyFields: false
            // }
        },
        dataIdFromObject: object => {
            switch (object.__typename) {
            case 'MergedCIType': return `MergedCIType:${object.id}:${object.layerhash}:${((object.atTime.isLatest) ? 'latest' : object.atTime.time)}`; 
            case 'CompactCIType': return `CompactCIType:${object.id}:${object.layerhash}:${((object.atTime.isLatest) ? 'latest' : object.atTime.time)}`; 
            case 'MergedCIAttributeType': return `MergedCIAttributeType:${object.attribute.id}:ls${object.layerStackIDs.join(',')}`;
            case 'CIAttributeType': return `CIAttributeType:${object.id}}`;
            case 'RelationType': return `RelationType:${object.id}:ls${object.layerStackIDs.join(',')}`;
            case 'RelatedCIType': return `RelatedCIType:${object.relationID}:${object.fromCIID}:${object.toCIID}:ls${object.relation.layerStackIDs.join(',')}`;
            default: return defaultDataIdFromObject(object);
            }
        }
    });

    const authLink = setContext((_, { headers }) => {
        // get the authentication token from local storage if it exists
        const token = localStorage.getItem('token');
        // return the headers to the context so httpLink can read them
        return {
          headers: {
            ...headers,
            ...(token ? {authorization: `Bearer ${token}`} : {}),
            // authorization: token ? `Bearer ${token}` : "",
          }
        }
      });

    const httpLink = createHttpLink({
      uri: env('BACKEND_URL'),
      credentials: 'include'
    });

    const client = new ApolloClient({
      cache,
      link: authLink.concat(httpLink),
      typeDefs: typeDefs,
      resolvers: resolvers,
    //   defaultOptions: {
    //     watchQuery: {
    //       fetchPolicy: 'no-cache',
    //     },
    //   },
    });

    var initialState = {
        selectedTimeThreshold: {
          time: null,
          isLatest: true
        },
        layerSettings: null
    };
    console.log("Writing initial state")
    cache.writeQuery({
        query: gql`
        query InitialState {
            selectedTimeThreshold {
                time
                isLatest
            }
            layerSettings {
                LayerSettings {
                    layerID
                    sortOffset
                    visible
                }
            }
        }
        `,
        data: initialState
    })

    return (
        <ApolloProvider client={client}>
            <ApolloHooksProvider client={client}>
                <Component {...rest} />
            </ApolloHooksProvider>
        </ApolloProvider>
    );
}

export default ApolloWrapper;