import React, { useCallback } from 'react'

import { useKeycloak } from '@react-keycloak/web'


export default () => {
  const { keycloak } = useKeycloak()

  const authorizationHeader = () => {
    if(!keycloak) return {};
    return {
      headers: {
        "Authorization": "Bearer " + keycloak.token
      }
    };
  }

  const callApi = () => {
    fetch("https://localhost:44378/ci", authorizationHeader()).then(d => {
      return d.text();
    }).then(text => console.log(text));
  };

  keycloak.loadUserProfile().then(() => {
    console.log(keycloak.profile);
  });

  return (
    <div>
      <div>User is {!keycloak.authenticated ? 'NOT ' : ''} authenticated</div>

      {!!keycloak.authenticated && (
        <button type="button" onClick={() => keycloak.logout()}>
          Logout
        </button>
      )}

      <button type="button" onClick={callApi}>
        Call API
      </button>
    </div>
  )
}
