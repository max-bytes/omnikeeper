import React from 'react'
import { Redirect } from 'react-router-dom'
import { useKeycloak } from '@react-keycloak/web'
import Page from './Page';
import { Spin } from 'antd';

export function PrivateRoute(props) {

  const { children, title, ...rest} = props;

  const { keycloak, initialized } = useKeycloak();

  if (!initialized) return <div style={{display: "flex", height: "100%"}}><Spin spinning={true} size="large" tip="Loading...">&nbsp;</Spin></div>;

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
