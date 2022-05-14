import React, { useState } from "react";
import env from "@beam-australia/react-env";
import { useExplorerLayers } from "utils/layers";
import { Button, Spin, Row, Col } from 'antd';
import { SyncOutlined } from '@ant-design/icons';
import Paragraph from "antd/lib/typography/Paragraph";
import { useQuery } from "@apollo/client";
import { queries } from "graphql/queries";
import TraitList from "./TraitList.js";
import Text from "antd/lib/typography/Text";
import _ from 'lodash';
import Graph from "./Graph.js";

export default function TraitCentricGraphRendering(props) {
    
    const [graphDefinition, setGraphDefinition] = useState(null);
    const [graphDefinitionLoading, setGraphDefinitionLoading] = useState(null);

    const { data: visibleLayers } = useExplorerLayers(true);
    const layerIDs = visibleLayers.map(l => l.id);

    const { data: activeTraits } = useQuery(queries.ActiveTraits);
    var [checkedTraits, setCheckedTraits] = useState([]);
    
    return <div style={styles.container}>
        <div style={styles.filterColumn}>
            <h2>Trait-Centric Graph Rendering</h2>
            <div style={styles.filterColumnEntry}>
                <Paragraph>
                    Visualizes CIs that fulfill the selected traits as nodes in a graph, and relations with predicates as edges between them. 
                    The numbers in brackets indicate the amount of CIs and the amount of relations.
                    Nodes and edges are colorized according to the layer they belong to.
                </Paragraph>
                <Paragraph>
                    Note: only data from visible layers will be shown. Make sure to mark the desired layers as visible in the layer side-bar.
                </Paragraph>
            </div>
            <div style={{marginBottom: "10px", overflow: "hidden", flexGrow: 1, display: 'flex'}}>
                {activeTraits && <TraitList traitList={activeTraits.activeTraits} checked={checkedTraits} onCheck={setCheckedTraits} />}
            </div>
            <div style={styles.filterColumnEntry}>
                <Row>
                    <Col span={24} style={{textAlign: 'right'}}>
                        <Button icon={<SyncOutlined />} type="primary" onClick={e => {
                            const layerIDParams = layerIDs.map(layerID => `layerIDs=${encodeURIComponent(layerID)}`).join('&');

                            // NOTE: checkedTraits also contains groups, not only leaf traits, so we filter the list by the active trait list
                            const allValidTraitIDs = activeTraits.activeTraits.map(t => t.id);
                            const selectedValidTraitIDs = checkedTraits.filter(t => allValidTraitIDs.includes(t));

                            const traitParams = selectedValidTraitIDs.map(traitID => `traitIDs=${encodeURIComponent(traitID)}`).join('&');
                            const params = [layerIDParams, traitParams].join('&');
                            const backendURL = `${env('BACKEND_URL')}/api/v1/GraphvizDot/traitCentric?${params}`;
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
