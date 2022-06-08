import { useLazyQuery } from "@apollo/client";
import React, {useCallback, useEffect, useState} from "react";
import { Button, Row, Col } from 'antd';
import _ from 'lodash';
import { queries } from "../../graphql/queries";
import { AgGridReact } from "ag-grid-react";
import moment from 'moment';
import { SyncOutlined } from '@ant-design/icons';
import { CIID } from "utils/uuidRenderers";
import { useAGGridEnterprise } from 'utils/useAGGridEnterprise';

export default function IssueList(props) {
    
    const [search, { loading: loadingIssues, data: dataIssues }] = useLazyQuery(queries.Issues, {
        notifyOnNetworkStatusChange: true
    });
    
    useAGGridEnterprise();

    // debounce search, so its not called too often
    const debouncedSearch = useCallback(_.debounce(search, 500), [search]);

    const [refreshNonce, setRefreshNonce] = useState(null);

    useEffect(() => {
        debouncedSearch({
            variables: {}
        });
    }, [debouncedSearch, refreshNonce]);

    const affectedCIsCellRenderer = function(params) {
        return <>
            {_.map(params.value, relation => {
                return <div key={relation.relatedCIID}><CIID id={relation.relatedCIID} link={true} /></div>;
            })}
        </>
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
            filter: true
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
    ];

    const list = _.map(dataIssues?.traitEntities?.m__meta__issue__issue?.all, item => item.entity);

    return <>
        <h2>Issues</h2>
        {list && 
            <>
                <div style={{display: 'flex', justifyContent: 'space-between'}}>
                    <h3>Results: {list.length} Issues</h3>
                    <Button icon={<SyncOutlined />} type="primary" loading={loadingIssues} onClick={() => setRefreshNonce(moment().toISOString())}>Refresh</Button>
                </div>
                <div style={{height:'100%'}} className={"ag-theme-balham"}>
                    <AgGridReact
                        frameworkComponents={{
                            affectedCIsCellRenderer: affectedCIsCellRenderer,
                        }}
                        rowData={list}
                        columnDefs={columnDefs}
                        defaultColDef={{
                            resizable: true,
                            sortable: true
                        }}
                        animateRows={true}
                        enableCellTextSelection={true}
                        getRowNodeId={function (data) {
                            return `${data.id}-${data.context}-${data.type}`;
                        }}
                    />
                </div>
            </>
        }
    </>;
}
