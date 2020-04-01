import React, { useEffect, useState } from 'react';
import { useKeycloak } from '@react-keycloak/web'
import { Menu, Button } from 'semantic-ui-react'

function UserBar(props) {
  const [ keycloak, keycloakInitialized ] = useKeycloak()
  const [ userProfile, setUserProfile ] = useState(undefined);

  useEffect(() => {
    if (keycloak.authenticated)
      keycloak.loadUserProfile().then(profile => {
        setUserProfile(profile);
      })
  }, [keycloak, keycloakInitialized])

  let items;
  if (userProfile) {

    items = <div style={{display: 'flex'}}>
      <Menu.Item>Logged in as user {userProfile.username} </Menu.Item>
      <Menu.Item>
        <Button onClick={() => keycloak.logout()}>Logout</Button>
      </Menu.Item>
    </div>;


  } else {
    items = <div>
    {/* <Menu.Item>Not logged in</Menu.Item> */}
  </div>;
  }
  
  return items;
}

export default UserBar;