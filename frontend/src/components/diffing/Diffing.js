import React, {useState} from 'react';
import Layers from 'components/Layers';
import { mutations } from 'graphql/mutations'
import { queries } from 'graphql/queries'
import { useMutation, useQuery } from '@apollo/react-hooks';
import _ from 'lodash';
// import ExplorerLayers from 'components/ExplorerLayers';


function Diffing() {

  var [ hiddenLayers, setHiddenLayers ] = useState([]);
  var [ layerSortOffsets, setLayerSortOffsets ] = useState([]);

  return (
    <div style={{position: 'relative', height: '100%'}}>
      <div className="left-bar">
        <div className={"layers"}>
          <h5>Layers</h5>
          <Layers hiddenLayers={hiddenLayers} layerSortOffsets={layerSortOffsets} 
    onSetHiddenLayers={ newHLs => setHiddenLayers(newHLs) }
    onSetLayerSortOffsets={ newLSOs => setLayerSortOffsets(newLSOs)}/>
          {/* <ExplorerLayers /> */}
        </div>
      </div>
    </div>
  );
}

export default Diffing;



// function useCustomLayers(skipInvisible = false, skipReadonly = false) {
//   const { error, data, loading } = useQuery(queries.Layers);
//   var { data: { hiddenLayers } } = useQuery(queries.HiddenLayers);
//   var { data: { layerSortings } } = useQuery(queries.LayerSortings);

//   if (data && hiddenLayers && layerSortings) {
//       // add visibility from cache
//       let layers = _.map(data.layers, layer => {
//           if (_.includes(hiddenLayers, layer.id))
//               return {...layer, visible: false};
//           else
//               return {...layer, visible: true};
//       });

//       // add sort order changes from cache
//       var baseSortOrder = 0;
//       layers = _.map(layers, layer => {
//           var s = _.find(layerSortings, ls => ls.layerID === layer.id);
//           if (s)
//               return {...layer, sort: baseSortOrder++ + s.sortOffset };
//           else
//               return {...layer, sort: baseSortOrder++ };
//       });

//       // sort
//       layers.sort((a,b) => {
//           var o = b.sort - a.sort;
//           if (o === 0) return ('' + a.name).localeCompare('' + b.name);
//           return o;
//       });
//       if (skipInvisible)
//           layers = layers.filter(l => l.visible);
//       if (skipReadonly)
//           layers = layers.filter(l => l.writable && l.state === 'ACTIVE');
//       return {error: error, data: layers, loading: loading};
//   }
//   return {error: error, data: [], loading: loading};
// }