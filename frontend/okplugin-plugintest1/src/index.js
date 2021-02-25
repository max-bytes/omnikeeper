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
                    <p>Wanted version from Core: {props.wantedVersion ? props.wantedVersion : "/"}</p>
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