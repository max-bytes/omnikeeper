import React, { useState, useEffect } from 'react';
import { Form, Button, Alert } from "antd";
import { useParams, withRouter } from "react-router-dom";
import AceEditor from "react-ace";

function AddNewContext(props) {
    const swaggerJson = props.swaggerJson;
    const apiVersion = props.apiVersion;
    const editMode = props.editMode;
    const { contextName } = useParams(); // get contextName from path

    const [jsonHasErrors, setJsonHasErrors] = useState(false);
    const [swaggerMsg, setSwaggerMsg] = useState("");
    const [swaggerError, setSwaggerError] = useState(false);
    const [loading, setLoading] = useState(true);
    const [context, setContext] = useState("Loading...");
    
    // get context
    useEffect(() => {
        if (editMode) {
            if (swaggerJson) {
                const fetchContext = async () => {
                    const contextJson = await swaggerJson.apis.GridView.GetContext(
                            {
                                version: apiVersion,
                                name: contextName
                            }
                        )
                        .then((result) => result.body);
                    setContext(JSON.stringify(contextJson.context, null, 2)); // set context
                    setLoading(false);
                };
                fetchContext();
            }
        } 
        else {
            const initialNewContext = {
                name: "",
                speakingName: "",
                description: "",
                configuration: {
                    showCIIDColumn: true,
                    writeLayer: 0,
                    readLayerset: [0],
                    columns: [],
                    trait: "",
                },
            };
            setContext(JSON.stringify(initialNewContext, null, 2)); // set context
            setLoading(false);
        }
    }, [editMode, contextName, swaggerJson, apiVersion]);

    return (
        <div style={{ height: "100%", width: "100%", padding: "10px" }}>
            <Form onFinish={async (e) => {
                if(!jsonHasErrors) {
                    try {
                        if (swaggerJson) {
                            setLoading(true);
                            const addContext = editMode
                                ? await swaggerJson.apis.GridView.EditContext(
                                    {
                                        version: apiVersion,
                                        name: contextName,
                                    },
                                    {
                                        requestBody: context,
                                    }
                                ).then((result) => result.body)
                                : await swaggerJson.apis.GridView.AddContext(
                                    {
                                        version: apiVersion,
                                    },
                                    {
                                        requestBody: context,
                                    }
                                ).then((result) => result.body);

                            setSwaggerError(false);
                            if(editMode) setSwaggerMsg("'" + contextName + "' has been changed.");
                            else setSwaggerMsg("'" + addContext.name + "' has been created.");
                        }
                    } catch(e) { // TODO: find a way to get HTTP-Error-Code and -Msg and give better feedback!
                        setSwaggerError(true);
                        setSwaggerMsg(e.toString());
                    }
                    setLoading(false)
                }
            }}>
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
                    {swaggerMsg && <Alert message={swaggerMsg} type={swaggerError ? "error": "success"} showIcon />}
                </Form.Item>
            </Form>
        </div>
    );
}

export default withRouter(AddNewContext);