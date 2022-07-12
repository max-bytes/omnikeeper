import React, { useEffect, useState } from "react";
import env from "@beam-australia/react-env";
import { useExplorerLayers } from "utils/layers";
import { Button, Spin, Row, Col, Space } from 'antd';
import Paragraph from "antd/lib/typography/Paragraph";
import { SyncOutlined } from '@ant-design/icons';
import { useQuery } from "@apollo/client";
import { queries } from "graphql/queries";
import TraitList from "./TraitList.js";
import _ from 'lodash';
import cytoscape from 'cytoscape';
import fcose from 'cytoscape-fcose';
import layoutUtilities from 'cytoscape-layout-utilities';
import { calculateNodeWidth, calculateNodeHeight } from './cytoscape_utils';

export default function TraitCentricGraphRenderingCytoscape(props) {
    
    const [graphDefinitionLoading, setGraphDefinitionLoading] = useState(null);

    const { data: visibleLayers } = useExplorerLayers(true);
    const layerIDs = visibleLayers.map(l => l.id);

    const { data: activeTraits } = useQuery(queries.ActiveTraits);
    var [checkedTraits, setCheckedTraits] = useState([]);

    var [cy, setCY] = useState(null);

    const layoutOptions = {
        name: 'fcose',
        quality: "proof",
        animate: true,
        animationEasing: 'ease-out-cubic',
        nodeDimensionsIncludeLabels: true,
        idealEdgeLength: edge => 300,
      };
    
    // TODO: should be remountable
    useEffect(() => {
        cytoscape.use( layoutUtilities );
        cytoscape.use( fcose );
    }, []);

    useEffect(() => {
        var cy = cytoscape({
            container: document.getElementById('graph'), // TODO: use ref
            layout: {},
            style: [ // the stylesheet for the graph
                {
                selector: 'node',
                style: {
                    'shape': 'rectangle',
                    'width': calculateNodeWidth,
                    'height': calculateNodeHeight,
                    'background-fill': 'linear-gradient',
                    'background-gradient-stop-colors': calculateBackgroundGradientStopColors, // 'white white red red green green blue blue white',
                    'background-gradient-stop-positions': calculateBackgroundGradientStopPositions,//'0px 3px 3px 9px 9px 15px 15px 21px 21px',
                    'border-width': '3px',
                    'border-color': (node) => node.data('colors')[0],
                    'label': 'data(label)',
                    'text-halign': 'center',
                    'text-valign': 'center',
                    'text-wrap': 'wrap'
                }
                },

                {
                selector: 'edge',
                style: {
                    'line-color': (node) => node.data('colors')[0],
                    'target-arrow-color': (node) => node.data('colors')[0],
                    'target-arrow-shape': 'triangle',
                    'curve-style': 'bezier',
                    'label': 'data(label)',
                    'text-background-color': '#fff',
                    'text-background-opacity': '0.8',
                    'text-wrap': 'wrap',
                    'loop-direction': '-10deg',
                    'loop-sweep': '20deg',
                }
                }
            ],
            isHeadless: false,
            // wheelSensitivity: 0.5 // HACK, but default is way too big
          });
        //   cy.ready(() => {
        //     console.log('ready');
        //   });
        //   cy.on('layoutstart', () => {
        //     console.log('layoutstart');
        //   });
          setCY(cy);
          
    }, [setCY]);
    
    return <div style={styles.container}>
        <div style={styles.filterColumn}>
            <h2>Trait-Centric Graph Rendering (Cytoscape)</h2>
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
                            const backendURL = `${env('BACKEND_URL')}/api/v1/Cytoscape/traitCentric?${params}`;
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
                                    return response.json();
                                }
                            })
                            .then(elements => {
                                cy.json({ 
                                    elements: elements
                                 });
                                 cy.layout(layoutOptions).run();
                            })
                            .catch(e => console.log(e))
                            .finally(() => setGraphDefinitionLoading(false));
                        }} size="large">Render</Button>
                    </Col>
                </Row>
            </div>
        </div>
        <div style={styles.resultsColumn}>
            <Spin spinning={graphDefinitionLoading} size="large" tip="Loading...">
                <div style={{display: 'flex', justifyContent: 'end'}}>
                    <Space>
                        <Button onClick={() => cy.fit()}>scale to fit</Button>
                        <Button onClick={() => cy.layout(layoutOptions).run()}>re-layout</Button>
                    </Space>
                </div>
                <div id="graph" style={{height: '100%'}}></div>
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

function calculateBackgroundGradientStopColors(node) {
    const layerColors = node.data('colors');
    const inners = _.flatMap(layerColors, color => [color, color]);
    const ret = _.join(_.flatten([['white'], inners, ['white']]), ' ');
    // console.log(ret);
    return ret;
}
function calculateBackgroundGradientStopPositions(node) {
    const layerColors = node.data('colors');
    const positionOffset = 3 - 0.01; // equal to border
    const layerThickness = 6;
    const inners = _.flatMap(layerColors, (color, i) => [positionOffset + i * layerThickness, positionOffset + i * layerThickness + layerThickness]);
    const ret = _.join(_.flatten([[positionOffset], inners, [positionOffset + layerColors.length * layerThickness]]), ' ');
    // console.log(ret);
    return ret;
}