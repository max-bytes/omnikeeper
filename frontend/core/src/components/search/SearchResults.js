import React from "react";
import { Link } from "react-router-dom";
import { FixedSizeList as List } from 'react-window';
import AutoSizer from "react-virtualized-auto-sizer";
import { Spin } from 'antd';
import { CIID } from "utils/uuidRenderers";


export function SearchResults(props) {

    const Row = ({ index, style }) => {
        const result = props.advancedSearchCompactCIs[index];
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
    
    const Results = () => (
        <div style={{flex:1}}> {/* reason for the div with flex: 1: https://github.com/bvaughn/react-virtualized/blob/master/docs/usingAutoSizer.md#can-i-use-autosizer-within-a-flex-container */}
            <AutoSizer>
                {({ height, width }) => (
                    <List
                    height={height}
                    itemCount={props.advancedSearchCompactCIs?.length ?? 0}
                    itemSize={42}
                    width={width}
                    >
                    {Row}
                    </List>
                )}
            </AutoSizer>
        </div>
    );

    return <>
    <h3>Results:</h3>     
        <Spin spinning={props.loading}>
            <h4>Number of CIs: {props.advancedSearchCompactCIs?.length ?? '?'}</h4>
            <Results />
        </Spin>
    </>;
}