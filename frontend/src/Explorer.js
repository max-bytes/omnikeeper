import { useQuery } from '@apollo/client';
import React from 'react';
import Layers from './Layers';
import MainAreaCI from './MainAreaCI';
import { queries } from './queries'
import Timeline from './Timeline';

function Explorer() {
  const { error: errorLayers, data: dataLayers } = useQuery(queries.Layers);
  const { error: errorTime, data: dataTime } = useQuery(queries.SelectedTimeThreshold);
  const { error: errorCI, data: dataCI } = useQuery(queries.SelectedCI);

    if (dataLayers && dataTime && dataCI) {
      // sort based on order
      let sortedLayers = dataLayers.layers.concat();
      sortedLayers.sort((a,b) => {
        var o = b.sort - a.sort;
        if (o === 0) return ('' + a.name).localeCompare(b.name);
        return o;
      });

      var ciid = dataCI.selectedCI;

    return (
      <div>
        <div className="left">
          <div className={"layers"}>
            <h5>Layers</h5>
            <Layers layers={sortedLayers}></Layers>
          </div>
          <div className={"timeline"}>
            <h5>Timeline</h5>
            <Timeline layers={sortedLayers} ciid={ciid} currentTime={dataTime.selectedTimeThreshold}></Timeline>
          </div>
        </div>
        <div className="center">
          <MainAreaCI layers={sortedLayers} ciid={ciid} currentTime={dataTime.selectedTimeThreshold}></MainAreaCI>
        </div>
      </div>);
    } else {
      if (errorLayers) return <p>Error: {errorLayers}</p>;
      else if (errorTime) return <p>Error: {errorTime}</p>;
      else if (errorCI) return <p>Error: {errorCI}</p>;
      else return <p>?</p>;
      
    }
}

export default Explorer;