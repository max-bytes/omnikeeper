import React from 'react'

export function ErrorView(props) {
  if (!props.error) return "";

  return (
    <div style={(props.inPopup) ? {maxWidth: '700px', maxHeight: '400px', display: 'flex', flexDirection: 'column'} : {}}>
      <h3>{props.error.name}</h3>
      <p style={{overflowY:'scroll'}}>{props.error.message}</p>
      <pre style={{overflowY:'scroll', whiteSpace: 'pre-wrap'}}>{props.error.stack}</pre>
      </div>
  )
}
