import { useLazyQuery } from "@apollo/client";
import React, {useCallback, useEffect, useState} from "react";
import { Spin, DatePicker, Button, Row, Col } from 'antd';
import _ from 'lodash';
import { queries } from "../../graphql/queries";
import { AgGridReact } from "ag-grid-react";
import UserTypeIcon from './../UserTypeIcon';
import { formatTimestamp } from "utils/datetime";
import LayerIcon from "components/LayerIcon";
import moment from 'moment';
import { useExplorerLayers } from "../../utils/layers";
import { SyncOutlined } from '@ant-design/icons';
import { ChangesetID } from "utils/uuidRenderers";
import Text from "antd/lib/typography/Text";
import { Link } from "react-router-dom";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faLink } from '@fortawesome/free-solid-svg-icons';

const { RangePicker } = DatePicker;

export default function ChangesetList(props) {
    
    const { data: visibleLayers, loading: loadingLayers } = useExplorerLayers(true);

    const [search, { loading: loadingChangesets, data: dataChangesets }] = useLazyQuery(queries.Changesets, {
        notifyOnNetworkStatusChange: true
    });

    // debounce search, so its not called too often
    const debouncedSearch = useCallback(_.debounce(search, 500), [search]);

    const [selectedTimeRange, setSelectedTimeRange] = useState([moment().startOf('day'), moment().endOf('day')]);

    const [refreshNonce, setRefreshNonce] = useState(null);

    useEffect(() => {
        if (visibleLayers && selectedTimeRange) {
            debouncedSearch({
                variables: {
                    from: selectedTimeRange[0].toDate(),
                    to: selectedTimeRange[1].toDate(),
                    layers: visibleLayers.map((l) => l.id),
                }
            });
        }
    }, [debouncedSearch, selectedTimeRange, visibleLayers, refreshNonce]);


    const loading = loadingLayers;
    
    const layerCellRenderer = function(params) {
        const layer = params.value;
        return <span><LayerIcon color={layer.color} /> {layer.id}</span>;
    };

    const userCellRenderer = function(params) {
        const user = params.value;
        return <span><UserTypeIcon userType={user.type} /> {user.displayName}</span>;
    };
    
    const timestampCellRenderer = function(params) {
        return formatTimestamp(params.value);
    }
    
    const changesetIDCellRenderer = function(params) {
        return <ChangesetID id={params.value} link={true} />;
    }
    
    const changesetDataCellRenderer = function(params) {
        const dataCIID = params.value;
        if (dataCIID)
            return <span><Link to={"/explorer/" + params.value}><FontAwesomeIcon icon={faLink}/> Data-CI</Link></span>;
        else
            return <Text type="secondary">None</Text>;
    }
    
    const statisticsCellRenderer = function(params) {
        const numAttributeChanges = params.value.numAttributeChanges;
        const numRelationChanges = params.value.numRelationChanges;
        const tokens = [
            `${numAttributeChanges} ${numAttributeChanges === 1 ? 'attribute' : 'attributes'}`,
            `${numRelationChanges} ${numRelationChanges === 1 ? 'relation' : 'relations'}`
        ]
        return tokens.filter(t => t).join(', ');
    }

    const columnDefs = [
        {
            headerName: "Timestamp",
            field: "timestamp",
            sortable: true,
            cellRenderer: "timestampCellRenderer",
            width: 140,
            sort: "desc"
        },
        {
            headerName: "Layer",
            field: "layer",
            sortable: true,
            cellRenderer: "layerCellRenderer",
            resizable: true,
            flex: 2,
            comparator: (valueA, valueB, nodeA, nodeB, isInverted) => {
                const va = valueA.id;
                const vb = valueB.id;
                if (!va) return -1;
                if (!vb) return 1;
                const r = va.localeCompare(vb);
                return r;
            },
            filter: true,
            filterParams: {
                valueGetter: (params) => {
                    return params.data.layer.id;
                }
            }
        },
        {
            headerName: "User",
            field: "user",
            cellRenderer: "userCellRenderer",
            comparator: (valueA, valueB, nodeA, nodeB, isInverted) => {
                const va = valueA.displayName;
                const vb = valueB.displayName;
                if (!va) return -1;
                if (!vb) return 1;
                const r = va.localeCompare(vb);
                return r;
            },
            sortable: true,
            resizable: true,
            flex: 3,
            filter: true,
            filterParams: {
                valueGetter: (params) => {
                    return params.data.user.displayName;
                }
            }
        },
        {
            headerName: "Changes",
            field: "statistics",
            width: 160,
            cellRenderer: "statisticsCellRenderer",
        },
        {
            headerName: "Changeset-ID",
            field: "id",
            width: 280,
            filter: true,
            cellRenderer: "changesetIDCellRenderer",
        },
        {
            headerName: "Data-CI",
            field: "dataCIID",
            width: 90,
            filter: true,
            cellRenderer: "changesetDataCellRenderer",
        },
    ];

    return <div style={styles.container}>
            <Spin
                spinning={loading}>
                {/* left column - search */}
                <div style={styles.filterColumn}>
                    <h2>Changesets</h2>
                    <div style={styles.filterColumnEntry}>
                        <h4>Time-Range</h4>
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
                        <Text>Note: only changesets from visible layers will be shown. Make sure to mark the desired layers as visible in the layer side-bar.</Text>
                    </div>
                    <div style={styles.filterColumnEntry}>
                        <Row>
                            <Col span={24} style={{textAlign: 'right'}}>
                                <Button icon={<SyncOutlined />} type="primary" loading={loadingChangesets} onClick={() => setRefreshNonce(moment().toISOString())}>Refresh</Button>
                            </Col>
                        </Row>
                    </div>
                </div>
                {/* right column - results */}
                <div style={styles.resultsColumn}>
                    {dataChangesets?.changesets && 
                        <div style={{height:'100%'}} className={"ag-theme-balham"}>
                            <AgGridReact
                                components={{
                                    statisticsCellRenderer: statisticsCellRenderer,
                                    userCellRenderer: userCellRenderer,
                                    timestampCellRenderer: timestampCellRenderer,
                                    layerCellRenderer: layerCellRenderer,
                                    changesetIDCellRenderer: changesetIDCellRenderer,
                                    changesetDataCellRenderer: changesetDataCellRenderer
                                }}
                                rowData={dataChangesets.changesets}
                                columnDefs={columnDefs}
                                animateRows={true}
                                getRowId={function (params) {
                                    return params.data.id;
                                }}
                            />
                        </div>
                    }
                </div>
            </Spin>
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
        overflowY: "auto",
        width: "360px",
        minWidth: "300px",
    },
    filterColumnEntry: {
        marginBottom: "20px",
    },

    // right column - results
    resultsColumn: {
        display: "flex",
        flexDirection: "column",
        margin: "10px",
        flex: "1 1 auto",
    },
};
