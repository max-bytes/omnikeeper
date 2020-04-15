// import { useQuery } from '@apollo/client';
// import React from 'react';
// import CIs from './CIs';
// import PropTypes from 'prop-types'
// import { queries } from '../graphql/queries'
// import { ErrorView } from './ErrorView';


// function MainAreaMultiCIs(props) {
//     let visibleLayers = props.layers.filter(l => l.visibility).map(l => l.name);

//     const { loading: loadingCIs, error: errorCIs, data: dataCIs } = useQuery(queries.CIs, {
//       variables: { layers: visibleLayers }
//     });

//     if (loadingCIs) return <p>Loading...</p>;
//     else if (errorCIs) return <ErrorView error={errorCI}/>;
//     else return (<CIs cis={dataCIs.cis} layers={visibleLayers}></CIs>);
// }

// MainAreaMultiCIs.propTypes = {
//     layers: PropTypes.arrayOf(
//       PropTypes.shape({
//         id: PropTypes.number.isRequired,
//         name: PropTypes.string.isRequired,
//         visibility: PropTypes.bool.isRequired
//       }).isRequired
//     ).isRequired,
//   }
  

// export default MainAreaMultiCIs;