import React from 'react';
import PropTypes from 'prop-types'
import { Icon } from 'semantic-ui-react';

function UserTypeIcon(props) {
  var iconName = (function(userType) {
    switch(userType) {
      case 'ROBOT':
        return 'cog';
      case 'HUMAN':
        return 'user';
      default:
        return 'question';
    }
  })(props.userType);

  return <Icon style={props.style} fitted name={iconName} />;
}

UserTypeIcon.propTypes = {
  userType: PropTypes.string.isRequired,
  style: PropTypes.object
}

export default UserTypeIcon;