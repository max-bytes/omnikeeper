import React from 'react'
import LayerIcon from './LayerIcon';

export function LayerColorCellRenderer(props) {
  return <LayerIcon color={props.value} />;
}
