import React, { useState } from 'react';
import { Form, Button, Alert } from "antd";
import { withRouter } from 'react-router-dom'
import SwaggerClient from "swagger-client";
import env from "@beam-australia/react-env";
import AceEditor from "react-ace";
// import { useQuery } from '@apollo/client';
// import { queries } from '../../graphql/queries';

const swaggerDefUrl = `${env("BACKEND_URL")}/../swagger/v1/swagger.json`; // TODO: HACK: BACKEND_URL contains /graphql suffix, remove!
const apiVersion = 1;

function AddNewContext(props) {
    let initialNewContext = {
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
    const [context, setContext] = useState(JSON.stringify(initialNewContext, null, 2));

    var [jsonHasErrors, setJsonHasErrors] = useState(false);
    var [swaggerMsg, setSwaggerMsg] = useState("")
    var [swaggerError, setSwaggerError] = useState(false)
    var [loading, setLoading] = useState(false)

    return (
        <div style={{ height: "100%", width: "100%", padding: "10px" }}>
            <Form onFinish={async (e) => {
                if(!jsonHasErrors) {
                    try {
                        setLoading(true);
                        const addContext = await new SwaggerClient(swaggerDefUrl)
                            .then((client) =>
                                client.apis.GridView.AddContext(
                                    {
                                        version: apiVersion,
                                    },
                                    {
                                        requestBody: context,
                                    }
                                )
                            )
                            .then((result) => result.body);
                        setSwaggerError(false);
                        setSwaggerMsg("'" + addContext.name + "' has been created.");
                    } catch(e) { // TODO: find a way to get HTTP-Error-Code and -Msg and give better feedback!
                        setSwaggerError(true);
                        setSwaggerMsg(e.toString());
                    }
                    setLoading(false)
                }
            }}>
                <h2>Context</h2>
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
                    <Button style={{ width: "100%" }} type="primary" htmlType="submit" disabled={jsonHasErrors || loading} >Create New Context</Button>
                    {swaggerMsg && <Alert message={swaggerMsg} type={swaggerError ? "error": "success"} showIcon />}
                </Form.Item>
            </Form>
        </div>
    );
}

export default withRouter(AddNewContext);