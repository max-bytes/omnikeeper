import { useState } from "react";
// TODO: delete this file, when finally using API
// returns mockUp-data for testing
// also see: https://www.mhx.at/gitlab/landscape/registry/snippets/1

export default function GridViewMockUpDataModel() {
    const [context, setContext] = useState(
        require("./gridViewMockUpJSONs/context.json")
    );
    const [schema, setSchema] = useState(
        require("./gridViewMockUpJSONs/schema.json")
    );
    const [data, setData] = useState(
        require("./gridViewMockUpJSONs/data.json")
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
            default:
                break;
        }
    };
    return { getMockUpData, setMockUpData };
}
