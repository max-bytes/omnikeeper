import React, { useEffect, useState } from 'react';
import { useKeycloak } from '@react-keycloak/web'
import { Button } from 'antd'

function UserBar(props) {
  const { keycloak, keycloakInitialized } = useKeycloak()
  const [ userProfile, setUserProfile ] = useState(undefined);

  useEffect(() => {
    if (keycloak.authenticated)
      keycloak.loadUserProfile().then(profile => {
        setUserProfile(profile);
      })
  }, [keycloak, keycloakInitialized])

  let items;
  if (userProfile) {
    items = <div style={{ display: "flex", alignItems: "center" }}>
      <span>{userProfile.firstName} {userProfile.lastName} </span>&nbsp;
        <Button onClick={() => keycloak.logout()} >Logout</Button>
    </div>;


  } else {
    items = <div>Not logged in</div>;
  }
  
  return items;
}

export default UserBar;