import React from "react";
import { Typography } from 'antd';
import { Link } from "react-router-dom";

const { Text } = Typography;

export default function TraitID(props) {
    const {id, link, title} = props;

    const inner = (title) ? title : id;

    if (link) {
        return <Link to={"/traits/" + id}>{inner}</Link>;
    } else {
        return <Text>{inner}</Text>;
    }
}
