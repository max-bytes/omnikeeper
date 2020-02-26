import { useQuery } from '@apollo/client';
import React from 'react';
import CI from './CI';
import PropTypes from 'prop-types'
import { queries } from './queries'


function MainAreaCI(props) {
    let visibleLayers = props.layers.filter(l => l.visibility).map(l => l.name);

    // TODO: identity variable

    const { loading: loadingCI, error: errorCI, data: dataCI } = useQuery(queries.CI, {
      variables: { identity: 'Habc', layers: visibleLayers }
    });

    if (loadingCI) return <p>Loading...</p>;
    else if (errorCI) return <p>Error: {JSON.stringify(errorCI, null, 2) }}</p>;
    else return (<CI ci={dataCI.ci} layers={props.layers}></CI>);
}

MainAreaCI.propTypes = {
    layers: PropTypes.arrayOf(
      PropTypes.shape({
        id: PropTypes.number.isRequired,
        name: PropTypes.string.isRequired,
        visibility: PropTypes.bool.isRequired
      }).isRequired
    ).isRequired,
  }
  

export default MainAreaCI;