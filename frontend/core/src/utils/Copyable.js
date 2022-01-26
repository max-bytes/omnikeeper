import React from "react";
import { message, Button } from 'antd';
import { CopyOutlined } from '@ant-design/icons';

export function Copyable(props) {
    const {enabled, children, copyText} = props;
    if (enabled) {
        return <span style={{display: 'inline-flex', alignItems: 'center'}}>
            {children}
            <Button icon={<CopyOutlined />} type="text" size="small" onClick={() => {
                navigator.clipboard.writeText(copyText);
                message.info(`${copyText} copied to clipboard`);
                }} />
        </span>
    } else {
        return children;
    }
}