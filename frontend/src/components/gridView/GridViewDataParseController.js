import _ from "lodash";

export default function GridViewDataParseController(rowStatus) {
    // ########## FROM BACKEND-STRUCTURE TO FRONTEND/AG-GRID-STRUCTURE ##########

    // Init columnDefs from schema and data
    const initColumnDefs = (schema, data) => {
        let columnDefsTemp = [
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
            columnDefsTemp.push({
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
        return columnDefsTemp;
    };

    // Init rowData from data
    const initRowData = (data) => {
        let dataTemp = [];
        _.forEach(data.rows, function (value) {
            let dataCellTemp = [];
            _.forEach(value.cells, function (value) {
                dataCellTemp[value.name] = value.value;
            });
            dataTemp.push({
                status: rowStatus.clean, // set status to 'clean'
                ciid: value.ciid,
                ...dataCellTemp,
            });
        });
        return dataTemp;
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
        initColumnDefs,
        initRowData,
    };
}
