import { useQuery } from '@apollo/client';
import React, { useState } from 'react';
import PropTypes from 'prop-types'
import { queries } from '../graphql/queries'
import LoadingOverlay from 'react-loading-overlay'
import { Form, Row, Col, Button } from 'react-bootstrap';
import { Button as SemanticButton, Icon } from 'semantic-ui-react'
import { mutations } from '../graphql/mutations';
import { useMutation } from '@apollo/react-hooks';
import moment from 'moment'

function Timeline(props) {
    // let visibleLayers = props.layers.filter(l => l.visibility).map(l => l.name);
    let allLayers = props.layers.map(l => l.name);

    var ciid = props.ciid;
    var from = "2010-01-01 00:00:00";
    var to = "2022-01-01 00:00:00";

    const { loading: loadingChangesets, error, data, refetch: refetchChangesets } = useQuery(queries.Changesets, {
      variables: { from: from, to: to, ciid: ciid, layers: allLayers }
    });

    // TODO: loading
    const [setSelectedTimeThreshold] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);
    
    const [refetchingChangesets, setRefetchingChangesets] = useState(false);

  
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
        setRefetchingChangesets(true);
        refetchChangesets()
        .then(() => {
          setSelectedTimeThreshold({variables: { newTimeThreshold: moment(), isLatest: true }});
        }).finally(() => setRefetchingChangesets(false));
      }}><Icon loading={refetchingChangesets} fitted name={'sync'} /></SemanticButton>)

      return (
        <div>
          <Row className={"my-1"}>
            <Col className={["d-flex", "align-items-center"]}>
              <h5>Timeline</h5>
            </Col>
            <Col className={["flex-grow-0", "mr-1"]}>
              <Form inline onSubmit={e => e.preventDefault()}>
                {refreshButton}
              </Form>
            </Col>
          </Row>
          <LoadingOverlay active={loadingChangesets} spinner>
          {changesets.map((cs) => {

            const label = `${moment(cs.timestamp).format('YYYY-MM-DD HH:mm:ss')} (${cs.user.username})`;
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