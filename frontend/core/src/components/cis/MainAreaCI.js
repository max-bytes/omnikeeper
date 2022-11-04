import { useQuery } from '@apollo/client';
import React from 'react';
import { Spin } from 'antd';
import CI from './CI';
import PropTypes from 'prop-types'
import { queries } from 'graphql/queries'
import { ErrorView } from 'components/ErrorView';
import { useExplorerLayers } from 'utils/layers';
import { useSelectedTime } from 'utils/useSelectedTime';

function MainAreaCI(props) {
  const { data: visibleLayers } = useExplorerLayers(true);

  if (visibleLayers) {
    return <LoadingCI visibleLayers={visibleLayers} ciid={props.ciid} />;
  } else return 'Loading...';
}

function LoadingCI(props) {

  const selectedTime = useSelectedTime();

  const timeThreshold = selectedTime.time;
  const isEditable = selectedTime.isLatest;
  const { loading: loadingCI, error: errorCI, data: dataCI, refetch: refetchCI } = useQuery(queries.FullCI, {
    variables: { ciid: props.ciid, layers: props.visibleLayers.map(l => l.id), timeThreshold }
  });
  
  React.useEffect(() => { if (selectedTime.refreshNonceCI) refetchCI(); }, [selectedTime, refetchCI]);

  if (dataCI) return (<Spin spinning={loadingCI} wrapperClassName="workaround-antd-spinner-flex-full-height">
      <CI timeThreshold={timeThreshold} ci={dataCI.cis[0]} isEditable={isEditable} ></CI>
    </Spin>);
  else if (loadingCI) return <div style={{display: "flex", height: "100%"}}><Spin spinning={true} size="large" tip="Loading...">&nbsp;</Spin></div>;
  else if (errorCI) return <ErrorView error={errorCI}/>;
  else return <p>?</p>;
}

MainAreaCI.propTypes = {
  ciid: PropTypes.string.isRequired
}
  
export default MainAreaCI;