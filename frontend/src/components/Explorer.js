import { useQuery } from '@apollo/client';
import React from 'react';
import Layers from './Layers';
import MainAreaCI from './MainAreaCI';
import { queries } from '../graphql/queries'
import Timeline from './Timeline';
import { useParams } from 'react-router-dom'
import { ErrorView } from './ErrorView';

function Explorer(props) {
  const { ciid } = useParams();

  const { error: errorLayers, data: dataLayers } = useQuery(queries.Layers);
  const { error: errorTime, data: dataTime } = useQuery(queries.SelectedTimeThreshold);

  if (dataLayers && dataTime) {
    // sort based on order
    let sortedLayers = dataLayers.layers.concat();
    sortedLayers.sort((a,b) => {
      var o = b.sort - a.sort;
      if (o === 0) return ('' + a.name).localeCompare(b.name);
      return o;
    });

    return (
      <div style={{position: 'relative', height: '100%'}}>
        <div className="left-bar">
          <div className={"layers"}>
            <h5>Layers</h5>
            <Layers layers={sortedLayers}></Layers>
          </div>
          <div className={"timeline"}>
            <Timeline layers={sortedLayers} ciid={ciid} currentTime={dataTime.selectedTimeThreshold}></Timeline>
          </div>
        </div>
        <div className="center">
          <MainAreaCI layers={sortedLayers} ciid={ciid} currentTime={dataTime.selectedTimeThreshold}></MainAreaCI>
        </div>
      </div>
    );
  } else {
    if (errorLayers) return <ErrorView error={errorLayers}/>;
    else if (errorTime) return <ErrorView error={errorTime}/>;
    else return <p>Loading</p>;
    
  }
}

export default Explorer;