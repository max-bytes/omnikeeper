import { useQuery } from '@apollo/client';
import React from 'react';
import CI from './CI';
import PropTypes from 'prop-types'
import { queries } from '../graphql/queries'
import LoadingOverlay from 'react-loading-overlay'
import { Container } from 'react-bootstrap';
import { ErrorView } from './ErrorView';
import { useLayers } from '../utils/useLayers';
import { useSelectedTime } from '../utils/useSelectedTime';

function MainAreaCI(props) {

  const { data: visibleLayers } = useLayers(true);
  const selectedTime = useSelectedTime();

  // TODO: move into CI
  const timeThreshold = selectedTime.time;//(selectedTime.isLatest) ? null : selectedTime.time;
  const isEditable = selectedTime.isLatest;
  const { loading: loadingCI, error: errorCI, data: dataCI, refetch: refetchCI } = useQuery(queries.FullCI, {
    variables: { identity: props.ciid, layers: visibleLayers.map(l => l.name), timeThreshold, includeRelated: 0 }
    
    // fetchPolicy: (selectedTime.refreshNonce) ? 'network-only' : 'cache-first'
  });
  
  React.useEffect(() => { if (selectedTime.refreshNonceCI) refetchCI({fetchPolicy: 'network-only'}); }, [selectedTime, refetchCI]);


  if (dataCI) return (<LoadingOverlay active={loadingCI} spinner>
      <Container fluid>
        <CI timeThreshold={timeThreshold} ci={dataCI.ci} isEditable={isEditable} ></CI>
      </Container>
    </LoadingOverlay>);
  else if (loadingCI) return <p>Loading...</p>;
  else if (errorCI) return <ErrorView error={errorCI}/>;
  else return <p>?</p>;
}

MainAreaCI.propTypes = {
  ciid: PropTypes.string.isRequired
}
  
export default MainAreaCI;