import React from "react";
import { name as pluginName, version as pluginVersion, description as pluginDescription } from './package.json';
import { faEye } from '@fortawesome/free-solid-svg-icons';
import GraphRendering from "./GraphRendering.js";

export default function OKPluginVisualization() {
    return {
        menuComponents: [
            {
                title: "Graph Rendering",
                url: "/graph-rendering",
                icon: faEye,
                component: (props) => <GraphRendering {...props} />
            }
        ]
    };
}

export const name = pluginName;
export const title = "Visualization";
export const version = pluginVersion;
export const description = pluginDescription;

