import { useLazyQuery, useMutation } from "@apollo/client";
import React, {useCallback, useEffect, useState, useRef} from "react";
import { Button, Space } from 'antd';
import _ from 'lodash';
import { queries } from "../../graphql/queries";
import { mutations } from '../../graphql/mutations'
import { AgGridReact } from "ag-grid-react";
import moment from 'moment';
import { SyncOutlined, DeleteOutlined, ExportOutlined } from '@ant-design/icons';
import { CIID } from "utils/uuidRenderers";
import { useAGGridEnterprise } from 'utils/useAGGridEnterprise';
import { formatTimestamp } from "utils/datetime";


export default function IssueList(props) {
    
    const [search, { loading: loadingIssues, data: dataIssues }] = useLazyQuery(queries.Issues, {
        notifyOnNetworkStatusChange: true
    });
    const [deleteIssues] = useMutation(mutations.DELETE_ISSUES);

    // debounce search, so its not called too often
    const debouncedSearch = useCallback(_.debounce(search, 500), [search]);

    const [refreshNonce, setRefreshNonce] = useState(null);

    const [deleteInProgress, setDeleteInProgress] = useState(false);
    const [csvExportInProgress, setCSVExportInProgress] = useState(false);
    const [excelExportInProgress, setExcelExportInProgress] = useState(false);

    const [selectedRows, setSelectedRows] = useState([]);

    const gridRef = useRef();

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
            cellClass: 'cell-wrap-text',
            checkboxSelection: true,
            headerCheckboxSelection: true,
            headerCheckboxSelectionFilteredOnly: true
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
        {
            headerName: "CIID",
            field: "ciid",
            hide: true,
            suppressColumnsToolPanel: true,
            suppressFiltersToolPanel: true
         }
    ];


    const list = _.map(dataIssues?.traitEntities?.m__meta__issue__issue?.all, item => {
        return {...item.entity, timestamp: item.latestChange.timestamp, ciid: item.ciid};
    });

    const deleteButton = <Button icon={<DeleteOutlined />} type="primary" danger disabled={selectedRows.length === 0 || deleteInProgress} loading={deleteInProgress} onClick={() => {
        const ciidsToDelete = selectedRows.map(r => r.ciid);

        setDeleteInProgress(true);
        deleteIssues({ variables: { ciids: ciidsToDelete } })
            .then(r => setRefreshNonce(moment().toISOString()))
            .catch(e => console.error(e))
            .finally(() => setDeleteInProgress(false));
    }}>Delete Selected ({selectedRows.length.toString()})</Button>;

    const exportCSVButton = <Button icon={<ExportOutlined />} type="primary" disabled={csvExportInProgress} loading={csvExportInProgress} onClick={() => {
        setCSVExportInProgress(true);
        gridRef.current.api.exportDataAsCsv({
            fileName: 'issues_export.csv',
            columnKeys: ["message", "type", "context", "group", "id", "timestamp"]
        });
        setCSVExportInProgress(false);
    }}>Export All as CSV</Button>;
    const exportExcelButton = <Button icon={<ExportOutlined />} type="primary" disabled={excelExportInProgress} loading={excelExportInProgress} onClick={() => {
        setExcelExportInProgress(true);
        gridRef.current.api.exportDataAsExcel({
            fileName: 'issues_export.xlsx',
            columnKeys: ["message", "type", "context", "group", "id", "timestamp"]
        });
        setExcelExportInProgress(false);
    }}>Export All as Excel</Button>;

    return <>
        <h2>Issues</h2>
        {list && 
            <>
                <div style={{display: 'flex', justifyContent: 'space-between', marginBottom: "10px"}}>
                    <h3>Results: {rowCount} Issues</h3>
                    <Space>
                        <Button icon={<SyncOutlined />} type="primary" loading={loadingIssues} onClick={() => setRefreshNonce(moment().toISOString())}>Refresh</Button>
                        {deleteButton}
                        {exportCSVButton}
                        {exportExcelButton}
                    </Space>
                </div>
                <div style={{height:'100%'}} className={"ag-theme-balham"}>
                    <AgGridReact
                        ref={gridRef}
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
                        rowSelection='multiple'
                        onSelectionChanged={(params) => {
                            setSelectedRows(params.api.getSelectedRows());
                        }}
                    />
                </div>
            </>
        }
    </>;
}
