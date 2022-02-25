# Layers

DRAFT

A key feature for organizing the data in omnikeeper are layers. Layers have multiple functions, such as:

*   **Separation of Concerns** - layers are used to split data into manageable chunks, for example via their source, their purpose, their governing team, human vs. process, ...
*   **Visibility** - Not every user must see every layer. Only the layers that are relevant need to be processed.
*   **Authorization** - Not every user must be able to modify every layer.
*   **Priority** - when there is conflicting information regarding the same thing on different layers, a layer priority ensures the correct resolution. Layers with a higher priority overshadow (but never overwrite!) information on the layers below. This is a similar concept to layers in image editing software, such as Photoshop, where layers can be used to non-destructively override areas of the canvas by drawing onto a separate layer that sits above the base layer.
*   **(In a future version) Change-Management** - a layer can be used as an RFC, detailing and documenting a potential change. This is called a &quot;change layer&quot;. Change managers are able to review change layers, alter or reject them, and also merge them with other layers
*   **(In a future version) What-if Analysis** - Create a personal layer, only visible to you, that contains changes you would like to explore. Use tools like diff, to see what would happen if applied

## Layer IDs
Layer IDs are text-based IDs used to uniquely identify each layer within an omnikeeper instance. They are used by clients for communicating with omnikeeper and should also be used when talking about/describing the data within omnikeeper. Layer IDs serve a double-role and should be thought of as both human- and computer readable. Because of their importance, layer IDs should be named thoughtfully.

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
omnikeeper allows clients querying data from multiple layers in a single query. Client can request the data they are interested in and compose layers on-the-fly. The result set they get is compressed and conflicts between layers are resolved. The process of combining data from multiple layers is called "layer merging" and is a central concept of omnikeeper.

Whenever a client reads data from omnikeeper, it supplies a layerset that governs what layers should be taken into consideration and what their relative priority is. Layers specified further at the start of the layerset have a higher priority than layers specified later. Layers that are not specified at all are not considered. 

Reading data using an empty layerset does not make much sense and will return no data. Reading data using a layerset containing only a single layer ID however is perfectly reasonable.

Because explaining the merging process is much easier using a diagram, consider the following example:

 ![Layer Merging](assets/drawio/overview-layer-ci-attributes.svg)

Looking at the left half of the picture first, there are three layers (aptly named layer_1, layer_2 and layer_3) and three CIs (for brevity, named CI 1, CI 2 and CI 3). Each layer is color-coded (blue, green and orange) and contains a select number of attributes and relations, also color-coded according to their associated layer. Each attribute/relation has a name (a1, a2, a3 and r1) and a value (A to G, X and Y). One way of looking at this diagram that might help your understanding of layers: layers and CIs form two separate dimensions and each individual attribute/relation is positioned at the cross-section between its parent CI and its containing layer.

On the right half, two examplary queries are performed with different layersets:
* The first query uses the layerset `[layer_1, layer_2, layer_3]` and the resulting data is shown below. The data from all three layers is merged into a single set of attributes for each of the three CIs. For `CI 1`, this is possible without any conflicts, as `layer_2` defines an attribute named `a1` and `layer_2` defines an attribute named `a2` and a relation `r1`. In `CI 2` however, both `layer_1` and `layer_3` define the same relation (`r1`). In this case, the relation from `layer_1` "wins" because the layerset used for querying specifies that `layer_1` is higher priority than `layer_3`. The resulting value of relation `r1` for `CI 2` is therefore `Y` and not `X`. A similar thing happens in `CI 3`, where the attribute `a1` is taken from layer `layer_1` instead of `layer_2` and its value is therefore `B` (not `F`).
* The second query uses layerset `[layer_2, layer_1]`. First of all, this query shows that layers can be completely ignored by not specifying them in the layerset. `layer_3` was simply not specified, so no attributes/relations from this layer are present in the result set. Secondly, it shows the difference of changing the ordering of `layer_1` and `layer_2` in the layerset. Because of this switch, `CI 3` has a value of `F` for attribute `a1` in this result set, in contrast to the first query, where it has the value `B`.

As the example shows, the resolution of conflicts is done by looking at pairs of attributes/relations (=same name and part of the same CI) that are present in more than one requested layer and choosing the attribute/relation in the layer with the highest priority.

An important fact of layer merging is that the merging happens at the time a request is made. There is no pre-configured set of "valid" layersets that can be queried while others can't be. Each client can - given the appropriate permissions - query data using any layerset they specify. Clients can choose to opt-in/-out of layers simply by adding/removing them from the layerset they use for querying.

## Compute Layers
See [[Compute Layers|compute-layers]].

## Layers and Mutations

When writing attributes and relations to a layer, what omnikeeper actually does in the end depends on a few factors, such as what data is already present in the write layer, what data is present in the layers below and above and a few "handling" settings.

The table below shows how writing an attribute to a CI has different outcomes depending on the mentioned factors. A few notes:

* while the inital request is always to write or delete a specific attribute, the resulting operation can differ a lot. The main intention is that omnikeeper tries to fulfill the request as best as possible, while still considering the "surrounding" layers.
* the list is not exhaustive, but should cover all important scenarios
* a `*` means "any value"
* relations are not depicted here, but behave similarly to attributes
* Mask Handling: turning on mask handling (ApplyMaskIfNecessary) for deletions makes omnikeeper aware of the layers below the write layer and apply a mask to properly hide the lower layer's data if it is necessary. Without this, the lower layer's data would "shine through" and be visible after the deletion. Depending on the use case, this might or might not be intended.
* Other Layers Value Handling: turning on other-layers-value-handling (TakeIntoAccount) for writes makes omnikeeper aware of the "surrounding" layers. If a surrounding layer already contains the same attribute with the same value, no write operation is performed. In fact, an existing attribute in the write layer would be deleted even, to reduce the duplication and make the other layer's value take priority. Depending on the use case, this might or might not be intended.


| New Attribute |     Mask Handling    | Other Layers Value Handling |   | Layer(s) above | Write Layer | Layer(s) below | Resulting Operation | Layer(s) above after | Write Layer after | Layer(s) below after |
|:--------------------:|:--------------------:|:---------------------------:|---|:--------------:|:-----------:|:--------------:|:-------------------:|:--------------------:|:-----------------:|:--------------------:|
|        Value Z       |          \*          |              \*             |   |   \[NotSet\]  | \[NotSet\] |   \[NotSet\]  |        Write        |      \[NotSet\]     |      Value Z      |      \[NotSet\]     |
|        Value Z       |          \*          |              \*             |   |   \[NotSet\]  |   Value A   |   \[NotSet\]  |        Write        |      \[NotSet\]     |      Value Z      |      \[NotSet\]     |
|        Value Z       |          \*          |              \*             |   |   \[NotSet\]  |   Value Z   |   \[NotSet\]  |        No-op        |      \[NotSet\]     |      Value Z      |      \[NotSet\]     |
|        Value Z       |          \*          |              \*             |   |   \[NotSet\]  | \[NotSet\] |     Value A    |        Write        |      \[NotSet\]     |      Value Z      |        Value A       |
|        Value Z       |          \*          |          ForceWrite         |   |   \[NotSet\]  | \[NotSet\] |     Value Z    |        Write        |      \[NotSet\]     |      Value Z      |        Value Z       |
|        Value Z       |          \*          |       TakeIntoAccount       |   |   \[NotSet\]  | \[NotSet\] |     Value Z    |        No-op        |      \[NotSet\]     |    \[NotSet\]    |        Value Z       |
|        Value Z       |          \*          |       TakeIntoAccount       |   |   \[NotSet\]  |   Value A   |     Value Z    |        Delete       |      \[NotSet\]     |    \[NotSet\]    |        Value Z       |
|        Value Z       |          \*          |       TakeIntoAccount       |   |   \[NotSet\]  |   Value Z   |     Value Z    |        Delete       |      \[NotSet\]     |    \[NotSet\]    |        Value Z       |
|        Value Z       |          \*          |          ForceWrite         |   |     Value Z    | \[NotSet\] |   \[NotSet\]  |        Write        |        Value Z       |      Value Z      |      \[NotSet\]     |
|        Value Z       |          \*          |       TakeIntoAccount       |   |     Value Z    | \[NotSet\] |   \[NotSet\]  |        No-op        |        Value Z       |    \[NotSet\]    |      \[NotSet\]     |
|        Value Z       |          \*          |              \*             |   |     Value A    | \[NotSet\] |   \[NotSet\]  |        Error        |        Value A       |    \[NotSet\]    |      \[NotSet\]     |
|      \[NotSet\]     |          \*          |              \*             |   |   \[NotSet\]  | \[NotSet\] |   \[NotSet\]  |        No-op        |      \[NotSet\]     |    \[NotSet\]    |      \[NotSet\]     |
|      \[NotSet\]     |          \*          |              \*             |   |   \[NotSet\]  |   Value A   |   \[NotSet\]  |        Delete       |      \[NotSet\]     |    \[NotSet\]    |      \[NotSet\]     |
|      \[NotSet\]     |        NoMask        |              \*             |   |   \[NotSet\]  |   Value B   |     Value A    |        Delete       |      \[NotSet\]     |    \[NotSet\]    |        Value A       |
|      \[NotSet\]     | ApplyMaskIfNecessary |              \*             |   |   \[NotSet\]  |   Value B   |     Value A    |         Mask        |      \[NotSet\]     |      \[Mask\]     |        Value A       |
|      \[NotSet\]     |          \*          |              \*             |   |     Value A    | \[NotSet\] |   \[NotSet\]  |        Error        |        Value A       |    \[NotSet\]    |      \[NotSet\]     |

## (OUTDATED) Types of Layers

*   **Regular layer:** writable by humans (not by processes), readable by everyone (unless restricted)
*   **Ingest layer (FMO):** only writable by a special ingest process which regularly puts data into the layer, readonly for everybody else
*   **Compute layer:** only writable by a &quot;compute layer brain&quot;, readonly for everybody else. This compute layer brain (CLB) is a process that runs regularly, can access/read data from other layers and writes data into its associated compute layer. This is a powerful mechanism, useful for a lot of automation tasks
*   **Change layer (FMO)**: like a regular layer, but its purpose is to specify and document potential changes, like in an RFC. Should be visible only to a small group of humans, and should be either merged with a regular layer or rejected and discarded.
*   **Personal layer (FMO):** like a regular layer, but only visible to a single human. Used to perform what-if analysis and other tests.

TODO: talk about sub-layers and layer-grouping, FMO
