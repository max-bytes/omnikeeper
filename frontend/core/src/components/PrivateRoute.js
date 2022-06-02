import React from 'react'
import { Redirect } from 'react-router-dom'
import { useKeycloak } from '@react-keycloak/web'
import Page from './Page';

export function PrivateRoute(props) {

  const { children, title, ...rest} = props;

  const { keycloak, initialized } = useKeycloak();

  if (!initialized) return "Loading...";

  return <Page title={title}
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
