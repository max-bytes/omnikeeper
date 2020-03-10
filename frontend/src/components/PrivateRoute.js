import React from 'react'
import { Route, Redirect } from 'react-router-dom'
import { useKeycloak } from '@react-keycloak/web'

export function PrivateRoute(props) {
  // console.log("private route");

  const { children, ...rest} = props;

  const [keycloak, initialized] = useKeycloak()

  if (!initialized) return (<div>Loading</div>);

  return (
    <Route
      {...rest}
      render={pprops => {
        return keycloak.authenticated ? 
          props.children
         : (
          <Redirect
            to={{
              pathname: '/login',
              state: { from: pprops.location }
            }}
          />
        );
        }
      }
    />
  )
}
