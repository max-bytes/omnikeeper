import React from "react";
import { Link } from "react-router-dom";
import { Spin } from 'antd';
import { CIID } from "utils/uuidRenderers";
import AutoSizedList from "utils/AutoSizedList";
import Text from "antd/lib/typography/Text";

export function SearchResults(props) {

    const {cis, loading, error} = props;

    const Row = (index) => {
        const result = cis[index];
        return <div
                style={{
                    display: "flex",
                    padding: "10px",
                    backgroundColor: index % 2 === 0 ? "#eee" : "#fff",
                }}>
                <div
                    style={{
                        flexGrow: "2",
                        fontWeight: "bold",
                        flexBasis: "0",
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                        whiteSpace: "nowrap",
                        paddingRight: "10px"
                    }}
                >
                    <Link key={result.id} to={`/explorer/${result.id}`}>{result.name ?? "[UNNAMED]"}</Link>
                </div>
                <div style={{ flexGrow: "0" }}>
                    <CIID id={result.id} link={false} copyable={true} />
                </div>
            </div>;
    };

    if (!loading && !cis) {
        if (error) {
            return <div style={{ display: "flex", justifyContent: "center", alignItems: "center", flexGrow: "1"}}>
                <Text type="danger">Encountered error while searching. Please try again...</Text>
            </div>;
        } else {
            return <div style={{ display: "flex", justifyContent: "center", alignItems: "center", flexGrow: "1"}}>
                <Text type="secondary">Use the search bar on the left to start searching...</Text>
            </div>;
        }
    } else {
        if (loading) {
            return <Spin spinning={true} size="large" tip="Searching...">&nbsp;</Spin>;
        } else {
            return <>  
                <h3>Results: {cis.length} CIs</h3>
                <div style={{flex:1}}> {/* reason for the div with flex: 1: https://github.com/bvaughn/react-virtualized/blob/master/docs/usingAutoSizer.md#can-i-use-autosizer-within-a-flex-container */}
                    <AutoSizedList itemCount={cis.length} item={Row} />
                </div>
            </>;
        }
    }

}