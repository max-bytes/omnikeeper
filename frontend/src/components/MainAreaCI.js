import { useQuery } from '@apollo/client';
import React from 'react';
import CI from './CI';
import PropTypes from 'prop-types'
import { queries } from '../graphql/queries'
import LoadingOverlay from 'react-loading-overlay'
import {Container} from 'react-bootstrap';
import { ErrorView } from './ErrorView';

// function useTraceUpdate(props) {
//   const prev = React.useRef(props);
//   React.useEffect(() => {
//     const changedProps = Object.entries(props).reduce((ps, [k, v]) => {
//       if (prev.current[k] !== v) {
//         ps[k] = [prev.current[k], v];
//       }
//       return ps;
//     }, {});
//     if (Object.keys(changedProps).length > 0) {
//       console.log('Changed props:', changedProps);
//     }
//     prev.current = props;
//   });
// }

function MainAreaCI(props) {
  // useTraceUpdate(props);

    let visibleLayers = props.layers.filter(l => l.visibility).map(l => l.name);

    // TODO: move into CI
    const timeThreshold = props.currentTime.time;
    const { loading: loadingCI, error: errorCI, data: dataCI } = useQuery(queries.FullCI, {
      variables: { identity: props.ciid, layers: visibleLayers, timeThreshold, includeRelated: false }
    });


    const isEditable = props.currentTime.isLatest;

    if (dataCI) return (<LoadingOverlay active={loadingCI} spinner>
        <Container fluid>
          <CI timeThreshold={timeThreshold} ci={dataCI.ci} layers={props.layers} visibleLayers={visibleLayers} isEditable={isEditable} ></CI>
        </Container>
      </LoadingOverlay>);
    else if (loadingCI) return <p>Loading...</p>;
    else if (errorCI) return <ErrorView error={errorCI}/>;
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