import { useQuery } from '@apollo/client';
import React, { useEffect, useState } from 'react';
import Layers from './Layers';
import MainAreaCI from './MainAreaCI';
import { queries } from '../graphql/queries'
import Timeline from './Timeline';
import { useKeycloak } from '@react-keycloak/web'
import { useParams } from 'react-router-dom'

function Explorer(props) {
  // const { search } = useLocation();
  // let params = new URLSearchParams(search);
  // var ciid = params.get('ciid');
  const { ciid } = useParams();

  const [ keycloak, keycloakInitialized ] = useKeycloak()
  const { error: errorLayers, data: dataLayers } = useQuery(queries.Layers);
  const { error: errorTime, data: dataTime } = useQuery(queries.SelectedTimeThreshold);
  // const { error: errorCI, data: dataCI } = useQuery(queries.SelectedCI);
  const [ userProfile, setUserProfile ] = useState(undefined);

  useEffect(() => {
    keycloak.loadUserProfile().then(profile => {
      setUserProfile(profile);
    })
  }, [keycloak, keycloakInitialized])

  if (dataLayers && dataTime && userProfile) {
    // sort based on order
    let sortedLayers = dataLayers.layers.concat();
    sortedLayers.sort((a,b) => {
      var o = b.sort - a.sort;
      if (o === 0) return ('' + a.name).localeCompare(b.name);
      return o;
    });

    // var ciid = dataCI.selectedCI;
      
    const logoutButton = (<div style={{display: 'flex', margin: '5px'}}>
        <span style={{flexGrow: 1}}>Logged in as user {userProfile.username} </span>
        <button type="button" onClick={() => keycloak.logout()}>Logout</button>
      </div>);

    return (
      <div>
        <div className="left">
          {logoutButton}
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
      </div>
    );
  } else {
    if (errorLayers) {
      return <p>Error: {errorLayers}</p>;
    }
    else if (errorTime) return <p>Error: {errorTime}</p>;
    // else if (errorCI) return <p>Error: {errorCI}</p>;
    else return <p>Loading</p>;
    
  }
}

export default Explorer;