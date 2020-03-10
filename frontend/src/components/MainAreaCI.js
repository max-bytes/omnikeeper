import { useQuery } from '@apollo/client';
import React from 'react';
import CI from './CI';
import PropTypes from 'prop-types'
import { queries } from '../graphql/queries'
import LoadingOverlay from 'react-loading-overlay'
import {Container} from 'react-bootstrap';

function MainAreaCI(props) {
    let visibleLayers = props.layers.filter(l => l.visibility).map(l => l.name);

    // TODO: identity variable

    const { loading: loadingCI, error: errorCI, data: dataCI } = useQuery(queries.CI, {
      variables: { identity: props.ciid, layers: visibleLayers, timeThreshold: props.currentTime.time }
    });

    const isEditable = props.currentTime.isLatest;

    if (dataCI) return (<LoadingOverlay active={loadingCI} spinner>
        <Container fluid>
          <CI ci={dataCI.ci} layers={props.layers} isEditable={isEditable} ></CI>
        </Container>
      </LoadingOverlay>);
    else if (loadingCI) return <LoadingOverlay spinner text='Loading your content...'></LoadingOverlay>;
    else if (errorCI) return <p>Error: {JSON.stringify(errorCI, null, 2) }}</p>;
    else return <p>?</p>;
}

MainAreaCI.propTypes = {
    currentTime: PropTypes.shape({
      time: PropTypes.string,
      isLatest: PropTypes.bool.isRequired
    }),
    ciid: PropTypes.string.isRequired,
    layers: PropTypes.arrayOf(
      PropTypes.shape({
        id: PropTypes.number.isRequired,
        name: PropTypes.string.isRequired,
        visibility: PropTypes.bool.isRequired
      }).isRequired
    ).isRequired,
  }
  

export default MainAreaCI;