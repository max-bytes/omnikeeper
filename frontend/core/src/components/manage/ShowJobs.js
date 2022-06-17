import React, { useCallback, useState, useEffect } from 'react';
import { useLazyQuery } from '@apollo/client';
import { queries } from '../../graphql/queries_manage'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import moment from 'moment'
import _ from 'lodash';
import { Button, Spin } from 'antd';
import { SyncOutlined } from '@ant-design/icons';
import { formatTimestamp } from 'utils/datetime.js';

export default function ShowJobs(props) {

  const [fetchJobs, { loading, data }] = useLazyQuery(queries.Jobs, {
    notifyOnNetworkStatusChange: true
  });
  const debouncedFetchJobs = useCallback(_.debounce(fetchJobs, 500), [fetchJobs]);

  const [refreshNonce, setRefreshNonce] = useState(null);

  useEffect(() => {
      debouncedFetchJobs();
  }, [debouncedFetchJobs, refreshNonce]);


  if (!data) return "Loading...";

  return <>
    <div style={{display: 'flex', gap: '2em'}}>
      <h2>Running Jobs</h2>
      <Button icon={<SyncOutlined />} type="primary" loading={loading} onClick={() => setRefreshNonce(moment().toISOString())}>Refresh</Button>
    </div>
    <Spin spinning={loading}>
      <p>Currently running jobs: {data.manage_runningJobs.length}</p>
        <ul>
        {data.manage_runningJobs.map(rj => {
          const durationStr = moment.utc(moment.duration(rj.runningForMilliseconds, 'milliseconds').asMilliseconds()).format('mm:ss.SSS');
          return <li key={rj.name}>
            {rj.name}
            <ul>
              <li>Started at {formatTimestamp(rj.startedAt)}</li>
              <li>Running for {durationStr}</li>
            </ul>
          </li>;
        })}
        </ul>
    </Spin>
  </>
}
