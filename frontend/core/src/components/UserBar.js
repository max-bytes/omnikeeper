import React, { useEffect, useState } from 'react';
import { useKeycloak } from '@react-keycloak/web'
import { Menu, Button } from 'antd'

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
    items = <>
      <Menu.Item {...props} key="userProfile">{userProfile.firstName} {userProfile.lastName}</Menu.Item>
      <Menu.Item {...props} key="logout">
        <Button onClick={() => keycloak.logout()} >Logout</Button>
      </Menu.Item>
    </>;


  } else {
    items = <>
      <Menu.Item {...props} key="notLoggedIn">Not logged in</Menu.Item>
    </>;
  }
  
  return items;
}

export default UserBar;