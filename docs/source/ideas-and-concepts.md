# Ideas and Concepts

TODO: write about how omnikeeper differs from a regular CMDB, state its goals, ...

TODO: write about the more flexible approach, compared to regular CMDBs and how this is a better long-term plan in an ever-changing industry. Example: how well can regular CMDBs deal with new technologies like containerization or SaaS? Answer: badly -&gt; more flexibility is needed! Another example: how long does it take for structurally new information to enter a regular CMDB? Answer: forever! People start mis-using fields and relations to circumvent inflexible CMDB structures -&gt; more flexibility is needed!

## Elements of omnikeeper

### CIs

CIs are the central element of omnikeeper. They are identified by an UUID (=GUID)-based ID called CI-ID or CIID. CIs themselves contain nothing more than that. The way to add information to CIs is by adding attributes and relations.

### Attributes

Attributes add data to CIs. Each attribute is assigned to a CI, is located on a layer, is part of a changeset and has a name. Every attribute also has a value with a type (text, integer, ...). Attributes of CIs are in flux: they can be removed or have their value overwritten (without deleting the previous value) as well as new attributes can be created.

TODO: write about dot-notation for grouping

### Relations

Relations are used to relate exactly two CIs to each other. Relations are directed, that means a relation has a start and an end, or a "from" and a "to" half. You can think of a relation as a directed link from one CI to another.  
A relation is further specified by a "predicate", which defines the nature of the relation. In a relation, you only specify the predicate-ID, which is a unique string referring to the predicate itself.  
An example of a relation might be to define a parent-child relationship between two CIs. In that case, it might make sense to use the predicate-ID `is_child_of` and make it "go" from the child-CI to the parent-CI.

### Predicates

A relation itself only specifies a predicate-ID, but does not define the predicate itself further. To add additional information to a predicate, you may use the management interface in the technical frontend.

### Changesets - _anchor_

Every change to omnikeeper's attributes and relations is kept track of inside of changesets. A changeset is a collection of attribute- and relation-changes. A changeset also is linked to the user who made the change.

### Layers

[[Link|layers]]

### Traits and Effective Traits

TODO

## Data Handling

### &quot;Immutable database&quot;

Data is never\* overwritten, never\* changed, never\* deleted. (\*except for maintenance/archiving purposes). This ensures nothing gets lost and offers a full view of all changes that happened and how the landscape changes over time. Vital for root cause analysis and debugging, among other things. In general, the time dimension is an important axis of omnikeeper.

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