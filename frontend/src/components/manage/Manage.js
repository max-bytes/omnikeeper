import React from 'react';
import { Link  } from 'react-router-dom'

export default function Manage(props) {
  return <div><h2>Manage</h2>
    <ul>
      <li><Link to="/manage/predicates">Manage Predicates</Link></li>
      <li><Link to="/manage/layers">Manage Layers</Link></li>
      <li><Link to="/manage/citypes">Manage CITypes</Link></li>
    </ul>
  </div>;
}
