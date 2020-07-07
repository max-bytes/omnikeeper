import React from 'react'
import { Icon } from 'semantic-ui-react'

export function RowStateCellRenderer(props) {
  if (props.data.isNew) return (<Icon name="plus" />);
  else if (props.data.isDeleted) return (<Icon name="delete" />);
  else if (props.data.isEdited) return (<Icon name="edit" />);
  else return "";
}
