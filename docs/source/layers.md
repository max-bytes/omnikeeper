# Layers

DRAFT

A key feature for organizing the data in omnikeeper are layers. Layers have multiple functions, such as:

*   **Separation of Concerns** - layers are used to split data into manageable chunks, for example via their source, their purpose, their governing team, human vs. process, ...
*   **Visibility** - Not every user must see every layer. Only the layers that are relevant need to be processed.
*   **Authorization** - Not every user must be able to modify every layer.
*   **Priority** - when there is conflicting information regarding the same thing on different layers, a layer ordering ensures the correct resolution. Layers with a higher priority overshadow (but never overwrite!) information on the layers below. This is a similar concept to layers in image editing software, such as Photoshop, where layers can be used to non-destructively override pixels by drawing onto a separate layer that sits above the base layer.
*   **(In a future version) Change-Management** - a layer can be used as a RFC, detailing and documenting a potential change. This is called a &quot;change layer&quot;. Change managers are able to review change layers, alter or reject them, and also merge them with other layers
*   **(In a future version) What-if Analysis** - Create a personal layer, only visible to you, that contains changes you would like to explore. Use tools like diff, to see what would happen if applied

## Layer IDs
Layer IDs are text-based IDs used to uniquely identify each layer within an omnikeeper instance. They are used by users and processes for communicating with omnikeeper and should also be used when talking about/describing the data within omnikeeper. Layer IDs serve a double-role and should be thought of as both human- and computer readable. Because of their importance, layer IDs should be named thoughtfully  
### Naming convention 
For technical and practical reasons, layer IDs must follow a naming convention:  
Layer IDs may only contain 
- lowercase characters (a-z)
- digits (0-9)
- underscores (_)

Underscores should be used to separate words, following the [snake_case](https://en.wikipedia.org/wiki/Snake_case) convention.

## Layersets
A layerset is an ordered list of layer IDs. Layersets are used in many cases, most notably when querying data from an omnikeeper instance through the REST or GraphQL APIs. In most usecases, a layerset should not be empty and contain at least a single layer ID.

## Layer Merging
The merging of layers on access is a central concept of omnikeeper. Whenever a client reads data from omnikeeper, it needs to supply a layerset, that governs what layers should be taken into consideration and what their relative priority is. Layers specified further at the start of the layerset have a higher priority than layers specified later. Layers that are not specified at all are not considered.

Reading data using an empty layerset does not make much sense and will return no data. Reading data using a layerset containing only a single layer ID however is perfectly reasonable.

Because explaining the merging process is much easier using a picture, consider the following example:
 ![Layer Merging](assets/drawio/overview-layer-ci-attributes.svg)
Looking at the left half of the picture first, there are three layers (aptly named layer_1, layer_2 and layer_3) and three CIs (for brevity, named CI 1,2,3). Each layer is color-coded (blue, green and orange) and contains a select number of attributes and relations, also color-coded according to their associated layer. Each attribute/relation has a name (a1, a2, a3 and r1) and a value (A to G, X and Y). One way of looking at this: layers and CIs form two separate dimensions and each individual attribute/relation is positioned at the cross-section between its parent CI and its containing layer.

On the right half, two examplary queries are performed with different layersets:
* The first query uses the layerset `[layer_1, layer_2, layer_3]` and the resulting data is shown below. The data from all three layers is merged into a single set of attributes for each of the three CIs. For `CI 1`, this is possible without any conflicts, as `layer_2` defines an attribute named `a1` and `layer_2` defines an attribute named `a2` and a relation `r1`. In `CI 2` however, both `layer_1` and `layer_3` define the same relation (`r1`). In this case, the relation from `layer_1` "wins" because the layerset using for querying specifies that `layer_1` is higher priority than `layer_3`. The resulting value of relation `r1` for `CI 2` is therefore `Y` and not `X`. A similar thing happens in `CI 3`, where the attribute `a1` is taken from layer `layer_1` instead of `layer_2` and its value is therefore `B` (not `A`).

## Types of Layers

TODO: outdated

*   **Regular layer:** writable by humans (not by processes), readable by everyone (unless restricted)
*   **Ingest layer (FMO):** only writable by a special ingest process which regularly puts data into the layer, readonly for everybody else
*   **Compute layer:** only writable by a &quot;compute layer brain&quot;, readonly for everybody else. This compute layer brain (CLB) is a process that runs regularly, can access/read data from other layers and writes data into its associated compute layer. This is a powerful mechanism, useful for a lot of automation tasks
*   **Change layer (FMO)**: like a regular layer, but its purpose is to specify and document potential changes, like in an RFC. Should be visible only to a small group of humans, and should be either merged with a regular layer or rejected and discarded.
*   **Personal layer (FMO):** like a regular layer, but only visible to a single human. Used to perform what-if analysis and other tests.

TODO: talk about sub-layers and layer-grouping, FMO
