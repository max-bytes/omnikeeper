import { useQuery } from '@apollo/client';
import React from 'react';
import CI from './CI';
import PropTypes from 'prop-types'
import { queries } from './queries'
import LoadingOverlay from 'react-loading-overlay'

function Timeline(props) {
    let visibleLayers = props.layers.filter(l => l.visibility).map(l => l.name);

    var ciid = props.ciid;
    var from = "2010-01-01 00:00:00";
    var to = "2022-01-01 00:00:00";

    const { loading, error, data } = useQuery(queries.Changesets, {
      variables: { from: from, to: to, ciid: ciid, layers: visibleLayers }
    });

    if (data) return (<LoadingOverlay active={loading} spinner>
        {data.changesets.map(cs => {
          console.log(cs);
          return <div></div>;
        })}
      </LoadingOverlay>);
    else if (loading) return <LoadingOverlay spinner text='Loading your content...'></LoadingOverlay>;
    else if (error) return <p>Error: {JSON.stringify(error, null, 2) }}</p>;
    else return <p>?</p>;
}

Timeline.propTypes = {
  ciid: PropTypes.number.isRequired,
  layers: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.number.isRequired,
      name: PropTypes.string.isRequired,
      visibility: PropTypes.bool.isRequired
    }).isRequired
  ).isRequired,
}
  

export default Timeline;