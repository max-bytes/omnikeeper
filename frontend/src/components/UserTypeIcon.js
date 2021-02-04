import React from 'react';
import PropTypes from 'prop-types'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCog, faUser, faQuestion } from '@fortawesome/free-solid-svg-icons';

function UserTypeIcon(props) {
  var iconName = (function(userType) {
    switch(userType) {
      case 'ROBOT':
        return faCog;
      case 'HUMAN':
        return faUser;
      default:
        return faQuestion;
    }
  })(props.userType);

  return <FontAwesomeIcon style={props.style} icon={iconName} />;
}

UserTypeIcon.propTypes = {
  userType: PropTypes.string.isRequired,
  style: PropTypes.object
}

export default UserTypeIcon;