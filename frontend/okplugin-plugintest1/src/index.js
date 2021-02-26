import React, { Component, useState } from "react";
import { Button } from "antd";
import "antd/dist/antd.css";

export default (props) => {
    const FeedbackMsg = props.FeedbackMsg;
    const swaggerClient = props.swaggerClient;
    const apiVersion = props.apiVersion;

    const [context, setContext] = useState(null)

    const [swaggerMsg, setSwaggerMsg] = useState("");
    const [swaggerErrorJson, setSwaggerErrorJson] = useState(false);
    
    return class extends Component {
        render() {
            return (
                <div>
                    <p>Version: {getPluginVersion()} (Wanted version: {props.wantedPluginVersion ? props.wantedPluginVersion : "/"})</p>
                    <Button onClick={() => alert("'Hello' back from plugin 1")}>Say 'Hello'</Button>
                    <Button onClick={() => refreshData()} style={{ marginLeft: "10px" }}>Try Swagger</Button>
                    <br/><br/>
                    {context ? JSON.stringify(context) : ""}
                    <div style={{ height: "100%" }}>
                        {swaggerMsg && <FeedbackMsg alertProps={{message: swaggerMsg, type: swaggerErrorJson ? "error": "success", showIcon: true, banner: true}} swaggerErrorJson={swaggerErrorJson} />}
                    </div>
                </div>
            )
        }

        
    }

    // READ / refresh context
    async function refreshData() {
        try {
            const context = await swaggerClient().apis.OKPluginGenericJSONIngest.GetAllContexts({
                    version: apiVersion,
                })
                .then((result) => result.body);
            setContext(context);
        } catch(e) {
            setSwaggerErrorJson(JSON.stringify(e.response, null, 2));
            setSwaggerMsg(e.toString());
        }
    }
}

// babel simply copies the files with no respect to relative paths, so the converted file and the source file for development have different paths for package.json.
// This function takes care of this. (It's quite ugly, though...)
const getPluginVersion = () => {
    try {
        const pluginVersion = require('./package.json').version; // deployed path
        return pluginVersion;
    } catch(e) {
        const pluginVersion = require('./../package.json').version; // development path
        return pluginVersion;
    }
}

export const version = getPluginVersion();