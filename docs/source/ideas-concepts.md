# Ideas and Concepts

TODO: write about how the Landscape Omnikeeper differs from a regular CMDB, state its goals, ...

TODO: write about the more flexible approach, compared to regular CMDBs and how this is a better long-term plan in an ever-changing industry. Example: how well can regular CMDBs deal with new technologies like containerization or SaaS? Answer: badly -&gt; more flexibility is needed! Another example: how long does it take for structurally new information to enter a regular CMDB? Answer: forever! People start mis-using fields and relations to circumvent inflexible CMDB structures -&gt; more flexibility is needed!

## Elements

### CIs - _anchor_

CIs are the central element of the Omnikeeper. They are identified by an UUID (=GUID)-based ID called CI-ID or CIID.

### CI Attributes

CI Attributes add data to CIs. Each attribute is assigned to a CI, is located on a layer, is part of a changeset and has a name (TODO: write about dot-notation for grouping). Every attribute also has a value with a type (text, integer, ...). Attributes of CIs are in flux: they can be removed or have their value overwritten (without deleting the previous value) as well as new attributes can be created.

### Relations

TODO

### Predicates - _anchor_

TODO

### Changesets - _anchor_

TODO

### Layers - _anchor_

TODO

### Traits and Effective Traits

TODO

### Templates

TODO

## Groups

TODO: talk about how to use CIs + &quot;is part of group&quot; relations/predicates as a grouping mechanism

## Error Handling

TODO: talk about how to use CI-attributes for error handling, meta-attribute-group &quot;\_\_error&quot;

## Layer-based approach

A key feature for organizing the data in Omnikeeper are layers. Layers have multiple functions, such as:

*   **Separation of Concerns** - layers are used to split data into manageable chunks, for example via their source, their purpose, their governing team, human vs. process, ...
*   **Visibility** - Not every human and every process must see every layer. Only the layers that are relevant need to be processed.
*   **Priority** - when there is conflicting information regarding the same thing on different layers, a layer ordering ensures the correct resolution. Layers with a higher priority overshadow (but never overwrite!) information on the layers below. This is a similar concept to layers in image editing software, such as photoshop
*   **Change-Management** - a layer can be used as a RFC, detailing and documenting a potential change. This is called a &quot;change layer&quot;. Change managers are able to review change layers, alter or reject them, and also merge them with other layers
*   **What-if Analysis** - Create a personal layer, only visible to you, that contains changes you would like to explore. Use tools like diff, to see what would happen if applied
*   **Authorization** - Not every human and every process must be able to modify every layer.

### Types of Layers

*   **Regular layer:** writable by humans (not by processes), readable by everyone (unless restricted)
*   **Ingest layer (FMO):** only writable by a special ingest process which regularly puts data into the layer, readonly for everybody else
*   **Compute layer:** only writable by a &quot;compute layer brain&quot;, readonly for everybody else. This compute layer brain (CLB) is a process that runs regularly, can access/read data from other layers and writes data into its associated compute layer. This is a powerful mechanism, useful for a lot of automation tasks
*   **Change layer (FMO)**: like a regular layer, but its purpose is to specify and document potential changes, like in an RFC. Should be visible only to a small group of humans, and should be either merged with a regular layer or rejected and discarded.
*   **Personal layer (FMO):** like a regular layer, but only visible to a single human. Used to perform what-if analysis and other tests.

TODO: talk about sub-layers and layer-grouping, FMO

## Data Handling

### &quot;Immutable database&quot;

Data is never\* overwritten, never\* changed, never\* deleted. (\*except for maintenance/archiving purposes). This ensures nothing gets lost and offers a full view of all changes that happened and how the landscape changes over time. Vital for root cause analysis and debugging, among other things. In general, the time dimension is an important axis of landscape.

Apart from maintenance, the only database operations are SELECT and INSERT. No UPDATE or DELETE statements are ever issued.

### Traceability

Every piece of data written can be traced back to a changeset, which in turn is related to a user. A user is either a human or an automated process.

### Anchor Modelling

Useful as a basis for modeling the data in the database: [https://en.wikipedia.org/wiki/Anchor\_modeling](https://en.wikipedia.org/wiki/Anchor_modeling)

In a typical CMDB, many things change over time: attributes, relations, groups, users, configuration items, customers, ... But certain things stay pretty much solid. CI-IDs, Relation types (=predicates), ... For the elements that change over time, this gets reflected in the database: each such element that is inserted is stored with a timestamp. Retrieval is only done by providing a timestamp as well, stating for WHEN the information is requested. Getting the latest data is achieved by simply specifying a current timestamp (or one in the future).  
Elements that stay static over time are modeled like anchors in the anchor modeling technique: highly normalized, with only an identity column. Additional data that is not static, yet still refers to an anchor is modeled using a secondary table with a timestamp and foreign key references.  
Non-anchors may only ever reference (via a foreign key) anchors. References between non-anchors are not allowed. This make a lot of data consistency topics easier. As long as the anchors stay fixed, non-anchor elements can be archived/backuped/restored with ease.

### Lifecycle Management

Elements that are anchors have a livecycle management associated with them. Each anchor can be in any of the following lifecycle states:

*   Active - TODO
*   Deprecated - TODO
*   Inactive - TODO
*   Marked for Deletion - TODO

TODO

## Access Logging (FMO)

Any access to the various elements (Effective Traits, CIs, Attributes, Layer, ...) should be loggable, even read-only accesses. By doing this, administrators can reason about which elements are used by whom and how frequently. Conversely, elements that are used infrequently or not at all can be tracked down and phased out (see lifecycle management).