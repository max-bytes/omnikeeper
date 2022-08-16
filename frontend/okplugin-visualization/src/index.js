import React from "react";
import { name as pluginName, version as pluginVersion, description as pluginDescription } from './package.json';
import { faEye } from '@fortawesome/free-solid-svg-icons';
import TraitCentricGraphRenderingGraphViz from "./TraitCentricGraphRenderingGraphViz.js";
import LayerCentricUsageGraphRendering from "./LayerCentricUsageGraphRendering.js";
import TraitCentricGraphRenderingCytoscape from "./TraitCentricGraphRenderingCytoscape.js";
import RelationGraphRendering from "./RelationGraphRendering";
import ReadWriteGraphRendering from "./ReadWriteGraphRendering";

export default function OKPluginVisualization() {
    return {
        menuComponents: [
            {
                title: "Trait-Centric Graph Rendering (GraphViz, Beta)",
                url: "/trait-centric-graph-rendering-graphviz",
                icon: faEye,
                component: (props) => <TraitCentricGraphRenderingGraphViz {...props} />
            },
            {
                title: "Trait-Centric Graph Rendering (Cytoscape, Beta)",
                url: "/trait-centric-graph-rendering-cytoscape",
                icon: faEye,
                component: (props) => <TraitCentricGraphRenderingCytoscape {...props} />
            },
            {
                title: "Layer-Centric Usage Graph Rendering (Beta)",
                url: "/layer-centric-usage-graph-rendering",
                icon: faEye,
                component: (props) => <LayerCentricUsageGraphRendering {...props} />
            },
            {
                title: "Relation Graph Rendering (Beta)",
                url: "/relation-graph-rendering",
                icon: faEye,
                component: (props) => <RelationGraphRendering {...props} />
            },
            {
                title: "Read-Write Graph Rendering (Beta)",
                url: "/read-write-graph-rendering",
                icon: faEye,
                component: (props) => <ReadWriteGraphRendering {...props} />
            }
        ]
    };
}

export const name = pluginName;
export const title = "Visualization";
export const version = pluginVersion;
export const description = pluginDescription;

