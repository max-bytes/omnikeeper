import React from 'react'
import { Button } from 'antd';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faMinus } from '@fortawesome/free-solid-svg-icons';

export default function DeleteRowCellRenderer(props) {

  var isCurrentlyBeingDeleted = !!props.data.isDeleted;
  var flip = () => {
    props.flipDelete(props);
  }

  return <Button danger={!isCurrentlyBeingDeleted} onClick={e => flip()}><FontAwesomeIcon icon={faMinus} style={{marginRight: "10px"}}/>{(isCurrentlyBeingDeleted) ? 'Undelete' : 'Delete'}</Button>;
}
