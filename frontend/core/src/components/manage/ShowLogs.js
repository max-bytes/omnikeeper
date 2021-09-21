import React, { useState, useEffect } from 'react';
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import { HubConnectionBuilder } from '@aspnet/signalr';
import { Console } from 'console-feed'
import env from "@beam-australia/react-env";

export default function ShowVersion() {
  const [logs, setLogs] = useState([])
  
  const [/*hubConnection*/, setHubConnection] = useState(null);
    useEffect(() => {
        const createHubConnection = async () => {

            const hubConnect = new HubConnectionBuilder()
                .withUrl(`${env('BACKEND_URL')}/api/signalr/logging`)
                .build();
            try {
                await hubConnect.start()

                setLogs(logs => [...logs, {method: 'info', data: ['Starting logs...']}]);

                hubConnect.stream("StreamLogs").subscribe({
                  next: (line) => {

                      const convertLogLevel2Method = (logLevel) => {
                        switch (logLevel) {
                          case 0:
                          case 1:
                          case 6:
                            return 'debug';
                          case 2:
                            return 'info';
                          case 3:
                            return 'warn';
                          case 4:
                          case 5:
                            return 'error';
                          default:
                            return 'log';
                        }
                      };
                      const convertedLine = {
                        method: convertLogLevel2Method(line.logLevel),
                        data: [line.category, line.message]
                      };
                      setLogs(logs => [...logs, convertedLine]);
                  },
                  error: (err) => { },
                  complete: () => { }
              });
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