import React from 'react';
import { Link  } from 'react-router-dom'

export default function Manage(props) {
  return <div><h2>Manage</h2>
    <ul>
      <li><Link to="/manage/baseConfiguration">Manage Base Configuration</Link></li>
      <li><Link to="/manage/predicates">Manage Predicates</Link></li>
      <li><Link to="/manage/layers">Manage Layers</Link></li>
      <li><Link to="/manage/traits">Manage Traits</Link></li>
      <li><Link to="/manage/cache">Manage Cache</Link></li>
      <li><Link to="/manage/oiacontexts">Manage Online Inbound Layer Contexts</Link></li>
      <li><Link to="/manage/odataapicontexts">Manage OData API Contexts</Link></li>
      <li><Link to="/manage/version">Version</Link></li>
      <li><Link to="/manage/current-user">Debug: current user</Link></li>
      
    </ul>
  </div>;
}
