import React, { useState, useEffect } from "react";
import { Collapse, Alert } from "antd";
import AceEditor from "react-ace";

const { Panel } = Collapse;

function FeedbackMsg(props) {

    const [show, setShow] = useState(true);
    useEffect(() => {
        if (!props.swaggerErrorJson) {
            const timeId = setTimeout(() => {
                setShow(false);
            }, 3000);

            return () => {
                clearTimeout(timeId);
            };
        }
    }, [props.swaggerErrorJson]);

    // INFO: Error messages do not close automatically
    if (!show) return null;

    return (
        props.swaggerErrorJson ?
            <Collapse ghost >
                <Panel header={<Alert {...props.alertProps} message={props.alertProps.message + " (click to show details)"} closable onClose={() => setShow(false)} />} showArrow={false} key="swaggerErrorJson">
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
                <Alert {...props.alertProps} closable onClose={() => setShow(false)} />
            </div>
    );
}

export default FeedbackMsg;