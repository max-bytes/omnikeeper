import React from "react";
import Form from 'react-bootstrap/Form';
import 'ace-builds';
import 'ace-builds/webpack-resolver';
import AceEditor from "react-ace";

import "ace-builds/src-noconflict/mode-json";
import "ace-builds/src-noconflict/mode-yaml";
import "ace-builds/src-noconflict/theme-textmate";

export const AttributeTypes = [
    {
        id: 'TEXT',
        name: 'Text'
    },
    {
        id: 'MULTILINE_TEXT',
        name: 'Multi-Line Text'
    },
    {
        id: 'INTEGER',
        name: 'Integer'
    },
    {
        id: 'JSON',
        name: 'JSON'
    },
    {
        id: 'YAML',
        name: 'YAML'
    }
];

function attributeType2InputProps(type) {
    switch(type) {
      case 'INTEGER': return {type: 'number' };
      case 'MULTILINE_TEXT': return {type: 'text', as: 'textarea', rows: 7 };
      default: return {type: 'text' };
    }
};

export function InputControl(props) {
    if (props.type === 'JSON' || props.type === 'YAML') {
        // return <ReactJson name={false} src={JSON.parse(props.value)} enableClipboard={false} 
        //     style={{flexGrow: 1, border: "1px solid #ced4da", borderRadius: ".25rem"}}/>; // TODO
        return <AceEditor
            value={props.value}
            editorProps={{autoScrollEditorIntoView: true}}
            onValidate={a => {
                const e = a.filter(a => a.type === 'error').length > 0;
                props.setHasErrors(e);
            }}
            readOnly={props.disabled}
            mode={((props.type === "JSON") ? "json" : "yaml")}
            theme="textmate"
            onChange={newValue => props.onChange(newValue)}
            name={props.name}
            maxLines={20}
            minLines={6}
            width={'unset'}
            style={{flexGrow: 1, border: "1px solid #ced4da", borderRadius: ".25rem"}}
            setOptions={{ 
                showPrintMargin: false
             }}
        />;
    } else {
        // simple type, simple handling
        // if (props.isArray) {
        //     return <Form.Control autoFocus={props.autoFocus} disabled={props.disabled} style={{flexGrow: 1}}
        //         {...attributeType2InputProps(props.type)} placeholder="Enter value" value={props.value} 
        //         onChange={e => props.onChange(e.target.value)} />
        // } else {
            return <Form.Control autoFocus={props.autoFocus} disabled={props.disabled} style={{flexGrow: 1, alignSelf: 'center'}} 
                {...attributeType2InputProps(props.type)} placeholder="Enter value" value={props.value} 
                onChange={e => props.onChange(e.target.value)} />
        // }
    }
  }