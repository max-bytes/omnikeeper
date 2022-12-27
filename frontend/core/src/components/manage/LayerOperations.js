import React, {useCallback, useState} from 'react';
import { useQuery, useMutation } from '@apollo/client';
import { queries } from '../../graphql/queries_manage'
import { mutations } from '../../graphql/mutations_manage'
import { Button, Card, Col, Divider, Popconfirm, Row, Statistic, Typography, Upload, Space, Alert, Radio, Form, Spin } from "antd";
import 'ag-grid-community/dist/styles/ag-grid.css';
import 'ag-grid-community/dist/styles/ag-theme-balham.css';
import { useParams } from 'react-router-dom'
import useSwaggerClient from 'utils/useSwaggerClient';
import download from 'downloadjs';
import { UploadOutlined } from '@ant-design/icons';
import MultiCISelect from 'components/MultiCISelect';
import { formatTimestamp } from 'utils/datetime.js';
const { Text } = Typography;

export default function LayerOperations(props) {
  const { layerID } = useParams();
  
  const { data, loading: loadingStatistics, refetch: refetchStatistics, error } = useQuery(queries.LayerStatistics, {
    variables: { layerID: layerID }
  });
  const { data: swaggerClient } = useSwaggerClient();

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

  
  var [importingLayers, setImportingLayers] = useState(false);
  const [fileToImport, setFileToImport] = useState(undefined);
  const [importError, setImportError] = useState(undefined);
  const [importSuccess, setImportSuccess] = useState(undefined);
  const importLayer = useCallback(async () => {
    if (swaggerClient) {
      setImportingLayers(true);
      setImportError(undefined);
      setImportSuccess(undefined);
      try {
        await swaggerClient.apis.ImportExportLayer.ImportExportLayer_ImportLayer({ 
            version: 1, 
            overwriteLayerID: layerID
          }, {
            requestBody: {
              files: [fileToImport]
            }
          })
          .then(response => {
            if (response.status === 200) {
              setFileToImport(undefined);
              setImportSuccess('Import successful');
            } else {
              setImportError(`Unknown error, HTTP response: ${response.status}`);
            }
          }).then(d => {
            return refetchStatistics();
          }).catch(e => {
            setImportError(e.response.text);
          });
      } finally {
        setImportingLayers(false);
      }
    }
  }, [swaggerClient, fileToImport, setFileToImport, refetchStatistics, layerID]);

    return <>
      <h2>Layer Statistics</h2>
      <h3>Layer-ID: {layerID}</h3>
      <Spin spinning={loadingStatistics}>
        {data && <>
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
            <Col span={4}>
              <Statistic title="Latest Change" value={data.manage_layerStatistics.latestChange} formatter={(value) => (value) ? formatTimestamp(value) : "Unknown" } />
            </Col>
          </Row>
        </>}
        {error && <div>error</div>}
        <Text italic>Note: showing statistics for stored data only, not showing data from generators</Text>
      </Spin>

      <Divider />

      <h2>Layer Operations</h2>
      <Row gutter={16}>
        <Col span={8}>
          <Card title="Export Layer">
            <ExportLayer layerID={layerID} swaggerClient={swaggerClient} />
          </Card>
        </Col><Col span={8}>
          <Card title="Import Layer">
            <p><Text>Import the attributes and relations from an .okl1 file and insert them into the current layer.</Text></p>
            <p><Text>Note: the imported data will overwrite any existing data in the layer. After import, the layer data matches the data in the import file. 
              However, only data that is actually different will be written. For example, importing the same file a second time will result in no second changeset because there are no changes to be done.</Text></p>
            <p><Text>Note: the data will be inserted as a new changeset from your current user and at the current timestamp.</Text></p>
            <Space direction="vertical">
              <Upload fileList={fileToImport ? [fileToImport] : []}
                beforeUpload={file => {
                  setFileToImport(file);
                  return false;
                }}
                onRemove={file => {
                  setFileToImport(null);
                }}
                >
                <Button icon={<UploadOutlined />}>Click to Select File</Button>
              </Upload>
              <Button type="primary" onClick={importLayer} disabled={importingLayers || !fileToImport} loading={importingLayers}>Import</Button>
              
              {importError && <Alert message="Error" type="error" showIcon closable description={importError} />}
              {importSuccess && <Alert message="Success" type="success" showIcon closable description={importSuccess} />}
            </Space>
          </Card>
        </Col><Col span={8}>
          <Card title="Truncate Layer">
            <p><Text type="danger">WARNING: truncating a layer deletes ALL of its attributes and relations. This includes both currently active ones as well as all of its history.</Text></p>

            <Popconfirm
                title={`Are you sure you want to truncate layer ${layerID}?`}
                onConfirm={truncateLayer}
                okText="Yes, truncate!"
                okButtonProps={{type: "danger"}}
                cancelText="No, cancel"
            >
              <Button type="danger" disabled={truncatingLayer} loading={truncatingLayer}>{truncatingLayer ? 'Running...' : 'Truncate Layer!'}</Button>
            </Popconfirm>
          </Card>
        </Col>
      </Row>
        
      </>;
}

function ExportLayer(props) {
  const {layerID, swaggerClient, } = props;
  
  var [exportingLayer, setExportingLayer] = useState(false);
  const [selectedCIIDs, setSelectedCIIDs] = useState(null);

  const exportLayer = useCallback(async () => {
    if (swaggerClient) {
      setExportingLayer(true);
      try {
        await swaggerClient.apis.ImportExportLayer.ImportExportLayer_ExportLayer({ version: 1, layerID: layerID, ciids: selectedCIIDs })
          .then(response => {
            const contentDisposition = response.headers["content-disposition"];
            const filename = contentDisposition.split('filename=')[1].split(';')[0]; // taken from https://stackoverflow.com/questions/40939380/how-to-get-file-name-from-content-disposition
            download(response.data, filename);
          });
      } finally {
        setExportingLayer(false);
      }
    }
  }, [swaggerClient, selectedCIIDs, layerID]);


  return <>
      <p><Text>Export the currently active attributes and relations of this layer into an .okl1 file and download it</Text></p>
      <p><Text>Note: this export only exports the currently active attributes and relations; it does NOT export any historic data</Text></p>
      <p><Text>Note: when selecting specific CIs, only relations where BOTH ends are part of the selection get exported</Text></p>
      <Form>
        <ExportLayerSelectCIs layerID={layerID} selectedCIIDs={selectedCIIDs} setSelectedCIIDs={setSelectedCIIDs} />
        <Form.Item>
          <Popconfirm 
              title={`Are you sure you want to export layer ${layerID}?`}
              onConfirm={exportLayer}
              okText="Yes, export!"
              cancelText="No, cancel"
          >
            <Button disabled={exportingLayer} loading={exportingLayer}>{exportingLayer ? 'Running...' : 'Export Layer'}</Button>
          </Popconfirm>
        </Form.Item>
      </Form>
    </>;
}

function ExportLayerSelectCIs(props) {
    
  const { layerID, selectedCIIDs, setSelectedCIIDs } = props;

  const type = (selectedCIIDs === null) ? 0 : 1;

  const layerIDs = [layerID];

  return <>
    <Form.Item>
      <Radio.Group onChange={(e) => setSelectedCIIDs((ts) => e.target.value === 0 ? null : [])} defaultValue={type}>
        <Radio id={`ci-select-all`} value={0} checked={type === 0}>All CIs</Radio>
        <Radio id={`ci-select-specific`} value={1} checked={type === 1}>Specific CIs</Radio>
      </Radio.Group>
    </Form.Item>
    {type === 1 && 
        <Form.Item>
          <MultiCISelect layers={layerIDs} selectedCIIDs={selectedCIIDs} setSelectedCIIDs={setSelectedCIIDs} />
        </Form.Item>
    }
  </>;
}