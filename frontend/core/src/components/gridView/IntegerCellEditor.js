import React, { useState, useRef, useImperativeHandle, forwardRef } from "react";
import { InputNumber } from 'antd';

export default forwardRef((props, ref) => {
    const inputRef = useRef();
    const [value, setValue] = useState('');

    function inputHandler(e) {
        setValue(e);
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
        <InputNumber
            className="ag-input-field-input ag-text-field-input"
            ref={inputRef}
            onChange={inputHandler}
            value={value}
        />
    )
})
