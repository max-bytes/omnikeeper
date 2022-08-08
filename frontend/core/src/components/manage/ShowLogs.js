import React, { useState, useEffect } from 'react';
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { Console } from 'console-feed'
import env from "@beam-australia/react-env";

const convertLogLevel2Method = (logLevel) => {
  switch (logLevel) {
    case 'Trace':
    case 'Debug':
      return 'debug';
    case 'Information':
      return 'info';
    case 'Warning':
      return 'warn';
    case 'Critical':
    case 'Error':
      return 'error';
    default:
      return 'log';
  }
};

export default function ShowVersion() {
  const [logs, setLogs] = useState([])
  
  const [/*hubConnection*/, setHubConnection] = useState(null);
    useEffect(() => {
        const createHubConnection = async () => {

            const hubConnect = new HubConnectionBuilder()
                .withUrl(`${env('BACKEND_URL')}/api/signalr/logging`)
                .build();
            try {
                setLogs(logs => [...logs, {method: 'info', data: ['Starting logs...']}]);

                hubConnect
                  .start()
                  .then(() => {
                    hubConnect.on('SendLogAsObject', (data) => {
                      const convertedLine = {
                        method: convertLogLevel2Method(data.level),
                        data: [data.timestamp, data.sourceContext, data.message]
                      };
                      setLogs(logs => [...logs, convertedLine]);
                    });
                  })
                  .catch(err => console.error(err));
            }
            catch (err) {
                console.error(err);
            }
            setHubConnection(hubConnect);
        }

        createHubConnection();
    }, []);

  return <>
    <h2>Logs</h2>
    <div style={{backgroundColor: '#333333', overflow: 'scroll', height: '100%'}}>
      <Console logs={logs} variant="dark" />
    </div>
  </>
}