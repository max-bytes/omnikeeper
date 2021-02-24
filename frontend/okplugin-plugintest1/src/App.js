import React, { Component } from "react";
import { Button } from "antd";
import "antd/dist/antd.css";

export default (wantedVersion) => {
    return class extends Component {
        render() {
            return (
                <div>
                    <h1>Plugin: okplugin-plugintest1</h1>
                    <p>Wanted version from Core: {wantedVersion ? wantedVersion : "/"}</p>
                    <Button onClick={() => alert("'Hello' back from plugin 1")}>Say 'Hello'</Button>
                </div>
            )
        }
    }
}