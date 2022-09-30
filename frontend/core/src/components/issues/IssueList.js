import { useLazyQuery } from "@apollo/client";
import React, {useCallback, useEffect, useState} from "react";
import { Button } from 'antd';
import _ from 'lodash';
import { queries } from "../../graphql/queries";
import { AgGridReact } from "ag-grid-react";
import moment from 'moment';
import { SyncOutlined } from '@ant-design/icons';
import { CIID } from "utils/uuidRenderers";
import { useAGGridEnterprise } from 'utils/useAGGridEnterprise';
import { formatTimestamp } from "utils/datetime";


export default function IssueList(props) {
    
    const [search, { loading: loadingIssues, data: dataIssues }] = useLazyQuery(queries.Issues, {
        notifyOnNetworkStatusChange: true
    });

    // debounce search, so its not called too often
    const debouncedSearch = useCallback(_.debounce(search, 500), [search]);

    const [refreshNonce, setRefreshNonce] = useState(null);

    useEffect(() => {
        debouncedSearch({
            variables: {}
        });
    }, [debouncedSearch, refreshNonce]);

    const [rowCount, setRowCount] = useState(0);
    
    useAGGridEnterprise();

    const affectedCIsCellRenderer = function(params) {
        return <>
            {_.map(params.value, relation => {
                return <div key={relation.relatedCIID}><CIID id={relation.relatedCIID} link={true} /></div>;
            })}
        </>
    }
    const timestampCellRenderer = function(params) {
        return formatTimestamp(params.value);
    }
    
    const columnDefs = [
        {
            headerName: "Message",
            field: "message",
            filter: true,
            flex: 1,
            autoHeight: true, 
            cellClass: 'cell-wrap-text'
        },
        {
            headerName: "Affected CIs",
            field: "affectedCIs",
            filter: false,
            resizable: false,
            width: 265,
            cellRenderer: "affectedCIsCellRenderer",
            sortable: false
        },
        {
            headerName: "Type",
            field: "type",
            filter: true,
            width: 85
        },
        {
            headerName: "Context",
            field: "context",
            filter: true
        },
        {
            headerName: "Group",
            field: "group",
            filter: true
        },
        {
            headerName: "ID",
            field: "id",
            filter: true
        },
        {
            headerName: "Occured at",
            field: "timestamp",
            filter: true,
            cellRenderer: "timestampCellRenderer",
            width: 140
        },
    ];

    const list = _.map(dataIssues?.traitEntities?.m__meta__issue__issue?.all, item => {
        return {...item.entity, timestamp: item.latestChange.timestamp};
    });

    return <>
        <h2>Issues</h2>
        {list && 
            <>
                <div style={{display: 'flex', justifyContent: 'space-between'}}>
                    <h3>Results: {rowCount} Issues</h3>
                    <Button icon={<SyncOutlined />} type="primary" loading={loadingIssues} onClick={() => setRefreshNonce(moment().toISOString())}>Refresh</Button>
                </div>
                <div style={{height:'100%'}} className={"ag-theme-balham"}>
                    <AgGridReact
                        components={{
                            affectedCIsCellRenderer: affectedCIsCellRenderer,
                            timestampCellRenderer: timestampCellRenderer,
                        }}
                        rowData={list}
                        columnDefs={columnDefs}
                        defaultColDef={{
                            resizable: true,
                            sortable: true,
                            filterParams: { newRowsAction: 'keep'}
                        }}
                        animateRows={true}
                        enableCellTextSelection={true}
                        getRowId={function (params) {
                            return `${params.data.id}-${params.data.group}-${params.data.context}-${params.data.type}`;
                        }}
                        onModelUpdated={(params) => setRowCount(params.api.getDisplayedRowCount())}
                    />
                </div>
            </>
        }
    </>;
}
