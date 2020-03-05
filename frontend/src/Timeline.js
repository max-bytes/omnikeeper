import { useQuery } from '@apollo/client';
import React from 'react';
import PropTypes from 'prop-types'
import { queries } from './queries'
import LoadingOverlay from 'react-loading-overlay'
import { Button } from 'react-bootstrap';
import { mutations } from './mutations';
import { useMutation } from '@apollo/react-hooks';

function Timeline(props) {
    // let visibleLayers = props.layers.filter(l => l.visibility).map(l => l.name);
    let allLayers = props.layers.map(l => l.name);

    var ciid = props.ciid;
    var from = "2010-01-01 00:00:00";
    var to = "2022-01-01 00:00:00";

    const { loading, error, data } = useQuery(queries.Changesets, {
      variables: { from: from, to: to, ciid: ciid, layers: allLayers }
    });

    // TODO: loading
    const [setSelectedTimeThreshold, { _ }] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);
  
    if (data) {
      var changesets = [...data.changesets].reverse();

      let activeChangeset = (props.currentTime.time === null) ? changesets.find(e => true) : changesets.find(cs => cs.timestamp === props.currentTime.time);

      if (!activeChangeset) {
        activeChangeset = {id: -1, timestamp: props.currentTime.time};

        function addAndSort2(arr, val) {
            arr.push(val);
            let i = arr.length - 1;
            const item = arr[i];
            while (i > 0 && item.timestamp > arr[i-1].timestamp) {
                arr[i] = arr[i-1];
                i -= 1;
            }
            arr[i] = item;
            return arr;
        }

        addAndSort2(changesets, activeChangeset);
      }
      
      const latestChangeset = changesets.find(e => true);

      return (<LoadingOverlay active={loading} spinner>
        {changesets.map((cs, index) => {
          if (activeChangeset === cs) {
            return (<Button variant="link" size="sm" disabled key={cs.id}>{cs.timestamp} &gt;</Button>);
          }
          const isLatest = latestChangeset === cs;
          return <Button key={cs.id} variant="link" size="sm" onClick={() => setSelectedTimeThreshold({variables: { newTimeThreshold: (isLatest) ? null : cs.timestamp, isLatest: isLatest }})}>{cs.timestamp}</Button>
        })}
      </LoadingOverlay>);
    } else if (loading) return <LoadingOverlay spinner text='Loading your content...'></LoadingOverlay>;
    else if (error) return <p>Error: {JSON.stringify(error, null, 2) }}</p>;
    else return <p>?</p>;
}

Timeline.propTypes = {
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
  

export default Timeline;