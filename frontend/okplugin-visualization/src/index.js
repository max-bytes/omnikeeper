import React from "react";
import { name as pluginName, version as pluginVersion, description as pluginDescription } from './package.json';
import { faEye } from '@fortawesome/free-solid-svg-icons';
import TraitCentricGraphRenderingGraphViz from "./TraitCentricGraphRenderingGraphViz.js";
import LayerCentricUsageGraphRendering from "./LayerCentricUsageGraphRendering.js";
import TraitCentricGraphRenderingCytoscape from "./TraitCentricGraphRenderingCytoscape.js";

export default function OKPluginVisualization() {
    return {
        menuComponents: [
            {
                title: "Trait-Centric Graph Rendering (GraphViz)",
                url: "/trait-centric-graph-rendering-graphviz",
                icon: faEye,
                component: (props) => <TraitCentricGraphRenderingGraphViz {...props} />
            },
            {
                title: "Trait-Centric Graph Rendering (Cytoscape)",
                url: "/trait-centric-graph-rendering-cytoscape",
                icon: faEye,
                component: (props) => <TraitCentricGraphRenderingCytoscape {...props} />
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

