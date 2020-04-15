import React from 'react'
import { Icon, Popup } from 'semantic-ui-react'
import { ErrorView } from './ErrorView'

export function ErrorModalCellRenderer(props) {
  var error = props.getValue();
  if (!error) return "";
  return (
    <Popup flowing on='click'
      trigger={<Icon color='red' name='warning circle' />}
    ><Popup.Content><ErrorView error={error} /></Popup.Content></Popup>
  )
}
