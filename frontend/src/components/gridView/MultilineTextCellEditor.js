import React, { useState, useRef, useImperativeHandle, forwardRef } from "react";
import { Input } from 'antd';

export default forwardRef((props, ref) => {
    const inputRef = useRef();
    const [value, setValue] = useState('');

    function inputHandler(e) {
        setValue(e.target.value);
    }
    useImperativeHandle(ref, () => {
        return {
            getValue: () => {
                return props.parseValue(value);
            },
            afterGuiAttached: () => {
                setValue(props.formatValue(props.value));
                inputRef.current.focus();
            },
        };
    });

    return (
        <Input.TextArea rows={4}
            type="text"
            className="ag-input-field-input ag-text-field-input"
            ref={inputRef}
            onChange={inputHandler}
            value={value}
        />
    )
})
