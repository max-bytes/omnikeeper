import React, { useState } from "react";
import { StepContent } from "semantic-ui-react";
// TODO: delete this file, when finally using API
// returns mockUp-data for testing
// also see: https://www.mhx.at/gitlab/landscape/registry/snippets/1

export default function GridViewMockUpDataController() {
    const [context, setContext] = useState(
        require("./gridViewMockUpJSONs/context.json")
    );
    const [schema, setSchema] = useState(
        require("./gridViewMockUpJSONs/schema.json")
    );
    const [data, setData] = useState(
        require("./gridViewMockUpJSONs/data.json")
    );
    const [changes, setChanges] = useState(
        require("./gridViewMockUpJSONs/changes.json")
    );
    const [changeResults, setChangeResults] = useState(
        require("./gridViewMockUpJSONs/changeResults.json")
    );

    const getMockUpData = (type) => {
        switch (type) {
            // Contexts:
            // note: when implementing, don't start with this. We know how this will work and it doesn't give us much insight.
            // work with a single, static context at first
            case "context":
                return context;
            // Schema:
            case "schema":
                return schema;
            // Data:
            case "data":
                // cells that are not present here, but have a defined column in the schema should implicitly
                // be treated as not-set cells.
                // if a value of a cell is null, it should be treated as a not-set cell
                return data;
            // Changes:
            case "changes":
                // two possible modes:
                // 1) only needs to contain the cells that changed
                //    cells that should be changed to a not-set value should specify value: null
                // 2) sends ALL the cells for this CI, even the ones that did not change
                // -> prefer 1! Reason: less traffic, better maps to single attribute changes in backend
                return changes;
            // ChangeResults:
            case "changeResults":
                // two possible modes:
                // 1) only needs to contain the cells that changed
                //    cells that were changed to a not-set value should specify value: null
                // 2) returns ALL the cells for this CI, even the ones that did not change
                // -> prefer 2! Reason: better works with concurrent changes, is more in line with
                //  the regular data retrieval, more future-proof (dependent columns)

                // -> changeable: true, // we need to return (and act on) changeable here too
                return changeResults;
            default:
                break;
        }
    };

    const setMockUpData = (type, obj) => {
        switch (type) {
            case "context":
                setContext(obj);
                break;
            case "schema":
                setSchema(obj);
                break;
            case "data":
                setData(obj);
                break;
            case "changes":
                setChanges(obj);
                break;
            case "changeResults":
                setChangeResults(obj);
                break;
            default:
                break;
        }
    };
    return { getMockUpData, setMockUpData };
}
