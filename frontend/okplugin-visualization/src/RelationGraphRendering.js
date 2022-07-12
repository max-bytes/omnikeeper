import React, { useEffect, useState } from "react";
import { useExplorerLayers } from "utils/layers";
import { Button, Spin, Row, Col, Space, InputNumber } from 'antd';
import { SyncOutlined } from '@ant-design/icons';
import { useLazyQuery } from "@apollo/client";
import _ from 'lodash';
import cytoscape from 'cytoscape';
import dagre from 'cytoscape-dagre';
import layoutUtilities from 'cytoscape-layout-utilities';
import { calculateNodeWidth, calculateNodeHeight } from './cytoscape_utils';
import gql from 'graphql-tag';
import SingleCISelect from "components/SingleCISelect";

export default function RelationGraphRendering(props) {
    
    const [graphDefinitionLoading, setGraphDefinitionLoading] = useState(null);

    const { data: visibleLayers } = useExplorerLayers(true);
    const layerIDs = visibleLayers.map(l => l.id);

    const [fetch, {}] = useLazyQuery(query);

    var [cy, setCY] = useState(null);

    const baseLayoutOptions = {
        nodeSep: 50,
        rankSep: 200,
        animate: true,
        fit: false,
        rankDir: 'LR',
        name: "dagre",
        padding: 20,
        spacingFactor: 1
      };
      
    // const baseLayoutOptions = {
    //     name: 'fcose',
    //     quality: "proof",
    //     animate: true,
    //     animationEasing: 'ease-out-cubic',
    //     nodeDimensionsIncludeLabels: true,
    //     idealEdgeLength: edge => 300,
    //   };

    const [layoutOptions, setLayoutOptions] = useState(baseLayoutOptions);

    const [baseCIID, setBaseCIID] = useState(null);
    const [edgeLimit, setEdgeLimit] = useState(10);
    const [depth, setDepth] = useState(1);
    
    // TODO: should be remountable
    useEffect(() => {
        cytoscape.use( layoutUtilities );
        cytoscape.use( dagre );
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
                        'background-color': '#fff',
                        'border-width': '3px',
                        'label': (node) => node.data('label'),// + "\n" + node.data('id'),
                        'text-halign': 'center',
                        'text-valign': 'center',
                        'text-wrap': 'wrap'
                    }
                },
                {
                    selector: 'edge',
                    style: {
                        'target-arrow-shape': 'triangle',
                        'curve-style': 'taxi',
                        'taxi-direction': 'horizontal',
                        'taxi-turn': '-150px',
                        'label': 'data(label)',
                        'text-background-color': '#fff',
                        'text-background-opacity': '0.8',
                        'text-wrap': 'wrap'
                    }
                }
            ],
            isHeadless: false,
          });
          setCY(cy);
    }, [setCY]);

    const canRender = !!baseCIID && !!layerIDs;
    
    return <div style={styles.container}>
        <div style={styles.filterColumn}>
            <h2>Relation Graph Rendering</h2>
            <div style={styles.filterColumnEntry}>
                <h4>Base-CI</h4>
                <SingleCISelect
                    layers={layerIDs} 
                    selectedCIID={baseCIID} 
                    setSelectedCIID={(ciid) => setBaseCIID(ciid)} 
                />
            </div>
            <div style={styles.filterColumnEntry}>
                <h4>Edge-Limit</h4>
                <h5>(maximum number of edges per node and predicate)</h5>
                <InputNumber min={1} onChange={setEdgeLimit} value={edgeLimit} />
            </div>
            <div style={styles.filterColumnEntry}>
                <h4>Depth</h4>
                <InputNumber min={1} max={2} onChange={setDepth} value={depth} />
            </div>
            <div style={styles.filterColumnEntry}>
                <Row>
                    <Col span={24} style={{textAlign: 'right'}}>
                        <Button icon={<SyncOutlined />} type="primary" disabled={!canRender} onClick={e => {
                            setGraphDefinitionLoading(true);

                            fetch({
                                variables:{ 
                                    layers: layerIDs, 
                                    ciid: baseCIID,
                                    n1: depth > 1
                              }}).then(({data, error}) => {
                                if (!data) {
                                  console.log(error); // TODO
                                  return;
                                }

                                const baseCI = data.cis[0];
                                const {elements, relativeConstraints, alignmentConstraints} = ci2elements(baseCI, true, [], null, edgeLimit);
                                cy.json({ 
                                    elements: elements
                                });

                                setLayoutOptions({...baseLayoutOptions, 
                                    relativePlacementConstraint: relativeConstraints,
                                    alignmentConstraint: {vertical: alignmentConstraints}
                                });
                                // NOTE: do not animate first run, so it's run syncronously and we can call cy.fit() right afterwards
                                cy.layout({...layoutOptions, animate: false}).run(); 
                                cy.fit();
                              }).finally(() => setGraphDefinitionLoading(false));

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

function createNodeID(ciid, path) { return `${ciid}_${_.join(path, '_')}` };
function createEdgeID(ciid, outgoing, predicateID) { return `edge_id_${(outgoing) ? 'outgoing' : 'incoming'}_${predicateID}_${ciid}` };
function createGroupID(ciid, outgoing, predicateID) { return `group_id_${(outgoing) ? 'outgoing' : 'incoming'}_${predicateID}_${ciid}` };

function ci2elements(baseCI, includeBaseCI, path, skipRelationID, edgeLimit) {
    const baseElement = (includeBaseCI) ? {
        data: {
            id: createNodeID(baseCI.id, path),
            label: baseCI.name
        }
    } : [];

    const unsortedOutgoing = _.flatMap(
        _.groupBy(
            _.filter(baseCI.outgoingMergedRelations, r => r.relation.id !== skipRelationID), 
            r => r.relation.predicateID),
        g => _.take(g, edgeLimit));
    const outgoing = _.sortBy(unsortedOutgoing, [r => r.relation.predicateID, r => r.relation.toCIName]);

    const unsortedIncoming = _.flatMap(
        _.groupBy(
            _.filter(baseCI.incomingMergedRelations, r => r.relation.id !== skipRelationID), 
            r => r.relation.predicateID),
        g => _.take(g, edgeLimit));
    const incoming = _.sortBy(unsortedIncoming, [r => r.relation.predicateID, r => r.relation.fromCIName]);

    // outgoing
    const outgoingGroups = _.map(_.groupBy(outgoing, r => r.relation.predicateID), (value, predicateID) => {
        return {data: {id: createGroupID(baseCI.id, true, predicateID), label: ''}};
    });
    const outgoingElements = _.map(outgoing, r => {
        return {
            data: {
                id: createNodeID(r.relation.toCIID, [...path, r.relation.id]),
                parent: createGroupID(baseCI.id, true, r.relation.predicateID),
                label: r.relation.toCIName
            }
        }
    });
    // TODO: re-use grouped relations by predicate
    const outgoingRelations = _.map(_.groupBy(outgoing, r => r.relation.predicateID), (value, predicateID) => {
        return {
            data: {
                id: createEdgeID(baseCI.id, true, predicateID),// createID(predicateID, path), // TODO, HACK: weird
                label: predicateID,
                source: createNodeID(baseCI.id, path),
                target: createGroupID(baseCI.id, true, predicateID)// createID(r.relation.toCIID, [...path, r.relation.id])
            }
        }
    });
    // TODO: re-use grouped relations by predicate
    const outgoingRelativeConstraints = _.map(_.groupBy(outgoing, r => r.relation.predicateID), (value, predicateID) => {
        return {left: createNodeID(baseCI.id, path), right: createGroupID(baseCI.id, true, predicateID)}
    });
    const outgoingAlignmentConstraints = _.map(_.groupBy(outgoing, r => r.relation.predicateID), (value, predicateID) => {
        return createGroupID(baseCI.id, true, predicateID);
    });

    const innerOutgoingCIs = _.map(outgoing, r => {
        if (r.relation.toCI)
            return ci2elements(r.relation.toCI, false, [...path, r.relation.id], r.relation.id, edgeLimit);
        return {elements: [], relativeConstraints: [], alignmentConstraints: []};
    });

    // incoming
    const incomingElements = _.map(incoming, r => {
        return {
            data: {
                id: createNodeID(r.relation.fromCIID, [...path, r.relation.id]),
                label: r.relation.fromCIName
            }
        }
    });
    const incomingRelations = _.map(incoming, r => {
        return {
            data: {
                id: createNodeID(r.relation.id, path),
                label: r.relation.predicateID,
                source: createNodeID(r.relation.fromCIID, [...path, r.relation.id]),
                target: createNodeID(baseCI.id, path)
            }
        }
    });
    const incomingRelativeConstraints = _.map(incoming, r => {
        return {left: createNodeID(r.relation.fromCIID, [...path, r.relation.id]), right: createNodeID(baseCI.id, path)}
    });
    const incomingAlignmentConstraints = _.map(incoming, r => createNodeID(r.relation.fromCIID, [...path, r.relation.id]));

    const innerIncomingCIs = _.map(incoming, r => {
        if (r.relation.fromCI)
            return ci2elements(r.relation.fromCI, false, [...path, r.relation.id], r.relation.id, edgeLimit);
        return {elements: [], relativeConstraints: [], alignmentConstraints: []};
    });

    // joining it all together
    const elements = _.flattenDeep([baseElement, 
        // incomingElements, 
        outgoingElements,
        outgoingGroups, 
        // incomingRelations, 
        outgoingRelations,
        _.map(innerIncomingCIs, t => t.elements), _.map(innerOutgoingCIs, t => t.elements)
    ]);

    const relativeConstraints = _.flattenDeep([
        outgoingRelativeConstraints, incomingRelativeConstraints,
        _.map(innerIncomingCIs, t => t.relativeConstraints), _.map(innerOutgoingCIs, t => t.relativeConstraints)
    ]);
    const innerOutgoingAlignmentConstraints = _.flatten(_.map(innerOutgoingCIs, t => t.alignmentConstraints));
    const innerIncomingAlignmentConstraints = _.flatten(_.map(innerIncomingCIs, t => t.alignmentConstraints));
    const alignmentConstraints = _.concat(
        [outgoingAlignmentConstraints, incomingAlignmentConstraints], 
        innerIncomingAlignmentConstraints, innerOutgoingAlignmentConstraints);

    return { elements, relativeConstraints, alignmentConstraints };
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

const baseCIFragment = gql`
fragment BaseCI on MergedCIType {
    id
    name
    outgoingMergedRelations {
        relation {
            id
            predicateID
            toCIID
            toCIName
        }
    }
    incomingMergedRelations {
        relation {
            id
            predicateID
            fromCIID
            fromCIName
        }
    }
}
`;
const query = gql`
${baseCIFragment}
query($ciid: Guid!, $layers: [String]!, $n1: Boolean!) {
    cis(ciids: [$ciid], layers: $layers) {
        ...BaseCI
        outgoingMergedRelations @include(if: $n1) {
            relation {
                toCI {
                    ...BaseCI
                }
            }
        }
        incomingMergedRelations @include(if: $n1) {
            relation {
                fromCI {
                    ...BaseCI
                }
            }
        }
    }
}
`;
