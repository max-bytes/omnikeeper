import { useQuery } from '@apollo/client';
import React from 'react';
import Layers from './Layers';
import MainAreaCI from './MainAreaCI';
import {Container} from 'react-bootstrap';
import { queries } from './queries'

function Explorer() {
  const { loading: loadingLayers, error: errorLayers, data: dataLayers } = useQuery(queries.Layers);

    if (loadingLayers) return <p>Loading...</p>;
    else if (errorLayers) return <p>Error: {errorLayers}</p>;
    else {
        // sort based on order
        let sortedLayers = dataLayers.layers.concat();
        sortedLayers.sort((a,b) => {
          var o = b.sort - a.sort;
          if (o === 0) return ('' + a.name).localeCompare(b.name);
          return o;
        });
      return (
        <Container>
            <Layers layers={sortedLayers}></Layers>
            <MainAreaCI layers={sortedLayers}></MainAreaCI>
        </Container>);
    }
}

export default Explorer;