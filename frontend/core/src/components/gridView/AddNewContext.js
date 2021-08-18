import React, { useState, useEffect, useCallback } from 'react';
import { Form, Button } from "antd";
import { useParams, withRouter } from "react-router-dom";
import AceEditor from "react-ace";
import FeedbackMsg from "components/FeedbackMsg.js";

function AddNewContext(props) {
    const swaggerClient = props.swaggerClient;
    const apiVersion = props.apiVersion;
    const editMode = props.editMode;
    const { contextName } = useParams(); // get contextName from path

    const [jsonHasErrors, setJsonHasErrors] = useState(false);
    const [swaggerMsg, setSwaggerMsg] = useState("");
    const [swaggerErrorJson, setSwaggerErrorJson] = useState(false);
    const [loading, setLoading] = useState(true);
    const [context, setContext] = useState("Loading...");
    
    // get context
    const refresh = useCallback(async () => {
        try {
            setLoading(true);
            if (editMode) {
                    const contextJson = await swaggerClient.apis.GridView.GetContext(
                            {
                                version: apiVersion,
                                name: contextName
                            }
                        )
                        .then((result) => result.body);
                    setContext(JSON.stringify(contextJson.context, null, 2)); // set context
            } 
            else {
                const initialNewContext = {
                    id: "",
                    speakingName: "",
                    description: "",
                    configuration: {
                        showCIIDColumn: true,
                        writeLayer: "layer01",
                        readLayerset: ["layer01"],
                        columns: [{
                            "sourceAttributeName": "__name",
                            "columnDescription": "Name"
                        }],
                        trait: "",
                    },
                };
                setContext(JSON.stringify(initialNewContext, null, 2)); // set context
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
        <div style={{ height: "100%", width: "100%", padding: "10px" }}>
            <Form onFinish={async (e) => {
                if(!jsonHasErrors) {
                    try {
                        setLoading(true);
                        const addContext = editMode
                            ? await swaggerClient.apis.GridView.EditContext(
                                {
                                    version: apiVersion,
                                    name: contextName,
                                },
                                {
                                    requestBody: context,
                                }
                            ).then((result) => result.body)
                            : await swaggerClient.apis.GridView.AddContext(
                                {
                                    version: apiVersion,
                                },
                                {
                                    requestBody: context,
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
            }}>
                {swaggerMsg && <FeedbackMsg alertProps={{message: swaggerMsg, type: swaggerErrorJson ? "error": "success", showIcon: true, banner: true}} swaggerErrorJson={swaggerErrorJson} />}
                <h2>{editMode ? "Edit" : "Add"} Context</h2>
                {editMode && <h4>{contextName}</h4>}
                <AceEditor
                    value={context}
                    onValidate={a => {
                        const e = a.filter(a => a.type === 'error').length > 0;
                        setJsonHasErrors(e);
                    }}
                    mode="json"
                    theme="textmate"
                    onChange={newValue => setContext(newValue)}
                    name="Context Editor"
                    width={'unset'}
                    height={'50vh'}
                    style={{marginBottom: '10px', flexGrow: 1, border: "1px solid #ced4da", borderRadius: ".25rem", backgroundColor: loading ? "lightgrey" : "unset"}}
                    setOptions={{ 
                        showPrintMargin: false
                    }}
                    readOnly={loading}
                />
                <Form.Item style={{display: 'flex', justifyContent: 'center', width: "500px", margin: "auto"}}>
                    <Button style={{ width: "100%" }} type="primary" htmlType="submit" disabled={jsonHasErrors || loading} >{editMode ? "Change " : "Create New "}Context</Button>
                </Form.Item>
            </Form>
        </div>
    );
}

export default withRouter(AddNewContext);