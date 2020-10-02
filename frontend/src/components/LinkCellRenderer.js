import React from 'react'
import { Link  } from 'react-router-dom'

export default function LinkCellRenderer(props) {
  return <Link to={props.link(props)}>{props.content(props)}</Link>;
}
