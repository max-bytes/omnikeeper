import React from "react";
import { Typography } from 'antd';
import { Link } from "react-router-dom";
import { Copyable } from "./Copyable";

const { Text } = Typography;

const monospaceFontFamily = '"DejaVu Sans Mono", Menlo, Consolas, "Liberation Mono", Monaco, "Lucida Console", monospace';

export function ChangesetID(props) {
    const {id, link, copyable} = props;

    if (link) {
        return <Copyable copyText={id} enabled={copyable}>
            <Link to={"/changesets/" + id} style={{fontFamily: monospaceFontFamily}}>{id}</Link>
        </Copyable>;
    } else {
        return <Copyable copyText={id} enabled={copyable}>
            <Text style={{fontFamily: monospaceFontFamily}}>{id}</Text>
        </Copyable>;
    }
}

export function CIID(props) {
    const {id, link, copyable} = props;

    if (link) {
        return <Copyable copyText={id} enabled={copyable}>
            <Link to={"/explorer/" + id} style={{fontFamily: monospaceFontFamily}}>{id}</Link>
        </Copyable>;
    } else {
        return <Copyable copyText={id} enabled={copyable}>
            <Text style={{fontFamily: monospaceFontFamily}}>{id}</Text>
        </Copyable>;
    }
}