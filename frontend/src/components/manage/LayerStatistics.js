import React from 'react';
import { Link } from 'react-router-dom'
import { useQuery } from '@apollo/client';
import { Icon } from 'semantic-ui-react';
import { queries } from '../../graphql/queries'
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import { useParams } from 'react-router-dom'

export default function LayerStatistics(props) {
  const { layerID } = useParams();
  
  const { data, loading } = useQuery(queries.LayerStatistics, {
    variables: { layerID: layerID }
  });

  if (data) {
    return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
      <h2>Layer Statistics</h2>
      <div style={{marginBottom: '10px'}}><Link to="/manage/layers"><Icon name="angle left" fitted /> Back</Link></div>
        <div>For layer: {data.layerStatistics.layer.name}</div>
        <div>
          # active attributes: {data.layerStatistics.numActiveAttributes}
        </div>
        <div>
          # attribute changes history: {data.layerStatistics.numAttributeChangesHistory}
        </div>
        <div>
          # active relations: {data.layerStatistics.numActiveRelations}
        </div>
        <div>
          # realtion changes history: {data.layerStatistics.numRelationChangesHistory}
        </div>
        <div>
          # layer changests history: {data.layerStatistics.numLayerChangesetsHistory}
        </div>
      </div>;
  } else if (loading) {
    return "Loading";
  } else {
    return "Error";
  }
}
