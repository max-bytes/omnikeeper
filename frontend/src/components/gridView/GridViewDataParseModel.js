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
                field: value.name,
                editable: function (params) {
                    const ciid = params.node.data.ciid;
                    const name = params.colDef.field;
                    return getCellEditable(ciid, name, data);
                },
                cellStyle: function (params) {
                    const editable = params.colDef.editable(params);
                    return editable ? {} : { fontStyle: "italic" };
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
                dataCell[value.name] = value.value;
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
            _.forEach(value, function (value, key) {
                if (key !== "ciid" && key !== "status" && key !== "reference") // TODO: what if columns are named like these?
                    cells.push({
                        name: key,
                        value: value,
                    });
            });
            let row = {
                ciid: value.ciid,
                reference: value.reference,
                cells: cells,
            };
            sparseRows.push(row);
        });

        const changes = { sparseRows: sparseRows };
        return changes;
    };

    // ########## HELPERS ##########

    // returns editable/changeable-attr of cell, defined by its ciid and name/colName
    function getCellEditable(ciid, name, data) {
        let obj;
        if (data) {
            obj = _.find(data.rows, function (o) {
                return o.ciid === ciid;
            });
            if (obj)
                obj = _.find(obj.cells, function (o) {
                    return o.name === name;
                });
        }
        return obj ? obj.changeable : true;
    }

    return {
        // ########## FROM BACKEND-STRUCTURE TO FRONTEND/AG-GRID-STRUCTURE ##########
        createColumnDefs,
        createRowData,
        // ##########  FRONTEND/AG-GRID-STRUCTURE TO FROM BACKEND-STRUCTURE ##########
        createChanges,
    };
}
