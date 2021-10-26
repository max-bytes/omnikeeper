import React from "react";
import { Link } from "react-router-dom";
import { Spin } from 'antd';
import { CIID } from "utils/uuidRenderers";
import AutoSizedList from "utils/AutoSizedList";

export function SearchResults(props) {

    const {cis} = props;

    const Row = ({ index, style }) => {
        const result = cis[index];
        return <Link key={result.id} to={`/explorer/${result.id}`} style={style}>
            <div
                style={{
                    display: "flex",
                    padding: "10px",
                    backgroundColor: index % 2 === 0 ? "#eee" : "#fff",
                }}
            >
                <div
                    style={{
                        flexGrow: "2",
                        fontWeight: "bold",
                        flexBasis: "0",
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                        whiteSpace: "nowrap"
                    }}
                >
                    {result.name ?? "[UNNAMED]"}
                </div>
                <div style={{ flexGrow: "2", flexBasis: "0" }}>
                    <CIID id={result.id} link={false} />
                </div>
            </div>
        </Link>;
    };

    return <>
        <h3>Results:</h3>     
        <Spin spinning={props.loading}>
            <h4>Number of CIs: {cis?.length ?? '?'}</h4>
            <div style={{flex:1}}> {/* reason for the div with flex: 1: https://github.com/bvaughn/react-virtualized/blob/master/docs/usingAutoSizer.md#can-i-use-autosizer-within-a-flex-container */}
                <AutoSizedList itemCount={cis?.length ?? 0} item={Row} />
            </div>
        </Spin>
    </>;
}