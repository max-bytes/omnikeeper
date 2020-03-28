import React from 'react'

export function ErrorView(props) {
  return (
    <div>
      <h3>{props.error.name}</h3>
      <p>{props.error.message}</p>
      <p><pre>{props.error.stack}</pre></p>
      </div>
  )
}
