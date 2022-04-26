import React, {useEffect, useState} from 'react';
import useSwaggerClient from 'utils/useSwaggerClient';
import PivotTableUI from 'react-pivottable/PivotTableUI';
import 'react-pivottable/pivottable.css';
import { DatePicker, Form, Space, Spin } from 'antd';
import moment from 'moment';

const { RangePicker } = DatePicker;

export default function UsageStats(props) {

  const { data: swaggerClient } = useSwaggerClient();

  const [data, setData] = useState(null);

  const [loadingData, setLoadingData] = useState(false);
  
  const [selectedTimeRange, setSelectedTimeRange] = useState([moment().startOf('day'), moment().endOf('day')]);
  
  const [pivotTableState, setPivotTableState] = useState({});

  useEffect(() => {
    async function fetch() {
      if (swaggerClient) {
        setLoadingData(true);
        await swaggerClient.apis.UsageStats.Fetch({
          version: 1,
          from: selectedTimeRange[0].toISOString(),
          to: selectedTimeRange[1].toISOString()
        })
        .then((result) => result.body)
        .then(elements => setData(elements))
        .finally(() => setLoadingData(false));
      }
    }

    fetch();

  }, [swaggerClient, setData, setLoadingData, selectedTimeRange]);

  if (!data) {
    return "Loading..."; // TODO
  }
  
  return <>
    <Space direction='vertical'>
      <h2>Usage Stats</h2>
      <Form layout='inline'>
        <Form.Item label="Date Range">
          <RangePicker
            showTime={{ format: 'HH:mm:ss' }}
            format="YYYY-MM-DD HH:mm:ss"
            showNow={true}
            value={selectedTimeRange}
            onChange={(dates) => setSelectedTimeRange(dates)}
            ranges={{
                Today: [moment().startOf('day'), moment().endOf('day')],
                'This Week': [moment().startOf('week'), moment().endOf('week')],
                'This Month': [moment().startOf('month'), moment().endOf('month')],
            }}
          />
        </Form.Item>
        {loadingData && <Form.Item><Spin /></Form.Item>}
      </Form>
      <PivotTableUI data={data} onChange={s => {
        delete s.data; // Hack to prevent new data from being overwritten by old data, see https://github.com/plotly/react-pivottable/issues/57#issuecomment-563322829
        setPivotTableState(s);
      }} {...pivotTableState} />
    </Space>
  </>;

}
