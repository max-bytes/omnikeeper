import React, { useState } from 'react';
import { useQuery } from '@apollo/client';
import PropTypes from 'prop-types'
import { queries } from '../graphql/queries'
import { Button, Popup, Icon } from 'semantic-ui-react'
import UserTypeIcon from './UserTypeIcon';

function InnerPopup(props) {

  const { loading, error, data } = useQuery(queries.Changeset, {
    variables: { id: props.changesetID }
  });

  props.onLoadingChange(loading);

  if (loading) return (<Icon loading name={'sync'} />);
  if (error) return (<Icon name={'exclamation circle'} />);
  if (data) {
    if (!data.changeset) return (<Icon name={'exclamation circle'} />);
    const userLabel = <span style={{display: 'flex', flexWrap: 'nowrap', whiteSpace: 'nowrap'}}><UserTypeIcon style={{paddingRight: '3px'}} userType={data.changeset.user.type} /> {data.changeset.user.displayName}</span>;
    const dls = {display: 'flex', flexWrap: 'nowrap', marginBottom: '0px'};
    const dts = {width: '120px', textAlign: 'right', paddingRight: '10px' }
    return (<div style={{display: 'flex', flexFlow: 'column'}}>
      <dl style={dls}>
        <dt style={dts}>User:</dt>
        <dd>{userLabel}</dd>
      </dl>
      <dl style={dls}>
        <dt style={dts}>Changeset-ID:</dt>
        <dd>{data.changeset.id}</dd>
      </dl>
      </div>
    );
  }
}

function ChangesetPopup(props) {
    var [loading, setLoading] = useState(false);
    return (
      <Popup popperDependencies={[loading]}
        trigger={
          <Button onClick={e => e.preventDefault()} basic size='mini' compact icon="user outline" />
        }
        content={<InnerPopup onLoadingChange={e => setLoading(e)} changesetID={props.changesetID} />}
        on='click'
        position='top right'
      />
  );
}

ChangesetPopup.propTypes = {
  changesetID: PropTypes.number.isRequired
}

export default ChangesetPopup;