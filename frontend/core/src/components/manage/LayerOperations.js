import React, {useState} from 'react';
import { Link } from 'react-router-dom'
import { useQuery, useMutation } from '@apollo/client';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faChevronLeft } from '@fortawesome/free-solid-svg-icons';
import { queries } from '../../graphql/queries_manage'
import { mutations } from '../../graphql/mutations_manage'
import { Button } from "antd";
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import { useParams } from 'react-router-dom'

export default function LayerOperations(props) {
  const { layerID } = useParams();
  
  const { data, loading: loadingStatistics, refetch: refetchStatistics } = useQuery(queries.LayerStatistics, {
    variables: { layerID: layerID }
  });

  var [truncatingLayer, setTruncatingLayer] = useState(false);
  const [truncateLayerMutation] = useMutation(mutations.TRUNCATE_LAYER);

  function truncateLayer() {
    setTruncatingLayer(true);
    truncateLayerMutation({ variables: { id: layerID } })
    .then(d => {
      return refetchStatistics();
    }).catch(e => {
      console.log(e);
    }).finally(() => {
      setTruncatingLayer(false);
    });
  }

  if (data) {
    return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
      <h2>Layer Operations</h2>
      <div style={{marginBottom: '10px'}}><Link to=".."><FontAwesomeIcon icon={faChevronLeft} /> Back</Link></div>
        <div>Statistics for layer: {data.manage_layerStatistics.layer.id}</div>
        <div>Note: showing statistics for stored data only, not showing data from online inbound adapters or generators</div>
        <div>
          # active attributes: {data.manage_layerStatistics.numActiveAttributes}
        </div>
        <div>
          # attribute changes: {data.manage_layerStatistics.numAttributeChangesHistory}
        </div>
        <div>
          # active relations: {data.manage_layerStatistics.numActiveRelations}
        </div>
        <div>
          # relation changes: {data.manage_layerStatistics.numRelationChangesHistory}
        </div>
        <div>
          # layer changesets: {data.manage_layerStatistics.numLayerChangesetsHistory}
        </div>

        <Button type="danger" onClick={truncateLayer} disabled={truncatingLayer || loadingStatistics}>{truncatingLayer ? 'Running...' : 'Truncate Layer!'}</Button>
      </div>;
  } else if (loadingStatistics) {
    return "Loading";
  } else {
    return "Error";
  }
}
