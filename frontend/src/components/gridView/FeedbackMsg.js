import React from "react";
import { Collapse, Alert } from "antd";
import AceEditor from "react-ace";

const { Panel } = Collapse;

function FeedbackMsg(props) {
    return (
        props.swaggerErrorJson ?
            <Collapse ghost >
                <Panel header={<Alert {...props.alertProps} message={props.alertProps.message + " (click to show details)"} />} showArrow={false} key="swaggerErrorJson">
                    {/* <pre style={{ heisght: "500px"}}>{props.swaggerErrorJson}</pre> */}
                    <AceEditor
                        value={props.swaggerErrorJson}
                        mode="json"
                        theme="textmate"
                        name="swaggerErrorJson Editor"
                        width={'unset'}
                        height={'50vh'}
                        style={{marginBottom: '10px', flexGrow: 1, border: "1px solid #ced4da", borderRadius: ".25rem" }}
                        setOptions={{ 
                            showPrintMargin: false
                        }}
                        readOnly={true}
                    />
                </Panel>
            </Collapse>
        :
            <div style={{ padding: "12px" }}>
                <Alert {...props.alertProps} />
            </div>
    );
}

export default FeedbackMsg;