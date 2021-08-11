import React, { useState, useEffect, useCallback } from 'react';
import { Form, Input, InputNumber, Button } from "antd";
import { useParams, withRouter } from "react-router-dom";
import FeedbackMsg from "components/FeedbackMsg.js";

function AddNewContext(props) {
    const swaggerClient = props.swaggerClient;
    const apiVersion = props.apiVersion;
    const editMode = props.editMode;
    const { contextName } = useParams(); // get contextName from path

    const [swaggerMsg, setSwaggerMsg] = useState("");
    const [swaggerErrorJson, setSwaggerErrorJson] = useState(false);
    const [loading, setLoading] = useState(true);
    const [context, setContext] = useState(null);
    
    // get context
    const refresh = useCallback(async () => {
        try {
            setLoading(true);
            if (editMode) {
                    const contextJson = await swaggerClient.apis.OKPluginGenericJSONIngest.GetContextByName(
                            {
                                version: apiVersion,
                                name: contextName
                            }
                        )
                        .then((result) => result.body);
                    setContext(contextJson); // set context
            } 
            else {
                const initialNewContext = {
                        name: "",
                        extractConfig: { $type: "OKPluginGenericJSONIngest.Extract.ExtractConfigPassiveRESTFiles, OKPluginGenericJSONIngest" },
                        transformConfig: { $type: "OKPluginGenericJSONIngest.Transform.JMESPath.TransformConfigJMESPath, OKPluginGenericJSONIngest", expression: "" }, 
                        loadConfig: { $type : "OKPluginGenericJSONIngest.Load.LoadConfig, OKPluginGenericJSONIngest", searchLayerIDs: [], writeLayerID: 0}
                };
                setContext(initialNewContext); // set context
            }
            // INFO: don't show message on basic load
        } catch(e) {
            setSwaggerErrorJson(JSON.stringify(e.response, null, 2));
            setSwaggerMsg(e.toString());
        }
        setLoading(false);
    }, [swaggerClient, apiVersion, contextName, editMode])

    useEffect(() => {refresh();}, [refresh]);

    return (
        <div style={{ display: 'flex', justifyContent: 'center', flexGrow: 1, margin: "10px"}}>
            { context ?
                <Form 
                    labelCol={{ span: "8" }}
                    style={{ display: 'flex', flexDirection: 'column', flexBasis: '1000px', margin:'10px 0px' }}
                    onFinish={async (e) => {
                        const newContext = context;
                        if (!editMode) newContext.name = e.name;
                        newContext.transformConfig.expression = e.expression ? e.expression : "";;
                        newContext.loadConfig.searchLayerIDs = e.searchLayerIDs.substring(1, e.searchLayerIDs.length-1).split`,`.map(x=>+x); // convert into array
                        newContext.loadConfig.writeLayerID = e.writeLayerID;

                        try {
                                setLoading(true);
                                // 'AddContext' will add a new context, if given context doesn't exist and edit the context, if it does
                                const addContext = await swaggerClient.apis.OKPluginGenericJSONIngest.AddNewContext(
                                        {
                                            version: apiVersion,
                                        },
                                        {
                                            requestBody: newContext,
                                        }
                                    ).then((result) => result.body);

                                setSwaggerErrorJson(false);
                                if(editMode) setSwaggerMsg("'" + contextName + "' has been changed.");
                                else setSwaggerMsg("'" + addContext.name + "' has been created.");
                            } catch(e) {
                                setSwaggerErrorJson(JSON.stringify(e.response, null, 2));
                                setSwaggerMsg(e.toString());
                            }
                            setLoading(false)
                        }
                    }
                    initialValues={{
                        "expression": context.transformConfig.expression, // text
                        "searchLayerIDs": "[" + context.loadConfig.searchLayerIDs.toString() + "]", // array with numbers (handled as text)
                        "writeLayerID": context.loadConfig.writeLayerID, // number
                    }}
                >
                    {swaggerMsg && <FeedbackMsg alertProps={{message: swaggerMsg, type: swaggerErrorJson ? "error": "success", showIcon: true, banner: true}} swaggerErrorJson={swaggerErrorJson} />}
                    <h2>{editMode ? "Edit" : "Add"} Context</h2>
                    {editMode && <h4>{contextName}</h4>}

                    {!editMode && 
                        <Form.Item name="name" label="name" style={{ margin: "0 0 50px 0" }}>
                            <Input />
                        </Form.Item>
                    }

                    <Form.Item label="transformConfig" style={{ margin: 0, fontStyle: "italic" }}/>
                    <Form.Item name="expression" label="expression" tooltip="JMESPath" style={{ margin: "0 0 50px 0"}}>
                        <Input.TextArea />
                    </Form.Item>

                    <Form.Item label="loadConfig" style={{ margin: 0, fontStyle: "italic" }}/>
                    <Form.Item name="searchLayerIDs" label="searchLayerIDs" tooltip="Array of layerIDs - e.g. '[layer1,layer2]'" rules={[{ required: true, pattern: /\[[0-9a-z_,]*[0-9a-z_]\]/ }]}>
                        <Input />
                    </Form.Item>
                    <Form.Item name="writeLayerID" label="writeLayerID" rules={[{ required: true }]} style={{ margin: "0 0 50px 0"}}>
                        <Input />
                    </Form.Item>


                    <div style={{ display: "flex", justifyContent: "center" }}>
                        <Button type="primary" htmlType="submit" disabled={loading} style={{ width: "100%" }}>{editMode ? "Change " : "Create New "}Context</Button>
                    </div>
                </Form>
            : "Loading..." }
        </div>
    );
}

export default withRouter(AddNewContext);