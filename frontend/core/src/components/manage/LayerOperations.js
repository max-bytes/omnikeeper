import React, {useCallback, useState} from 'react';
import { Link } from 'react-router-dom'
import { useQuery, useMutation } from '@apollo/client';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faChevronLeft } from '@fortawesome/free-solid-svg-icons';
import { queries } from '../../graphql/queries_manage'
import { mutations } from '../../graphql/mutations_manage'
import { Button, Card, Col, Divider, Popconfirm, Row, Statistic, Typography } from "antd";
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import { useParams } from 'react-router-dom'
import useSwaggerClient from 'utils/useSwaggerClient';
import download from 'downloadjs';
const { Text } = Typography;

export default function LayerOperations(props) {
  const { layerID } = useParams();
  
  const { data, loading: loadingStatistics, refetch: refetchStatistics } = useQuery(queries.LayerStatistics, {
    variables: { layerID: layerID }
  });
  const { data: swaggerClient, loading: loadingSwaggerClient, error: errorSwaggerClient } = useSwaggerClient();

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

  var [exportingLayer, setExportingLayer] = useState(false);
  const exportLayer = useCallback(async () => {
    if (data && swaggerClient) {
      setExportingLayer(true);
      try {
        await swaggerClient.apis.ImportExportLayer.ExportLayer({ version: 1, layerID: data.manage_layerStatistics.layer.id })
          .then(response => {
            const contentDisposition = response.headers["content-disposition"];
            const filename = contentDisposition.split('filename=')[1].split(';')[0]; // taken from https://stackoverflow.com/questions/40939380/how-to-get-file-name-from-content-disposition
            download(response.data, filename);
          });
      } finally {
        setExportingLayer(false);
      }
    }
  }, [swaggerClient, data]);

  if (data && swaggerClient) {
    return <>
      <div style={{marginBottom: '10px'}}><Link to=".."><FontAwesomeIcon icon={faChevronLeft} /> Back</Link></div>
      <h2>Layer Statistics</h2>
      <h3>Layer-ID: {data.manage_layerStatistics.layer.id}</h3>
      <Row gutter={4}>
        <Col span={4}>
          <Statistic title="Active Attributes" value={data.manage_layerStatistics.numActiveAttributes} />
        </Col>
        <Col span={4}>
          <Statistic title="Attributes Changes" value={data.manage_layerStatistics.numAttributeChangesHistory} />
        </Col>
        <Col span={4}>
          <Statistic title="Active Relations" value={data.manage_layerStatistics.numActiveRelations} />
        </Col>
        <Col span={4}>
          <Statistic title="Relation Changes" value={data.manage_layerStatistics.numRelationChangesHistory} />
        </Col>
        <Col span={4}>
          <Statistic title="Layer Changesets" value={data.manage_layerStatistics.numLayerChangesetsHistory} />
        </Col>
      </Row>
      <Text italic>Note: showing statistics for stored data only, not showing data from online inbound adapters or generators</Text>

      <Divider />

      <h2>Layer Operations</h2>
      <Row gutter={16}>
        <Col span={8}>
          <Card title="Export Layer">
            <p><Text>Export the currently active attributes and relations of this layer into an .okl1 file and download it</Text></p>

            <Popconfirm
                title={`Are you sure you want to export layer ${data.manage_layerStatistics.layer.id}?`}
                onConfirm={exportLayer}
                okText="Yes, export!"
                cancelText="No, cancel"
            >
              <Button disabled={exportingLayer}>{exportingLayer ? 'Running...' : 'Export Layer'}</Button>
            </Popconfirm>
          </Card>
        </Col><Col span={8}>
          <Card title="Truncate Layer">
            <p><Text type="danger">WARNING: truncating a layer deletes ALL of its attributes and relations. This includes both currently active ones as well as all of its history.</Text></p>

            <Popconfirm
                title={`Are you sure you want to truncate layer ${data.manage_layerStatistics.layer.id}?`}
                onConfirm={truncateLayer}
                okText="Yes, truncate!"
                okButtonProps={{type: "danger"}}
                cancelText="No, cancel"
            >
              <Button type="danger" disabled={truncatingLayer}>{truncatingLayer ? 'Running...' : 'Truncate Layer!'}</Button>
            </Popconfirm>
          </Card>
        </Col>
      </Row>
        
      </>;
  } else if (loadingStatistics) {
    return "Loading";
  } else {
    return "Error";
  }
}
