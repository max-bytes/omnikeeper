import { useQuery } from '@apollo/client';
import React, { useState } from 'react';
import PropTypes from 'prop-types'
import { queries } from 'graphql/queries'
import LoadingOverlay from 'react-loading-overlay' // TODO: switch to antd spin
import { Form, Button } from "antd";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faSync, faExchangeAlt, faArrowDown } from '@fortawesome/free-solid-svg-icons'
import { mutations } from 'graphql/mutations';
import { useMutation } from '@apollo/client';
import UserTypeIcon from './UserTypeIcon';
import { formatTimestamp } from 'utils/datetime.js';
import { ErrorView } from './ErrorView';
import { useExplorerLayers } from 'utils/layers';
import { useSelectedTime } from 'utils/useSelectedTime';
import { Link } from 'react-router-dom';
import { buildDiffingURLQueryBetweenChangesets } from 'components/diffing/Diffing'
import _ from "lodash"

function Timeline(props) {
  const { data: layers } = useExplorerLayers();

  if (layers) {
    return <LoadingTimeline layers={layers} ciid={props.ciid} />;
  } else return 'Loading...';
}

function LoadingTimeline(props) {
  
  const selectedTime = useSelectedTime();

  var ciid = props.ciid;
  var from = "2010-01-01 00:00:00";
  var to = "2022-01-01 00:00:00";
  var [limit, setLimit] = useState(10);

  const { loading: loadingChangesets, error, data, refetch: refetchChangesets } = useQuery(queries.Changesets, {
    variables: { from: from, to: to, ciids: [ciid], layers: props.layers.map(l => l.name), limit: limit } // TODO
  });

  React.useEffect(() => { if (selectedTime.refreshNonceTimeline) refetchChangesets({fetchPolicy: 'network-only'}); }, [selectedTime, refetchChangesets]);

  const [setSelectedTimeThreshold] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);
  
  var { data: layerSettingsData } = useQuery(queries.LayerSettings);

  if (data && layerSettingsData) {
    var changesets = [...data.changesets]; // TODO: why do we do a copy here?

    let activeChangeset = (selectedTime.isLatest) ? changesets.find(e => true) : changesets.find(cs => cs.timestamp === selectedTime.time);

    if (!activeChangeset) {
      activeChangeset = {id: -1, timestamp: selectedTime.time};

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
  
    const refreshButton = (<Button size='small' onClick={() => {
        setSelectedTimeThreshold({variables: { newTimeThreshold: null, isLatest: true, refreshTimeline: true, refreshCI: true }});
    }}><FontAwesomeIcon icon={faSync} spin={loadingChangesets} color={"grey"} style={{ padding: "2px"}} /></Button>)

          
    const buttonStyle = {
      textAlign: 'left',
      flexGrow: 1,
      overflow: 'hidden',
      whiteSpace: 'nowrap',
      textOverflow: 'ellipsis',
    };
    const diffButtonStyle = {
    };
    const lineStyle = {
      display: 'flex',
      width: '100%',
      paddingRight: '5px'
    };

    return (
      <div>
        <div className={"d-flex align-items-center"} style={{minHeight: "24px"}}>
          <h5 className={"flex-grow-1 my-0"} style={{float: "left"}}>Timeline</h5>
          <Form layout="inline" style={{float: "right"}}>
            {refreshButton}
          </Form>
        </div>
        <LoadingOverlay active={loadingChangesets} spinner>
            
          {changesets.map((cs) => {
            const userLabel = (cs.user) ? <span><UserTypeIcon userType={cs.user.type} /> {cs.user.displayName}</span> : '';
            const label = <span style={((activeChangeset === cs) ? {fontWeight: 'bold'} : {})}>{formatTimestamp(cs.timestamp)} - {userLabel}</span>;
            if (activeChangeset === cs) {
              return (<Button style={buttonStyle} type="link" size="small" disabled key={cs.id}>{label}</Button>);
            }
            const isLatest = latestChangeset === cs;
            const diffQuery = buildDiffingURLQueryBetweenChangesets(layerSettingsData.layerSettings, ciid, (latestChangeset === activeChangeset) ? null : activeChangeset.timestamp, (isLatest) ? null : cs.timestamp);
            return (<div style={lineStyle} key={cs.id}>
                <Button style={buttonStyle} type="link" size="small" 
                onClick={() => setSelectedTimeThreshold({variables: { newTimeThreshold: (isLatest) ? null : cs.timestamp, isLatest: isLatest }})}>
                  {label}
                </Button>
                <Link style={diffButtonStyle} 
                  to={`/diffing?${diffQuery}`}><FontAwesomeIcon icon={faExchangeAlt} /></Link>
              </div>);
          })}
          <Form layout="inline" style={{justifyContent: "center"}}>
            {!(limit > _.size(changesets)) && <Button size='small' onClick={() => {
              setLimit(l => l + 10);
            }}><FontAwesomeIcon icon={loadingChangesets ? faSync : faArrowDown} spin={loadingChangesets} color={"grey"} style={{ padding: "2px"}} /></Button>}
          </Form>
        </LoadingOverlay>
      </div>);
  } else if (loadingChangesets) return ( 
        <div style={{ height: (limit-8)*24+1 + "px" }}>  {/* setting the height to a specific, limit-dependent value ensures, that it doesn't 'jump around' while loading */}
            <div className={"d-flex align-items-center"} style={{minHeight: "24px"}}>
                <h5 className={"flex-grow-1 my-0"} style={{float: "left"}}>Timeline</h5>
            </div>
            <p>Loading...</p>
        </div>
  )
  else if (error) return <ErrorView error={error}/>;
  else return <p>?</p>;
}

Timeline.propTypes = {
  // currentTime: PropTypes.shape({
  //   time: PropTypes.string,
  //   isLatest: PropTypes.bool.isRequired
  // }),
  ciid: PropTypes.string.isRequired,
}
  

export default Timeline;