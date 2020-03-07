import React, { useCallback } from 'react'
import { Redirect, withRouter } from 'react-router-dom'

import { withKeycloak } from '@react-keycloak/web'

const LoginPage = withRouter(

  withKeycloak(({ keycloak, location }) => {
    const { from } = location.state || { from: { pathname: '/ex' } }
    if (keycloak.authenticated) {
      return <Redirect to={from} />
    }

    const login = useCallback(() => {
      keycloak.login()
    }, [keycloak])
    
    return (
      <div style={{
      display: 'flex',
      justifyContent: 'center',
      height: '100%',
      flexBasis: '100px'
      }}>
        <button type="button" onClick={login} style={{alignSelf: 'center', flexBasis: '200px', minHeight: '50px'}}>
          Login
        </button>
      </div>
    )}
  )
)

export default LoginPage
