import { useQuery } from '@apollo/client';
import React from 'react';
import PropTypes from 'prop-types'
import { queries } from '../graphql/queries'
import LoadingOverlay from 'react-loading-overlay'
import { Form, Button } from 'react-bootstrap';
import { Button as SemanticButton, Icon } from 'semantic-ui-react'
import { mutations } from '../graphql/mutations';
import { useMutation } from '@apollo/react-hooks';
import UserTypeIcon from './UserTypeIcon';
import moment from 'moment'

function Timeline(props) {
    let allLayers = props.layers.map(l => l.name);

    var ciid = props.ciid;
    var from = "2010-01-01 00:00:00";
    var to = "2022-01-01 00:00:00";

    const { loading: loadingChangesets, error, data, refetch: refetchChangesets } = useQuery(queries.Changesets, {
      variables: { from: from, to: to, ciid: ciid, layers: allLayers }
    });

    React.useEffect(() => { if (props.currentTime.isLatest) refetchChangesets(); }, [props.currentTime, refetchChangesets]);

    const [setSelectedTimeThreshold] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);

    if (data) {
      var changesets = [...data.changesets].reverse();

      let activeChangeset = (props.currentTime.isLatest) ? changesets.find(e => true) : changesets.find(cs => cs.timestamp === props.currentTime.time);

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
   
      const refreshButton = (<SemanticButton basic size='mini' compact onClick={() => {
          setSelectedTimeThreshold({variables: { newTimeThreshold: null, isLatest: true }});
      }}><Icon loading={loadingChangesets} fitted name={'sync'} /></SemanticButton>)

      return (
        <div>
          <div className={"d-flex align-items-center"}>
            <h5 className={"flex-grow-1 my-0"}>Timeline</h5>
            <Form inline onSubmit={e => e.preventDefault()}>
              {refreshButton}
            </Form>
          </div>
          <LoadingOverlay active={loadingChangesets} spinner>
          {changesets.map((cs) => {

            const userLabel = (cs.user) ? <span><UserTypeIcon userType={cs.user.type} /> {cs.user.username}</span> : '';
            const label = <span>{moment(cs.timestamp).format('YYYY-MM-DD HH:mm:ss')} - {userLabel}</span>;
            if (activeChangeset === cs) {
              return (<Button variant="link" size="sm" disabled key={cs.id}>{label} &gt;</Button>);
            }
            const isLatest = latestChangeset === cs;
            return <Button key={cs.id} variant="link" size="sm" onClick={() => setSelectedTimeThreshold({variables: { newTimeThreshold: cs.timestamp, isLatest: isLatest }})}>{label}</Button>
          })}
          </LoadingOverlay>
        </div>);
    } else if (loadingChangesets) return <LoadingOverlay spinner text='Loading your content...'></LoadingOverlay>;
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