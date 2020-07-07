import React from 'react'
import { Button } from 'semantic-ui-react'

export default function DeleteRowCellRenderer(props) {

  var isCurrentlyBeingDeleted = !!props.data.isDeleted;
  var flip = () => {
    props.flipDelete(props);
  }

  return <Button compact negative={!isCurrentlyBeingDeleted} icon='minus' content={(isCurrentlyBeingDeleted) ? 'Undelete' : 'Delete'} onClick={e => flip()}></Button>;
}
