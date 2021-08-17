import React from "react";
import { Typography } from 'antd';
import { Link } from "react-router-dom";

const { Text } = Typography;

const monospaceFontFamily = '"DejaVu Sans Mono", Menlo, Consolas, "Liberation Mono", Monaco, "Lucida Console", monospace';

export function ChangesetID(props) {
    const {id, link} = props;

    if (link) {
        return <Link to={"/changesets/" + id} style={{fontFamily: monospaceFontFamily}}>{id}</Link>;
    } else {
        return <Text style={{fontFamily: monospaceFontFamily}}>{id}</Text>;
    }
}

export function CIID(props) {
    const {id, link} = props;

    if (link) {
        return <Link to={"/explorer/" + id} style={{fontFamily: monospaceFontFamily}}>{id}</Link>;
    } else {
        return <Text style={{fontFamily: monospaceFontFamily}}>{id}</Text>;
    }
}