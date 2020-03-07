import React from 'react'
import { Route, Redirect } from 'react-router-dom'
import { useKeycloak } from '@react-keycloak/web'

export function PrivateRoute({ component: Component, ...rest }) {
  // console.log("private route");

  const [keycloak, initialized] = useKeycloak()

  if (!initialized) return (<div>Loading</div>);

  return (
    <Route
      {...rest}
      render={props => {
        return keycloak.authenticated ? (
          <Component {...props} />
        ) : (
          <Redirect
            to={{
              pathname: '/login',
              state: { from: props.location }
            }}
          />
        );
        }
      }
    />
  )
}
