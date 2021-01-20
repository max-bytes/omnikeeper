import React from 'react';
import { useQuery } from '@apollo/client';
import PropTypes from 'prop-types'
import { queries } from '../graphql/queries'
import { Button, Popup, Icon } from 'semantic-ui-react'
import UserTypeIcon from './UserTypeIcon';
import { formatTimestamp } from 'utils/datetime.js';

function InnerPopup(props) {

  const { loading, error, data } = useQuery(queries.Changeset, {
    variables: { id: props.changesetID }
  });

  if (loading) return (<Icon loading name={'sync'} />);
  if (error) return (<Icon name={'exclamation circle'} />);
  if (data) {
    const dls = {display: 'flex', flexWrap: 'nowrap', marginBottom: '0px', whiteSpace: 'nowrap'};
    const dts = {width: '120px', textAlign: 'right', paddingRight: '10px' }
    return (<div style={{display: 'flex', flexFlow: 'column'}}>
      <dl style={dls}>
        <dt style={dts}>Origin-Type:</dt>
        <dd>{props.originType}</dd>
      </dl>
      <dl style={dls}>
        <dt style={dts}>User:</dt>
        <dd>{data.changeset ? <span style={{display: 'flex', flexWrap: 'nowrap', whiteSpace: 'nowrap'}}><UserTypeIcon style={{paddingRight: '3px'}} userType={data.changeset.user.type} /> {data.changeset.user.displayName}</span> : 'None'}</dd>
      </dl>
      <dl style={dls}>
        <dt style={dts}>Timestamp:</dt>
        <dd>{data.changeset ? formatTimestamp(data.changeset.timestamp) : 'None'}</dd>
      </dl>
      <dl style={dls}>
        <dt style={dts}>Changeset-ID:</dt>
        <dd>{data.changeset ? data.changeset.id : 'None'}</dd>
      </dl>
      </div>
    );
  }
}

function OriginPopup(props) {
    // var [loading, setLoading] = useState(false);

    const icon = (function(originType) {
      switch(originType) {
        case 'MANUAL':
          return 'user outline';
        case 'INBOUND_INGEST':
          return 'archive';
        case 'INBOUND_ONLINE':
          return 'plug';
        case 'COMPUTE_LAYER':
          return 'cogs';
        case 'GENERATOR':
          return 'calculator';
        default:
          return '';
      }
    })(props.originType)

    return (
      <Popup
        trigger={
          <Button onClick={e => e.preventDefault()} basic size='mini' compact icon={`${icon}`} />
        }
        content={<InnerPopup changesetID={props.changesetID} originType={props.originType} />}
        on='click'
        position='top right'
      />
  );
}

OriginPopup.propTypes = {
  changesetID: PropTypes.string.isRequired,
  originType: PropTypes.string.isRequired
}

export default OriginPopup;