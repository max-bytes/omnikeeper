import React, { useState } from "react";
import env from "@beam-australia/react-env";
import { useExplorerLayers } from "utils/layers";
import { Button, Spin, Row, Col, DatePicker } from 'antd';
import { SyncOutlined } from '@ant-design/icons';
import Paragraph from "antd/lib/typography/Paragraph";
import Text from "antd/lib/typography/Text";
import _ from 'lodash';
import Graph from "./Graph.js";
import moment from 'moment';

const { RangePicker } = DatePicker;

export default function LayerCentricUsageGraphRendering(props) {
    
    const [graphDefinition, setGraphDefinition] = useState(null);
    const [graphDefinitionLoading, setGraphDefinitionLoading] = useState(null);

    const { data: visibleLayers } = useExplorerLayers(true);
    const layerIDs = visibleLayers.map(l => l.id);

    const [selectedTimeRange, setSelectedTimeRange] = useState([moment().startOf('day'), moment().endOf('day')]);

    return <div style={styles.container}>
        <div style={styles.filterColumn}>
            <h2>Layer-Centric Usage Graph Rendering</h2>
            <div style={styles.filterColumnEntry}>
                <Paragraph>
                    Note: only data from visible layers will be shown. Make sure to mark the desired layers as visible in the layer side-bar.
                </Paragraph>
            </div>
            <div style={styles.filterColumnEntry}>
                <RangePicker
                    showTime={{ format: 'HH:mm:ss' }}
                    format="YYYY-MM-DD HH:mm:ss"
                    showNow={true}
                    value={selectedTimeRange}
                    onChange={(dates) => setSelectedTimeRange(dates)}
                    ranges={{
                        Today: [moment().startOf('day'), moment().endOf('day')],
                        'This Week': [moment().startOf('week'), moment().endOf('week')],
                        'This Month': [moment().startOf('month'), moment().endOf('month')],
                    }}
                    />
            </div>
            <div style={styles.filterColumnEntry}>
                <Row>
                    <Col span={24} style={{textAlign: 'right'}}>
                        <Button icon={<SyncOutlined />} type="primary" onClick={e => {
                            const layerIDParams = layerIDs.map(layerID => `layerIDs=${encodeURIComponent(layerID)}`).join('&');
                            const timeParams = `from=${selectedTimeRange[0].toISOString()}&to=${selectedTimeRange[1].toISOString()}`;
                            const params = [layerIDParams, timeParams].join('&');
                            const backendURL = `${env('BACKEND_URL')}/api/v1/GraphvizDot/layerCentric?${params}`;
                            const token = localStorage.getItem('token');
                            setGraphDefinitionLoading(true);
                            fetch(backendURL, {
                                headers: new Headers({
                                    'Authorization': `Bearer ${token}`, 
                                })
                            })
                                .then(response => {
                                    if (response.status != 200) {
                                        throw new Exception(`Loading graph definition failed with status code ${response.status}: ${response.text()}`);
                                    } else {
                                        return response.text();
                                    }
                                })
                                .then(data => setGraphDefinition(data))
                                .catch(e => console.log(e))
                                .finally(() => setGraphDefinitionLoading(false));
                        }} size="large">Render</Button>
                    </Col>
                </Row>
            </div>
        </div>
        <div style={styles.resultsColumn}>
            <Spin spinning={graphDefinitionLoading} size="large" tip="Loading...">
                {graphDefinition ? 
                    <Graph graphDefinition={graphDefinition} /> : 
                    <div style={{ display: "flex", justifyContent: "center", alignItems: "center", flexGrow: "1"}}>
                        <Text type="secondary">Use the bar on the left to render a graph...</Text>
                    </div>}
            </Spin>
        </div>
    </div>;
}

const styles = {
    container: {
        display: "flex",
        flexDirection: "row",
        height: "100%",
    },
    // left column
    filterColumn: {
        display: "flex",
        flexDirection: "column",
        margin: "10px",
        overflowY: "hidden",
        width: "360px",
        minWidth: "300px",
    },
    filterColumnEntry: {
        marginBottom: "10px",
    },

    // right column - results
    resultsColumn: {
        display: "flex",
        flexDirection: "column",
        margin: "10px",
        flex: "1 1 auto",
    },
};
