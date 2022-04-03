import React from 'react'
import { Route, Redirect } from 'react-router-dom'
import { useKeycloak } from '@react-keycloak/web'

export function PrivateRoute(props) {

  const { children, ...rest} = props;

  const { keycloak, initialized } = useKeycloak();

  if (!initialized) return "Loading...";

  return <Route
      {...rest}
      render={pprops => {
        if (keycloak.authenticated) {
            return children;
        } else {
          return <Redirect
            to={{
              pathname: '/login',
              state: { from: pprops.location }
            }}
          />;
        }}
      }
    />;
}
