import React from 'react';
import { Link  } from 'react-router-dom'
import { Icon } from 'semantic-ui-react';
import { useQuery } from '@apollo/client';
import { queries } from '../../graphql/queries'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import ReactJson from 'react-json-view'

export default function ManageCITypes() {

  const { data } = useQuery(queries.Traits);

  if (!data) return "Loading";

  return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
    <h2>Traits</h2>
    <div style={{marginBottom: '10px'}}><Link to="/manage"><Icon name="angle left" fitted /> Back</Link></div>
    <p>TODO: make editable / manageable</p>
    <ReactJson name={false} collapsed={1} src={JSON.parse(data.traits)} enableClipboard={false} />
    
  </div>;
}
