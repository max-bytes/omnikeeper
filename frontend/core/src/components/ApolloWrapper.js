import React from 'react';
import { queries } from '../graphql/queries'
import { ApolloProvider, ApolloClient, createHttpLink, InMemoryCache,gql,defaultDataIdFromObject  } from '@apollo/client';
import { ApolloProvider as ApolloHooksProvider } from '@apollo/client'
import { setContext } from "@apollo/client/link/context";
import moment from 'moment'
import env from "@beam-australia/react-env";

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
        }
    `;
    
    const resolvers = {
        Mutation: {
            setSelectedTimeThreshold: (_root, variables, { cache, getCacheKey }) => {
                cache.writeQuery({query: queries.SelectedTimeThreshold, data: {
                    selectedTimeThreshold: {
                        time: variables.newTimeThreshold,
                        isLatest: variables.isLatest,
                        refreshNonceTimeline: (variables.refreshTimeline) ? moment().format() : null,
                        refreshNonceCI: (variables.refreshCI) ? moment().format() : null
                    }
                }});
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
        addTypename: false,
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
            case 'MergedCIAttributeType': return `MergedCIAttributeType:${object.attribute.id}:ls${object.layerStackIDs.join(',')}`;
            case 'CIAttributeType': return `CIAttributeType:${object.id}`;
            case 'RelationType': return `RelationType:${object.id}`;
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
      uri: env('BACKEND_URL') + "/graphql",
      credentials: 'include'
    });

    function setInitialState() {
        var initialState = {
            selectedTimeThreshold: {
            time: null,
            isLatest: true
            }
        };
        // console.log("Writing initial state")
        cache.writeQuery({
            query: gql`
            query InitialState {
                selectedTimeThreshold {
                    time
                    isLatest
                }
            }
            `,
            data: initialState
        })
    }

    const client = new ApolloClient({
      cache,
      link: authLink.concat(httpLink),
      typeDefs: typeDefs,
      resolvers: resolvers,
      defaultOptions: {
        watchQuery: {
          fetchPolicy: 'no-cache',
        //   nextFetchPolicy: 'cache-first',
        },
        query: {
          fetchPolicy: 'no-cache',
        //   nextFetchPolicy: 'cache-first',
        },
      },
    });

    setInitialState();
    client.onResetStore(setInitialState);

    return (
        <ApolloProvider client={client}>
            <ApolloHooksProvider client={client}>
                <Component {...rest} />
            </ApolloHooksProvider>
        </ApolloProvider>
    );
}

export default ApolloWrapper;