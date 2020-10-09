// TODO: delete this file, when finally using API
// returns mockUp-data for testing
// also see: https://www.mhx.at/gitlab/landscape/registry/snippets/1
export default function getMockUpData(type) {
    switch (type) {
        // Contexts:
        // note: when implementing, don't start with this. We know how this will work and it doesn't give us much insight.
        // work with a single, static context at first
        case "context":
            return {
                configuredContexts: [
                    {
                        name: "test",
                        speakingName: "Test-Context",
                        description:
                            "This is a context used to test out basic GridView capabilities",
                    },
                    {
                        name: "test2",
                        speakingName: "Test-Context2",
                        description:
                            "This is a context2 used to test out basic GridView capabilities",
                    },
                ],
            };
        // Schema:
        case "schema":
            return {
                showCIIDColumn: true,
                columns: [
                    {
                        name: "attr1",
                        description: "Attribute 1",
                    },
                    {
                        name: "attr2",
                        description: "Attribute 2",
                    },
                    {
                        name: "attr3",
                        description: "Attribute 3",
                    },
                ],
            };
        // Data:
        case "data":
            return {
                rows: [
                    {
                        ciid: "035fbf89-1ed2-4432-a67e-620577cc806d",
                        // cells that are not present here, but have a defined column in the schema should implicitly
                        // be treated as not-set cells.
                        // if a value of a cell is null, it should be treated as a not-set cell
                        cells: [
                            {
                                name: "attr1",
                                value: "Value A-1",
                                changeable: false,
                            },
                            {
                                name: "attr2",
                                value: "Value A-2",
                                changeable: false,
                            },
                            {
                                name: "attr3",
                                value: "Value A-3",
                                changeable: true,
                            },
                        ],
                    },
                    {
                        ciid: "620577cc806d-a67e-035fbf89-4432-1ed2",
                        cells: [
                            {
                                name: "attr1",
                                value: "Value B-1",
                                changeable: true,
                            },
                            {
                                name: "attr3",
                                value: "Value B-3",
                                changeable: true,
                            },
                        ],
                    },
                    {
                        ciid: "4432-a67e-620577cc806d-035fbf89-1ed2",
                        cells: [
                            {
                                name: "attr1",
                                value: "Value C-1",
                                changeable: true,
                            },
                            {
                                name: "attr2",
                                value: "",
                                changeable: true,
                            },
                            {
                                name: "attr3",
                                value: "Value C-3",
                                changeable: false,
                            },
                            {
                                name: "attr4",
                                value: "Value C-4",
                                changeable: true,
                            },
                        ],
                    },
                ],
            };
        // Changes:
        case "changes":
            return {
                sparseRows: [
                    // prefixed "sparse", explanation why below
                    {
                        ciid: "035fbf89-1ed2-4432-a67e-620577cc806d",
                        // two possible modes:
                        // 1) only needs to contain the cells that changed
                        //    cells that should be changed to a not-set value should specify value: null
                        // 2) sends ALL the cells for this CI, even the ones that did not change
                        // -> prefer 1! Reason: less traffic, better maps to single attribute changes in backend
                        cells: [
                            {
                                name: "attributeA",
                                value: "Value A-1 changed",
                            },
                            // ...
                        ],
                    },
                    // ...
                ],
            };
        // ChangeResults:
        case "changeResults":
            return {
                rows: [
                    {
                        ciid: "035fbf89-1ed2-4432-a67e-620577cc806d",
                        // two possible modes:
                        // 1) only needs to contain the cells that changed
                        //    cells that were changed to a not-set value should specify value: null
                        // 2) returns ALL the cells for this CI, even the ones that did not change
                        // -> prefer 2! Reason: better works with concurrent changes, is more in line with
                        //  the regular data retrieval, more future-proof (dependent columns)
                        cells: [
                            {
                                name: "attributeA",
                                value: "Value A-1 changed",
                                changeable: true, // we need to return (and act on) changeable here too
                            },
                            // ...
                        ],
                    },
                    // ...
                ],
            };

        default:
            break;
    }
}
