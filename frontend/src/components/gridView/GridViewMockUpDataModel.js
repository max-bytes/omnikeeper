import { useState } from "react";
// TODO: delete this file, when finally using API
// returns mockUp-data for testing
// also see: https://www.mhx.at/gitlab/landscape/registry/snippets/1

export default function GridViewMockUpDataModel() {
    const [context] = useState(require("./gridViewMockUpJSONs/context.json"));
    const [schema1] = useState(require("./gridViewMockUpJSONs/schema1.json"));
    const [schema2] = useState(require("./gridViewMockUpJSONs/schema2.json"));
    const [data1] = useState(require("./gridViewMockUpJSONs/data1.json"));
    const [data2] = useState(require("./gridViewMockUpJSONs/data2.json"));

    const getMockUpData = (type, contextId) => {
        switch (type) {
            // Contexts:
            // note: when implementing, don't start with this. We know how this will work and it doesn't give us much insight.
            // work with a single, static context at first
            case "context":
                return context;
            // Schema:
            case "schema":
                switch (contextId) {
                    case "context1":
                        return schema1;
                    case "context2":
                        return schema2;
                    default:
                        break;
                }
                break;
            // Data:
            case "data":
                // cells that are not present here, but have a defined column in the schema should implicitly
                // be treated as not-set cells.
                // if a value of a cell is null, it should be treated as a not-set cell
                switch (contextId) {
                    case "context1":
                        return data1;
                    case "context2":
                        return data2;
                    default:
                        break;
                }
                break;
            default:
                break;
        }
    };
    return { getMockUpData };
}
