import React from "react";
import Form from 'react-bootstrap/Form';
import 'ace-builds';
import 'ace-builds/webpack-resolver';
import AceEditor from "react-ace";

import "ace-builds/src-noconflict/mode-java";
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
    }
];

function attributeType2InputProps(type) {
    switch(type) {
      case 'INTEGER': return {type: 'number' };
      case 'JSON': return {type: 'text' };
      case 'MULTILINE_TEXT': return {type: 'text', as: 'textarea', rows: 7 };
      default: return {type: 'text' };
    }
};

export function InputControl(props) {
    if (props.type === 'JSON') {
        return <AceEditor
            value={props.value}
            editorProps={{autoScrollEditorIntoView: true}}
            // onLoad={function(editor){ editor.renderer.setPadding(10); editor.renderer.setScrollMargin(10); }}
            onValidate={a => {
                const e = a.filter(a => a.type === 'error').length > 0;
                props.setHasErrors(e);
            }}
            readOnly={props.disabled}
            mode="json"
            theme="textmate"
            onChange={newValue => props.onChange(newValue)}
            name={props.name}
            maxLines={20}
            minLines={6}
            width={'unset'}
            style={{flexGrow: 1, border: "1px solid #ced4da", borderRadius: ".25rem"}}
            setOptions={{ 
                showPrintMargin: false,
                // autoScrollEditorIntoView: true
             }}
        />;
    } else {
        // simple type, simple handling
        if (props.isArray) {
            return <Form.Control disabled={props.disabled} style={{flexGrow: 1}}
                {...attributeType2InputProps(props.type)} placeholder="Enter value" value={props.value} 
                autoFocus={props.autoFocus}
                onChange={e => props.onChange(e.target.value)} />
        } else {
            return <Form.Control autoFocus={props.autoFocus} disabled={props.disabled} style={{flexGrow: 1}} 
                {...attributeType2InputProps(props.type)} placeholder="Enter value" value={props.value} 
                onChange={e => props.onChange(e.target.value)} />
        }
    }
  }