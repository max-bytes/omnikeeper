import React from "react";
import { CIID } from "./uuidRenderers";
import { Typography } from 'antd';

const { Title } = Typography;

export default function CITitle(props) {
    const {ciName, ciid} = props;

    const finalCIName = ciName ?? "[UNNAMED]";

    return <Title level={5} style={{marginBottom: 0}}>{finalCIName} - <CIID id={ciid} link={true} /></Title>;
}

