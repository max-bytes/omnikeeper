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
    if (!data.changeset) return (<Icon name={'exclamation circle'} />);
    const userLabel = <span style={{display: 'flex', flexWrap: 'nowrap', whiteSpace: 'nowrap'}}><UserTypeIcon style={{paddingRight: '3px'}} userType={data.changeset.user.type} /> {data.changeset.user.displayName}</span>;
    const dls = {display: 'flex', flexWrap: 'nowrap', marginBottom: '0px', whiteSpace: 'nowrap'};
    const dts = {width: '120px', textAlign: 'right', paddingRight: '10px' }
    return (<div style={{display: 'flex', flexFlow: 'column'}}>
      <dl style={dls}>
        <dt style={dts}>Origin-Type:</dt>
        <dd>{props.originType}</dd>
      </dl>
      <dl style={dls}>
        <dt style={dts}>User:</dt>
        <dd>{userLabel}</dd>
      </dl>
      <dl style={dls}>
        <dt style={dts}>Timestamp:</dt>
        <dd>{formatTimestamp(data.changeset.timestamp)}</dd>
      </dl>
      <dl style={dls}>
        <dt style={dts}>Changeset-ID:</dt>
        <dd>{data.changeset.id}</dd>
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
        case 'INBOUNDINGEST':
          return 'archive';
        case 'INBOUNDONLINE':
          return 'plug';
        case 'COMPUTELAYER':
          return 'cogs';
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