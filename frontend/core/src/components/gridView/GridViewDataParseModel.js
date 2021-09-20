import _ from "lodash";

export default function GridViewDataParseModel(rowStatus) {
    // ########## FROM BACKEND-STRUCTURE TO FRONTEND/AG-GRID-STRUCTURE ##########

    // Create columnDefs from schema and data
    const createColumnDefs = (schema, data) => {
        let columnDefs = [
            {
                // new, edited, clean, deleted
                headerName: "Status",
                field: "status",
                editable: false,
                checkboxSelection: true, // checkbox for selecting row
                pinned: "left", // pinn to the left
                // set width = minWidth = maxWith, so fitting is suppressed in every possible way
                width: 92,
                minWidth: 92,
                maxWidth: 92,
                resizable: false,
                suppressSizeToFit: true, // suppress sizeToFit
                // get name of status
                valueGetter: function (params) {
                    if (params.data.status.id !== undefined)
                        return params.data.status.name;
                },
            },
            {
                headerName: "CIID",
                field: "ciid",
                editable: false,
                hide: !schema.showCIIDColumn,
                cellStyle: { fontStyle: "italic" },
            },
        ];
        _.forEach(schema.columns, function (value) {
            columnDefs.push({
                headerName: value.description,
                field: value.id,
                editable: function (params) {
                    const ciid = params.node.data.ciid;
                    const columnID = params.colDef.field;
                    return getCellEditable(ciid, columnID, data, value.writable); // TODO: this function sucks(?) it cannot deal with new items
                },
                cellStyle: function (params) {
                    const editable = params.colDef.editable(params);
                    return editable ? {} : { fontStyle: "italic" };
                },
                valueParser: (params) => {
                    return {...params.oldValue, values: [params.newValue]};
                },
                valueFormatter: (params) => {
                    const value = params.value?.values?.[0];

                    // IMPORTANT INFO: Copying an undefined value with aggrid_copy_cut_paste-module is NOT possible, because this valueFormatter will return an empty string and therefore force the module to copy this empty string instead.
                    // valueFormatters don't allow returning undefined. Doing so would instead act like there simply is no valueFormatter at all.
                    // In this case the CellEditor would then get an Object and would show "[object Object]". (Further, the valueParser would throw an error, when trying to save this object.)
                    if (value === undefined)
                        return ""; // TODO: is this an Ok default for all cell editors?
                    return value;
                },
                cellRenderer: (params) => {
                    const value = params.value?.values?.[0];
                    if (value === undefined)
                        return "[not set]";
                    return value;
                },
                cellEditorSelector: function(params) {
                    if (params.value.type === 'MultilineText') {
                        return { component: 'multilineTextCellEditor' };
                    } else if (params.value.type === 'Integer') {
                        return { component: 'integerCellEditor' };
                    } else {
                        return { component: 'agTextCellEditor', params: {useFormatter: true}};
                    }
                },
                suppressKeyboardEvent: (params) => {
                    const colId = params.column.colId;
                    const value = params.data[colId];
                    
                    // TODO: this is not the best place for this, but I couldn't make it work inside the cell editor
                    if (value.type === 'MultilineText') {
                        // prevent shift+enter from propagating
                        const event = params.event;
                        const key = event.which || event.keyCode;
                        const keycodeEnter = 13;
                        if (event.shiftKey && key === keycodeEnter) { // shift+enter allows for newlines
                            return true;
                        }
                        return false;
                    } else return false;
                },
            });
        });
        return columnDefs;
    };

    // Create rowData from data (or changeResults)
    const createRowData = (data) => {
        let rowdata = [];
        _.forEach(data.rows, function (value) {
            let dataCell = [];
            _.forEach(value.cells, function (value) {
                dataCell[value.columnID] = value.value;
            });
            rowdata.push({
                status: rowStatus.clean, // set status to 'clean'
                ciid: value.ciid,
                ...dataCell,
            });
        });
        return rowdata;
    };

    // ##########  FRONTEND/AG-GRID-STRUCTURE TO FROM BACKEND-STRUCTURE ##########

    // Create changes from rowData (delta)
    const createChanges = (rowData) => {
        let sparseRows = [];
        _.forEach(rowData, function (value) {
            let cells = [];
            _.forOwn(value, function (v, key, o) {
                if (key !== "ciid" && key !== "status")
                    cells.push({
                        id: key,
                        value: v,
                    });
            });
            let row = {
                ciid: value.ciid,
                cells: cells,
            };
            sparseRows.push(row);
        });

        const changes = { sparseRows: sparseRows };
        return changes;
    };

    // ########## HELPERS ##########

    // returns editable/changeable-attr of cell, defined by its ciid and id (field in ag grid speak)
    function getCellEditable(ciid, columnID, data, isColumnWritable) {
        if (!isColumnWritable) return false;
        if (data) {
            let row = _.find(data.rows, o => o.ciid === ciid);
            if (row) {
                let cell = _.find(row.cells, o => o.columnID === columnID);
                if (cell)
                    return cell.changeable;
                else return false;
            } else {
                return true; // row must be new because we couldn't find it, treat cell as writable
            }
        }
        return false;
    }

    return {
        // ########## FROM BACKEND-STRUCTURE TO FRONTEND/AG-GRID-STRUCTURE ##########
        createColumnDefs,
        createRowData,
        // ##########  FRONTEND/AG-GRID-STRUCTURE TO FROM BACKEND-STRUCTURE ##########
        createChanges,
    };
}
