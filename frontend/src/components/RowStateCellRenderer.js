import React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faPlus, faMinus, faEdit } from '@fortawesome/free-solid-svg-icons';

export function RowStateCellRenderer(props) {
  if (props.data.isNew) return (<FontAwesomeIcon icon={faPlus} />);
  else if (props.data.isDeleted) return (<FontAwesomeIcon icon={faMinus} />);
  else if (props.data.isEdited) return (<FontAwesomeIcon icon={faEdit} />);
  else return "";
}
