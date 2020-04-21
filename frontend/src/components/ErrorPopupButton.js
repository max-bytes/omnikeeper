import React from 'react'
import { Icon, Popup } from 'semantic-ui-react'
import { ErrorView } from './ErrorView'

export function ErrorPopupButton(props) {
  if (!props.error) return "";
  return (
    <Popup className="semantic-popup" position="right center" positionFixed={true} on='click' trigger={<Icon color='red' name='warning circle' />}
    ><Popup.Content><ErrorView error={props.error} inPopup={true} /></Popup.Content></Popup>
  )
}
