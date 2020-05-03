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

  return (
    <div style={{position: 'relative', height: '100%'}}>
      <div className="left-bar">
        <div className={"layers"}>
          <h5>Layers</h5>
          <Layers />
        </div>
        <div className={"timeline"}>
          <Timeline ciid={ciid}></Timeline>
        </div>
      </div>
      <div className="center">
        <MainAreaCI ciid={ciid}></MainAreaCI>
      </div>
    </div>
  );
}

export default Explorer;