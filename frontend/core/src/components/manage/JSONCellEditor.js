import React, {useState, forwardRef, useImperativeHandle} from "react";
import { Form } from "antd"
import 'ace-builds';
import 'ace-builds/webpack-resolver';
import AceEditor from "react-ace";

export default forwardRef((props, ref) => {

    var [value, setValue] = useState(props.value ?? '');

    useImperativeHandle(ref, () => {
        return {
            getValue: () => {
                return value;
            },
            isPopup: () => true
        };
    });

    return <div style={{display: 'flex'}}>
        <Form style={{minWidth: '400px', margin: '10px'}}>
        <AceEditor ref={ref}
            value={value}
            mode="json"
            theme="textmate"
            onChange={newValue => setValue(newValue)}
            width={'800px'}
            height={'50vh'}
            style={{}}
            setOptions={{ showPrintMargin: false }}
        />
        </Form>
    </div>;
})