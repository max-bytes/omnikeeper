import React from "react";
import { Typography } from 'antd';
import { Link } from "react-router-dom";

const { Text } = Typography;

export default function TraitID(props) {
    const {id, link} = props;

    if (link) {
        return <Link to={"/traits/" + id}>{id}</Link>;
    } else {
        return <Text>{id}</Text>;
    }
}
