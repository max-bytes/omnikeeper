import React, { useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faEquals } from '@fortawesome/free-solid-svg-icons'
import { faNotEqual } from '@fortawesome/free-solid-svg-icons'
import { faWaveSquare } from '@fortawesome/free-solid-svg-icons'

export function MissingLabel() {
    return <div style={{display: 'flex', minHeight: '38px', alignItems: 'center', justifyContent: 'center'}}>
      <span style={{color: 'red', fontWeight: 'bold'}}>Missing</span>
    </div>;
}
  
export function CompareLabel(props) {
    return <div style={{display: 'flex', minHeight: '38px', alignItems: 'center', justifyContent: 'center'}}>
    {(() => { switch (props.state) {
        case 'equal': return <span style={{color: 'green', fontWeight: 'bold'}}><FontAwesomeIcon icon={faEquals} size="lg" /></span>;
        case 'similar': return <span style={{color: 'orange', fontWeight: 'bold'}}><FontAwesomeIcon icon={faWaveSquare} size="lg" /></span>;
        default: return <span style={{color: 'red', fontWeight: 'bold'}}><FontAwesomeIcon icon={faNotEqual} size="lg" /></span>;
    }})()}
    </div>;
}

export function EmptyLabel() {
    return <div style={{display: 'flex', justifyContent: 'center', marginLeft: '220px', padding: '20px', fontSize: '1.4rem', fontWeight: 'bold'}}>
      Empty
    </div>;
  }
  