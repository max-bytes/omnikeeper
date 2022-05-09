import React from "react";
import { Form, Input, InputNumber } from 'antd';
import 'ace-builds';
import 'ace-builds/webpack-resolver';
import AceEditor from "react-ace";
import { Image } from 'antd';
import { useExplorerLayers } from '../utils/layers';
import env from "@beam-australia/react-env";
import buildUrl from 'build-url';

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
        id: 'DOUBLE',
        name: 'Double'
    },
    {
        id: 'JSON',
        name: 'JSON'
    },
    {
        id: 'YAML',
        name: 'YAML'
    },
    {
        id: 'MASK',
        name: 'Mask'
    },
    {
        id: 'IMAGE',
        name: 'Image'
    }
];

function InputElementForAttributeType(props) {
    const {type, onChange, ...rest} = props;

    switch(type) {
        case 'INTEGER':
            return <InputNumber {...rest} stringMode onChange={onChange} />;
        case 'DOUBLE':
            return <InputNumber {...rest} stringMode onChange={onChange} />;
        default: 
            // HACK: onChange is implemented inconsistently between InputNumber and Input, need to consolidate
            const compatibleOnChange = (e) => onChange(e.target.value);
            return <Input {...rest} onChange={compatibleOnChange} />;
      }
}

function generateImageURL(ciid, layers, attributeName, index) {
    const url = buildUrl(env('BACKEND_URL'), {
        path: '/api/v1/attributeValueImage',
        disableCSV: true,
        queryParams: {
            ciid: ciid,
            layerIDs: layers.map(l => l.id),
            attributeName: attributeName,
            index: index
        }
    });
    return url;
}

export function InputControl(props) {
    const { data: visibleLayers } = useExplorerLayers(true);
    if (props.type === 'IMAGE') {
        const url = generateImageURL(props.ciid, visibleLayers, props.attributeName, props.arrayIndex);
        return <Image width={200} height={100} src={url}
            style={{flexGrow: 1}}
            fallback="data:image/png;base64,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"
        />;
    } else if (props.type === 'JSON' || props.type === 'YAML') {
        // return <ReactJson name={false} src={JSON.parse(props.value)} enableClipboard={false} 
        //     style={{flexGrow: 1, border: "1px solid #ced4da", borderRadius: ".25rem"}}/>; // TODO
        var value = props.value;
        if (props.type === 'JSON') {
            var o = JSON.parse(value);
            value = JSON.stringify(o, null, 2);
        }
        return <AceEditor
            value={value}
            editorProps={{autoScrollEditorIntoView: true}}
            onValidate={a => {
                const e = a.filter(a => a.type === 'error').length > 0;
                props.setHasErrors(e);
            }}
            readOnly={props.disabled}
            mode={((props.type === "JSON") ? "json" : "yaml")}
            theme="textmate"
            onChange={newValue => props.onChange(newValue)}
            name={props.attributeName + "_" + props.arrayIndex}
            maxLines={20}
            minLines={6}
            width={'unset'}
            style={{flexGrow: 1, border: "1px solid #ced4da", borderRadius: ".25rem"}}
            setOptions={{ 
                showPrintMargin: false
             }}
        />;
    } else if (props.type === 'MULTILINE_TEXT') {
        // from AntDesign Docs: use Input.TextArea instead of type="textarea"
        return (
            <Form.Item style={{ marginBottom: 0 }} labelCol={props.hideNameLabel ? {} : { span: "4" }} name={props.name} label={props.hideNameLabel ? "" : props.name} initialValue={props.value ?? ""}>
                <Input.TextArea
                    style={{ flexGrow: 1, alignSelf: "center" }}
                    autoFocus={props.autoFocus}
                    disabled={props.disabled}
                    placeholder={props.disabled ? "[Empty]" : "[Empty], enter value"}
                    value={props.value ?? ""}
                    onChange={(e) => props.onChange(e.target.value)}
                    autoSize={{ minRows: 3, maxRows: 5 }}
                />
            </Form.Item>
        );
    } else if (props.type === 'MASK') {
        return <Form.Item style={{ marginBottom: 0 }} labelCol={props.hideNameLabel ? {} : { span: "4" }} name={props.name} label={props.hideNameLabel ? "" : props.name}>
            <div style={{minHeight: '32px', border: '1px dashed black', background: '#f0f0f0', opacity: '0.7', display: 'flex', alignItems: 'center'}}>
                <span>[MASK]</span>
            </div>
        </Form.Item>
    } else {
        // simple type, simple handling
        return (
            <Form.Item style={{ marginBottom: 0 }} labelCol={props.hideNameLabel ? {} : { span: "4" }} name={props.name} label={props.hideNameLabel ? "" : props.name} initialValue={props.value ?? ""}>
                <InputElementForAttributeType
                    type={props.type}
                    style={{ flexGrow: 1, alignSelf: "center", minWidth: "150px" }}
                    autoFocus={props.autoFocus}
                    disabled={props.disabled}
                    placeholder={props.disabled ? "[Empty]" : "[Empty], enter value"}
                    value={props.value ?? ""}
                    onChange={(v) => props.onChange(v)}
                />
            </Form.Item>
        );
    }
  }