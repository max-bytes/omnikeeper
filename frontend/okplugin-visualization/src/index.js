import React from "react";
import { name as pluginName, version as pluginVersion, description as pluginDescription } from './package.json';
import { faEye } from '@fortawesome/free-solid-svg-icons';
import TraitCentricGraphRendering from "./TraitCentricGraphRendering.js";
import LayerCentricUsageGraphRendering from "./LayerCentricUsageGraphRendering.js";

export default function OKPluginVisualization() {
    return {
        menuComponents: [
            {
                title: "Trait-Centric Graph Rendering",
                url: "/trait-centric-graph-rendering",
                icon: faEye,
                component: (props) => <TraitCentricGraphRendering {...props} />
            },
            {
                title: "Layer-Centric Usage Graph Rendering",
                url: "/layer-centric-usage-graph-rendering",
                icon: faEye,
                component: (props) => <LayerCentricUsageGraphRendering {...props} />
            }
        ]
    };
}

export const name = pluginName;
export const title = "Visualization";
export const version = pluginVersion;
export const description = pluginDescription;

