// TODO: delete this file, when finally using API
// returns mockUp-data for testing
// also see: https://www.mhx.at/gitlab/landscape/registry/snippets/1
export default function getMockUpData(type) {
    switch (type) {
        // Contexts:
        // note: when implementing, don't start with this. We know how this will work and it doesn't give us much insight.
        // work with a single, static context at first
        case "context":
            var json = require("./gridViewMockUpJSONs/context.json");
            return json;
        // Schema:
        case "schema":
            var json = require("./gridViewMockUpJSONs/schema.json");
            return json;
        // Data:
        case "data":
            // cells that are not present here, but have a defined column in the schema should implicitly
            // be treated as not-set cells.
            // if a value of a cell is null, it should be treated as a not-set cell
            var json = require("./gridViewMockUpJSONs/data.json");
            return json;
        // Changes:
        case "changes":
            // two possible modes:
            // 1) only needs to contain the cells that changed
            //    cells that should be changed to a not-set value should specify value: null
            // 2) sends ALL the cells for this CI, even the ones that did not change
            // -> prefer 1! Reason: less traffic, better maps to single attribute changes in backend
            var json = require("./gridViewMockUpJSONs/changes.json");
            return json;
        // ChangeResults:
        case "changeResults":
            // two possible modes:
            // 1) only needs to contain the cells that changed
            //    cells that were changed to a not-set value should specify value: null
            // 2) returns ALL the cells for this CI, even the ones that did not change
            // -> prefer 2! Reason: better works with concurrent changes, is more in line with
            //  the regular data retrieval, more future-proof (dependent columns)

            // -> changeable: true, // we need to return (and act on) changeable here too
            var json = require("./gridViewMockUpJSONs/changeResults.json");
            return json;
        default:
            break;
    }
}
