import React from 'react';
import './App.css';
import { ApolloProvider, ApolloClient, HttpLink, InMemoryCache,gql,defaultDataIdFromObject  } from '@apollo/client';
import { ApolloProvider as ApolloHooksProvider } from '@apollo/react-hooks'
import Explorer from './Explorer';
import 'bootstrap/dist/css/bootstrap.min.css';
import { queries } from './queries'


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

function App() {

  const typeDefs = gql`
    type LayerSortingAndVisibility {
      id: Int!
      sort: Int!
      visibility: Bool!
    }
    type LocalState {
      layerSortingAndVisibility: LayerSortingAndVisibility!
    }
    extend type Query {
      localState: LocalState!
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
        layers.filter(l => l.sort === newSortOrder && l.id !== variables.id).forEach(l => {
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
    }
  };

var cache = new InMemoryCache({
  dataIdFromObject: object => {
    switch (object.__typename) {
      case 'CIType': return `CIType:${object.identity}:${object.layerhash}`; 
      case 'CIAttributeType': return `CIAttributeType:${object.name}:${object.layerID}:ls${object.layerStackIDs.join(',')}`;
      default: return defaultDataIdFromObject(object);
    }
  }
});
  
const client = new ApolloClient({
  cache,
  link: new HttpLink({
      uri: 'https://localhost:44378/graphql',
    }),
  typeDefs: typeDefs,
  resolvers: resolvers
});

var initialState = {
  data: {
    localState: {
        layerSortingAndVisibility: [{
          id: 0,
          sort: 1,
          visibility: true
        }],
    },
    '__typename': 'LocalState!'
  }
};
cache.writeData(initialState);

  return (
      <ApolloProvider client={client}>
        <ApolloHooksProvider client={client}>
          <div className="App">
            <Explorer></Explorer>
          </div>
        </ApolloHooksProvider>
      </ApolloProvider>
  );
}

export default App;
