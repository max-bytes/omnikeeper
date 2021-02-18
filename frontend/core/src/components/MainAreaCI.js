import { useQuery } from '@apollo/client';
import React from 'react';
import CI from './CI';
import PropTypes from 'prop-types'
import { queries } from '../graphql/queries'
import LoadingOverlay from 'react-loading-overlay' // TODO: switch to antd spin
import { ErrorView } from './ErrorView';
import { useExplorerLayers } from '../utils/layers';
import { useSelectedTime } from '../utils/useSelectedTime';

function MainAreaCI(props) {
  const { data: visibleLayers } = useExplorerLayers(true);

  if (visibleLayers) {
    return <LoadingCI visibleLayers={visibleLayers} ciid={props.ciid} />;
  } else return 'Loading...';
}

function LoadingCI(props) {

  const selectedTime = useSelectedTime();

  // TODO: move into CI
  const timeThreshold = selectedTime.time;//(selectedTime.isLatest) ? null : selectedTime.time;
  const isEditable = selectedTime.isLatest;
  const { loading: loadingCI, error: errorCI, data: dataCI, refetch: refetchCI } = useQuery(queries.FullCI, {
    variables: { ciid: props.ciid, layers: props.visibleLayers.map(l => l.name), timeThreshold, includeRelated: 0 }
  });
  
  React.useEffect(() => { if (selectedTime.refreshNonceCI) refetchCI({fetchPolicy: 'network-only'}); }, [selectedTime, refetchCI]);

  if (dataCI) return (<LoadingOverlay active={loadingCI} spinner>
      <div style={{ width: "100%", padding: "0 15px" }}>
        <CI timeThreshold={timeThreshold} ci={dataCI.ci} isEditable={isEditable} ></CI>
      </div>
    </LoadingOverlay>);
  else if (loadingCI) return <p>Loading...</p>;
  else if (errorCI) return <ErrorView error={errorCI}/>;
  else return <p>?</p>;
}

MainAreaCI.propTypes = {
  ciid: PropTypes.string.isRequired
}
  
export default MainAreaCI;