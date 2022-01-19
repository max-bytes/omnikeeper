import { Button } from 'antd';
import React from 'react';
import useSwaggerClient from "utils/useSwaggerClient";

export default function ManageRestartApplication() {

  const { data: swaggerClient, loading, error } = useSwaggerClient();
  
  if (error) return "Error:" + error;
  if (loading) return "Loading...";

  const restartApplication = async () => {
    await swaggerClient.apis.RestartApplication.Restart(
        { version: 1 }
    );
  };

  return <>
    <h2>Restart Application</h2>
    <Button onClick={async (e) => await restartApplication()} type='primary' style={{alignSelf: 'flex-start'}}>Restart</Button>
  </>;
}
