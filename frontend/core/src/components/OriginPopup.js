import React, { useState } from 'react';
import { useLazyQuery } from '@apollo/client';
import PropTypes from 'prop-types'
import { queries } from '../graphql/queries'
import { Popover, Button } from 'antd';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSync, faExclamationCircle, faUser, faArchive, faPlug, faCogs, faCalculator, faInfo } from '@fortawesome/free-solid-svg-icons';
import UserTypeIcon from './UserTypeIcon';
import { formatTimestamp } from 'utils/datetime.js';
import { ChangesetID } from 'utils/uuidRenderers';

function InnerPopup(props) {

  const { changeset } = props;
  
  const dls = {display: 'flex', flexWrap: 'nowrap', marginBottom: '0px', whiteSpace: 'nowrap'};
  const dts = {width: '120px', textAlign: 'right', paddingRight: '10px' }
  return (<div style={{display: 'flex', flexFlow: 'column'}}>
    <dl style={dls}>
      <dt style={dts}>Origin-Type:</dt>
      <dd>{changeset.dataOrigin.type}</dd>
    </dl>
    <dl style={dls}>
      <dt style={dts}>User:</dt>
      <dd>{changeset ? <span style={{display: 'flex', flexWrap: 'nowrap', whiteSpace: 'nowrap'}}><UserTypeIcon style={{paddingRight: '3px'}} userType={changeset.user.type} /> {changeset.user.displayName}</span> : 'None'}</dd>
    </dl>
    <dl style={dls}>
      <dt style={dts}>Timestamp:</dt>
      <dd>{changeset ? formatTimestamp(changeset.timestamp) : 'None'}</dd>
    </dl>
    <dl style={dls}>
      <dt style={dts}>Changeset:</dt>
      <dd>{changeset ? <ChangesetID id={changeset.id} link={true} copyable={true} /> : 'None'}</dd>
    </dl>
    </div>
  );
}

function OriginPopup(props) {
  const { changesetID } = props;
  
  const [load, { loading, error, data }] = useLazyQuery(queries.BasicChangeset, {
    variables: { id: changesetID }
  });
  const [visible, setVisible] = useState(false);

  if (loading) return (<FontAwesomeIcon icon={faSync} />);
  if (error) return (<FontAwesomeIcon icon={faExclamationCircle} />);


  const icon = (function(originType) {
    switch(originType) {
      case 'MANUAL':
        return faUser;
      case 'INBOUND_INGEST':
        return faArchive;
      case 'INBOUND_ONLINE':
        return faPlug;
      case 'COMPUTE_LAYER':
        return faCogs;
      case 'GENERATOR':
        return faCalculator;
      case 'UNKNOWN':
        return faInfo;
      default:
        return faInfo;
    }
  })((data) ? ((data.changeset) ? data.changeset.dataOrigin.type : 'GENERATOR') : 'UNKNOWN');

    return (
      <Popover
        placement="topRight"
        trigger="click"
        content={data ? (data.changeset ? <InnerPopup changeset={data.changeset} /> : "No changeset / Calculated") : "Loading..."}
        // on='click'
        visible={visible}
        onVisibleChange={(visible) => setVisible(visible)}
        position='top right'
      >
        <Button size='small' style={{ marginRight: "5px" }} onClick={() => {load(); setVisible(old => { return !old; });}}>
          <FontAwesomeIcon icon={icon} color={"gray"} />
        </Button>
      </Popover>
    );
}

OriginPopup.propTypes = {
  changesetID: PropTypes.string.isRequired
}

export default OriginPopup;