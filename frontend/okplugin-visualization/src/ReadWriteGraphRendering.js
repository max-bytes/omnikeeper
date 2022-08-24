import React, { useEffect, useState } from "react";
import { Button, Spin, Row, Col, Space } from 'antd';
import { SyncOutlined } from '@ant-design/icons';
import { useLazyQuery } from "@apollo/client";
import _ from 'lodash';
import cytoscape from 'cytoscape';
import dagre from 'cytoscape-dagre';
import gql from 'graphql-tag';
import { toHtml, icon } from "@fortawesome/fontawesome-svg-core";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCogs, faUser, faTableCells } from "@fortawesome/free-solid-svg-icons";
import Paragraph from "antd/lib/typography/Paragraph";

// TODO: refactor to own file, avoid duplication
function argbToRGB(color) {
    return '#'+ ('000000' + (color & 0xFFFFFF).toString(16)).slice(-6);
  }

const getSVGURI = (faIcon, color) => {
  const abstract = icon(faIcon).abstract[0];
  // HACK: we force the svg's dimensions, otherwise rendering breaks when zooming
  abstract.attributes.width = '60px';
  abstract.attributes.height = '60px';
  if (color) abstract.children[0].attributes.fill = color;
  return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(toHtml(abstract))}`;
};

export default function ReadWriteGraphRendering(props) {
    
    const [graphDefinitionLoading, setGraphDefinitionLoading] = useState(null);

    const [fetchAuthRoles, {}] = useLazyQuery(queryAuthRoles);
    const [fetchLayers, {}] = useLazyQuery(queryLayers);
    const [fetchODataContexts, {}] = useLazyQuery(queryODataContexts);
    
    var [cy, setCY] = useState(null);

    const baseLayoutOptions = {
            name: 'dagre',
            animate: true,
            animationEasing: 'ease-out-cubic',
            nodeDimensionsIncludeLabels: true,
            uniformNodeDimensions: false,
            packComponents: true,
            idealEdgeLength: edge => 300,
            nodeRepulsion: node => 1,
            rankDir: 'LR',
            rankSep: 200
          };

    const [layoutOptions, setLayoutOptions] = useState(baseLayoutOptions);

    // TODO: should be remountable
    useEffect(() => {
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
                        'background-color': '#fff',
                        'label': (node) => node.data('label'),
                    }
                },
                {
                    selector: 'edge',
                    style: {
                        'target-arrow-shape': 'triangle',
                        'curve-style': 'taxi',
                        'taxi-direction': 'horizontal',
                        'source-label': 'data(label)',
                        'source-text-offset': '100px',
                        'text-background-color': '#fff',
                        'text-background-opacity': '0.8',
                        'text-halign': 'left'
                    }
                },
                {
                    selector: '.layer',
                    style: {
                        'text-halign': 'center',
                        'text-valign': 'center',
                        'border-width': '3px',
                        'width': '60px',
                        'height': '60px',
                        'text-margin-y': '10px',
                        'background-color': (node) => node.data('color'),
                        'border-color': '#333333',
                        'text-valign': 'bottom',
                    }
                },
                {
                    selector: '.auth-role',
                    style: {
                        'background-image': `url(${getSVGURI(faUser, '#333333')})`,
                    }
                },
                {
                    selector: '.clb',
                    style: {
                        'background-image': `url(${getSVGURI(faCogs, '#333333')})`,
                    }
                },
                {
                    selector: '.odata-context',
                    style: {
                        'background-image': `url(${getSVGURI(faTableCells, '#333333')})`,
                    }
                },
                {
                    selector: '.iconized',
                    style: {
                        'background-fit': 'cover',
                        'border-width': '0px',
                        'width': 70,
                        'height': 70,
                        'text-valign': 'bottom',
                        'background-position-x': '50%',
                        'background-position-y': '50%',
                        'background-offset-x': '-50%',
                        'background-offset-y': '-50%',
                        'text-margin-y': '10px',
                    }
                }
            ],
            isHeadless: false,
          });
          setCY(cy);
    }, [setCY]);

    const canRender = true;
    
    return <div style={styles.container}>
        <div style={styles.filterColumn}>
            <h2>Read-Write Graph Rendering</h2>
            <div style={styles.filterColumnEntry}>
                <Paragraph>
                    Note: management permission required
                </Paragraph>
            </div>
            <div style={styles.filterColumnEntry}>
                <Paragraph>
                    Legend: 
                    <ul style={{"listStyle": "none"}}>
                        <li><FontAwesomeIcon fixedWidth icon={faCogs}/> Compute Layer</li>
                        <li><FontAwesomeIcon fixedWidth icon={faUser}/> Auth-Role</li>
                        <li><FontAwesomeIcon fixedWidth icon={faTableCells}/> OData Context</li>
                    </ul>
                </Paragraph>
            </div>
            <div style={styles.filterColumnEntry}>
                <Row>
                    <Col span={24} style={{textAlign: 'right'}}>
                        <Button icon={<SyncOutlined />} type="primary" disabled={!canRender} onClick={e => {
                            setGraphDefinitionLoading(true);

                            const authRolesPromise = fetchAuthRoles();
                            const layersPromise = fetchLayers();
                            const odataContextPromise = fetchODataContexts();

                            Promise.all([authRolesPromise, layersPromise, odataContextPromise]).then(results => {
                                const [
                                    {data: authRoles, error: errorAuthRoles}, 
                                    {data: layers, error: errorLayers},
                                    {data: odataContexts, error: errorODataContexts},
                                ] = results;

                                if (errorAuthRoles) {
                                    console.log(errorAuthRoles); // TODO
                                    return;
                                }
                                if (errorLayers) {
                                    console.log(errorLayers); // TODO
                                    return;
                                }
                                if (errorODataContexts) {
                                    console.log(errorODataContexts); // TODO
                                    return;
                                }
                                const { elements, alignmentConstraints } = data2elements(authRoles.manage_authRoles, layers.manage_layers, odataContexts.manage_odataapicontexts);

                                cy.json({ 
                                    elements: elements
                                });

                                const newLayoutOptions = {
                                    ...baseLayoutOptions,
                                    alignmentConstraint: {vertical: alignmentConstraints}
                                };
                                setLayoutOptions(newLayoutOptions);
                                // NOTE: do not animate first run, so it's run syncronously and we can call cy.fit() right afterwards
                                cy.layout({...newLayoutOptions, animate: false}).run(); 
                                cy.fit();
                            })
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

function createLayerNodeID(layerID) { return `layer_${layerID}` };
function createODataContextNodeID(id) { return `odata_context_${id}` };
function createCLBNodeID(clConfigID, layerID) { return `clb_${clConfigID}@${layerID}` };
function createAuthRoleNodeID(authRole) { return `auth_role_${authRole.id}` };

function data2elements(authRoles, layers, odataContexts) {

    const layerNodes = _.map(layers, layer => {
        return {
            data: {
                id: createLayerNodeID(layer.id),
                label: `${layer.id}`,
                color: argbToRGB(layer.color)
            },
            classes: ['layer']
        };
    });

    const layerAlignmentConstraints = [];//[_.map(layers, layer => createLayerNodeID(layer.id))];

    const authRoleNodes = _.map(authRoles, authRole => {
        return {
            data: {
                id: createAuthRoleNodeID(authRole),
                label: `${authRole.id}`
            },
            classes: ['auth-role', 'iconized']
        };
    });

    const authRole2layerEdgesRead = _.flatten(_.map(authRoles, authRole => {
        const filteredReadLayers = _.filter(authRole.grantsReadAccessForLayers, readLayer => !_.some(authRole.grantsWriteAccessForLayers, writeLayer => writeLayer.id == readLayer.id));
        return _.map(filteredReadLayers, l => {
            return {
                data: {
                    id: `edge_${createLayerNodeID(l.id)}->can_be_read_from->${createAuthRoleNodeID(authRole)}`,
                    label: "can be read from",
                    source: createLayerNodeID(l.id),
                    target: createAuthRoleNodeID(authRole),
                }
            }
        });
    }));
    const authRole2layerEdgesWrite = _.flatten(_.map(authRoles, authRole => {
        return _.map(authRole.grantsWriteAccessForLayers, l => {
            return {
                data: {
                    id: `edge_${authRole.id}->can_write_to->${l.id}`,
                    label: "can write to",
                    source: createAuthRoleNodeID(authRole),
                    target: createLayerNodeID(l.id),
                }
            }
        });
    }));
    
    const layersWithActiveCLBs = _.filter(layers, layer => layer.clConfig);
    const clbNodes = _.map(layersWithActiveCLBs, layer => {
        return {
            data: {
                id: createCLBNodeID(layer.clConfig.id, layer.id),
                label: `${layer.clConfig.id}`
            },
            classes: ['clb', 'iconized']
        };
    });
    const clb2layerEdgesRead = _.flatten(_.map(layersWithActiveCLBs, layer => {
        return _.map(layer.clConfig.dependentLayers, dependantLayer => {
            return {
                data: {
                    id: `edge_${createLayerNodeID(dependantLayer.id)}->can_be_read_from->${createCLBNodeID(layer.clConfig.id, layer.id)}`,
                    label: "can be read from",
                    source: createLayerNodeID(dependantLayer.id),
                    target: createCLBNodeID(layer.clConfig.id, layer.id),
                }
            }
        });
    }));
    const clb2layerEdgesWrite = _.flatten(_.map(layersWithActiveCLBs, layer => {
        return {
            data: {
                id: `edge_${createCLBNodeID(layer.clConfig.id, layer.id)}->can_write_to->${createLayerNodeID(layer.id)}`,
                label: "can write to",
                source: createCLBNodeID(layer.clConfig.id, layer.id),
                target: createLayerNodeID(layer.id),
            }
        }
    }));
    
    const odataContextNodes = _.map(odataContexts, odataContext => {
        return {
            data: {
                id: createODataContextNodeID(odataContext.id),
                label: `${odataContext.id}`
            },
            classes: ['odata-context', 'iconized']
        };
    });
    const odataContext2layerEdgesRead = _.flatten(_.map(odataContexts, odataContext => {
        return _.map(odataContext.readLayers, layer => {
            return {
                data: {
                    id: `edge_${createLayerNodeID(layer.id)}->can_be_read_from->${createODataContextNodeID(odataContext.id)}`,
                    label: "can be read from",
                    source: createLayerNodeID(layer.id),
                    target: createODataContextNodeID(odataContext.id),
                }
            }
        });
    }));

    const elements = _.flatten([
        layerNodes, authRoleNodes, authRole2layerEdgesRead, authRole2layerEdgesWrite, 
        clbNodes, clb2layerEdgesRead, clb2layerEdgesWrite,
        odataContextNodes, odataContext2layerEdgesRead]);
    return { elements, alignmentConstraints: layerAlignmentConstraints };
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

const queryAuthRoles = gql`
query {
    manage_authRoles {
      id
      grantsReadAccessForLayers {
        id
      }
      grantsWriteAccessForLayers {
        id
      }
    }
  }
`;

const queryLayers = gql`
query {
    manage_layers {
      id
      color
      clConfig {
        id
        dependentLayers {
          id
        }
      }
    }
  }
`;

const queryODataContexts = gql`
query {
    manage_odataapicontexts {
      id
      readLayers {
        id
      }
    }
  }
`;