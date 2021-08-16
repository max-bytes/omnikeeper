# Layers

TODO: improve

A key feature for organizing the data in omnikeeper are layers. Layers have multiple functions, such as:

*   **Separation of Concerns** - layers are used to split data into manageable chunks, for example via their source, their purpose, their governing team, human vs. process, ...
*   **Visibility** - Not every user must see every layer. Only the layers that are relevant need to be processed.
*   **Authorization** - Not every user must be able to modify every layer.
*   **Priority** - when there is conflicting information regarding the same thing on different layers, a layer ordering ensures the correct resolution. Layers with a higher priority overshadow (but never overwrite!) information on the layers below. This is a similar concept to layers in image editing software, such as photoshop
*   **Change-Management** - a layer can be used as a RFC, detailing and documenting a potential change. (TODO: This is called a &quot;change layer&quot;. Change managers are able to review change layers, alter or reject them, and also merge them with other layers)
*   **What-if Analysis** - Create a personal layer, only visible to you, that contains changes you would like to explore. Use tools like diff, to see what would happen if applied

## Layer Merging
TODO: talk about how attributes and relations are merged when working with multiple layers: merging, layer priority, ...
 ![Layer Merging](assets/drawio/overview-layer-ci-attributes-Seite-1.svg)

## Types of Layers

TODO: outdated

*   **Regular layer:** writable by humans (not by processes), readable by everyone (unless restricted)
*   **Ingest layer (FMO):** only writable by a special ingest process which regularly puts data into the layer, readonly for everybody else
*   **Compute layer:** only writable by a &quot;compute layer brain&quot;, readonly for everybody else. This compute layer brain (CLB) is a process that runs regularly, can access/read data from other layers and writes data into its associated compute layer. This is a powerful mechanism, useful for a lot of automation tasks
*   **Change layer (FMO)**: like a regular layer, but its purpose is to specify and document potential changes, like in an RFC. Should be visible only to a small group of humans, and should be either merged with a regular layer or rejected and discarded.
*   **Personal layer (FMO):** like a regular layer, but only visible to a single human. Used to perform what-if analysis and other tests.

TODO: talk about sub-layers and layer-grouping, FMO
