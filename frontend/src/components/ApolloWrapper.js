import React from 'react';
import { queries } from '../graphql/queries'
import { ApolloProvider, ApolloClient, createHttpLink, InMemoryCache,gql,defaultDataIdFromObject  } from '@apollo/client';
import { ApolloProvider as ApolloHooksProvider } from '@apollo/react-hooks'
import { useKeycloak } from '@react-keycloak/web'
import moment from 'moment'
import env from "@beam-australia/react-env";

let toHSL = function(string, opts) {
  var h, s, l;
  opts = opts || {};
  opts.hue = opts.hue || [0, 360];
  opts.sat = opts.sat || [60, 75];
  opts.lit = opts.lit || [60, 80];

  var range = function(hash, min, max) {
      var diff = max - min;
      var x = ((hash % diff) + diff) % diff;
      return x + min;
  }

  var hash = 0;
  if (string.length === 0) return hash;
  for (var i = string.length - 1; i >= 0; i--) {
      hash = string.charCodeAt(i) + ((hash << 10) - hash);
      hash = hash & hash;
  }

  h = range(hash, opts.hue[0], opts.hue[1]);
  s = range(hash, opts.sat[0], opts.sat[1]);
  l = range(hash, opts.lit[0], opts.lit[1]);

  return `hsl(${h}, ${s}%, ${l}%)`;
}

function ApolloWrapper({ component: Component, ...rest }) {

    const [ keycloak, keycloakInitialized ] = useKeycloak()

    if (!keycloakInitialized)
        return <div>Loading</div>;

    const typeDefs = gql`
        type LayerSortingAndVisibility {
            id: Int!
            sort: Int!
            visibility: Bool!
        }
        type LocalState {
            layerSortingAndVisibility: LayerSortingAndVisibility!
        }
        type SelectedTimeThreshold {
            time: DateTimeOffset!
            isLatest: Bool!
        }
        extend type Query {
            localState: LocalState!
            selectedTimeThreshold: SelectedTimeThreshold! 
        }
        extend type Mutation {
            updateLayerSortingAndVisibility(layers: [LayerSortingAndVisibility]!): [LayerSortingAndVisibility]!
        }
        extend type LayerType {
            visibility: Bool!
            color: String!
        }
    `;
    var initalLayerSort = 0;

    const resolvers = {
        Mutation: {
            setSelectedTimeThreshold: (_root, variables, { cache, getCacheKey }) => {
                cache.writeQuery({query: queries.SelectedTimeThreshold, data: {
                    selectedTimeThreshold: { 
                        time: variables.newTimeThreshold || moment().add(1, 'year').format('YYYY-MM-DD HH:mm:ss'),
                        isLatest: variables.isLatest
                    }
                }});
                return null;
            },
            // setSelectedCI: (_root, variables, { cache, getCacheKey }) => {
            //     cache.writeQuery({query: queries.SelectedCI, data: {selectedCI: variables.newSelectedCI } });
            //     return null;
            // },
            toggleLayerVisibility: (_root, variables, { cache, getCacheKey }) => {
                const id = getCacheKey({ __typename: 'LayerType', id: variables.id })
                const fragment = gql`
                fragment visibleLayer on LayerType {
                    visibility
                }
                `;
                const layer = cache.readFragment({ fragment, id });
                const data = { ...layer, visibility: !layer.visibility }; 
                cache.writeData({ id, data });
                return null;
            },
            changeLayerSortOrder: (_root, variables, { cache, getCacheKey }) => {
                const id = getCacheKey({ __typename: 'LayerType', id: variables.id })
                const fragment = gql`
                fragment layer on LayerType {
                    sort
                }
                `;
                const layer = cache.readFragment({ fragment, id });

                var sortOrderChange = variables.change;
                var newSortOrder = layer.sort + sortOrderChange;

                var { layers } = cache.readQuery({query: queries.Layers});
                var pushedLayers = layers.filter(l => l.sort === newSortOrder && l.id !== variables.id);

                if (pushedLayers.length === 0)
                return null;

                pushedLayers.forEach(l => {
                const d = { ...l, sort: l.sort - sortOrderChange }; 
                var cacheKey = getCacheKey({ __typename: 'LayerType', id: l.id });
                // console.log("Moving layer " + l.id + " (" + cacheKey +  ") from sort " + l.sort + " to sort " + d.sort);
                cache.writeData({ id: cacheKey, data: d });
                // console.log({ id: cacheKey, data: d });
                });

                // console.log("Moving layer " + variables.id + " (" + id + ") from sort " + layer.sort + " to sort " + (layer.sort + sortOrderChange));
                const data = { ...layer, sort: layer.sort + sortOrderChange }; 
                cache.writeData({ id, data });
                // console.log({ id, data });
                return null;
            },
        },
        LayerType: {
            visibility: (obj, args, context, info) => {
                return true;
            },
            sort: (obj, args, context, info) => {
                return initalLayerSort++;
            },
            color: (obj, args, context, info) => {
                var layerName = obj.name;
                var layerHue = toHSL(layerName);
                return layerHue; // TODO: use cie lab instead
            }
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
            case 'MergedCIType': return `MergedCIType:${object.id}:${object.layerhash}:${object.atTime}`; 
            case 'MergedCIAttributeType': return `MergedCIAttributeType:${object.attribute.id}:ls${object.layerStackIDs.join(',')}`;
            case 'CIAttributeType': return `CIAttributeType:${object.id}}`;
            case 'RelationType': return `RelationType:${object.id}:ls${object.layerStackIDs.join(',')}`;
            case 'RelatedCIType': return `RelatedCIType:${object.relation.id}:f-${object.isForward}:ls${object.relation.layerStackIDs.join(',')}`;
            default: return defaultDataIdFromObject(object);
            }
        }
    });

    const httpLink = createHttpLink({
      uri: env('BACKEND_URL'),
      credentials: 'include',
      headers: {
        "Authorization": "Bearer " + ((keycloak.token) ? keycloak.token : "")
      }
    });

    const client = new ApolloClient({
      cache,
      link: httpLink,
      typeDefs: typeDefs,
      resolvers: resolvers,
    //   defaultOptions: {
    //     watchQuery: {
    //       fetchPolicy: 'no-cache',
    //     },
    //   },
    });

    var initialState = {
      data: {
        selectedTimeThreshold: {
          time: null,
          isLatest: true
        },
        '__typename': 'LocalState!'
      }
    };
    cache.writeData(initialState);

    return (
        <ApolloProvider client={client}>
            <ApolloHooksProvider client={client}>
                <Component {...rest} />
            </ApolloHooksProvider>
        </ApolloProvider>
    );
}

export default ApolloWrapper;