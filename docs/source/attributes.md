# Attributes

DRAFT

Attributes are the central building block for storing data in omnikeeper. Each attribute has a name and a value. While the name is a simple text string, the value is more complex and stores the actual data. Each attribute is placed in exactly one CI and exactly one layer.
At every moment in time, an attribute is unique throughout an omnikeeper instance when looking at its name, its CI and its layer. In other words, there cannot be more than one attribute with the same name, CI and layer (at a particular point in time). There can (and typically will) be more than one attribute with the same name at different CIs and/or different layers.

## Attributes over time
omnikeeper stores attributes with time in mind. In addition to accessing the currently active attributes, omnikeeper allows users to look at attributes at earlier points in time as well. This is made possible by storing all earlier "versions" of every attribute that omnikeeper knows about.

Each attribute has a timestamp at which it was created. If a new attribute is added with the same name, CI and layer as an existing attribute, the new attribute effectively overwrites the old one. However, the old attribute is not totally gone, it merely stops being the active attribute for this combination of name, CI and layer at the timestamp when the new attribute is added. Querying omnikeeper with a timestamp before the new attribute was added would still yield the old attribute.

There is a mechanism to limit how long (outdated) attributes are stored. In the base configuration, a time threshold can be set. Attributes that are older than this threshold AND are not currently active will be deleted by a regularly running process. Setting this threshold to 0 effectively disables omnikeeper's history and only keeps the latest set of attributes.

The following diagram shows how attributes appear over time:

 ![Attributes over time](assets/drawio/overview-attributes-time-Seite-1.svg)

Depending on what timestamp is used for querying, a different set of attributes would be returned. When querying for the latest timestamp, the returned attribute values would be "1", "3", "7" and "5" (in order of diagram columns, left-to-right). But when querying for an earlier point in time, attributes added later would not appear in the result anymore and older attributes would show instead.

Note how in the third column, the attribute with value "7" is not eligible for deletion even though it is older than the threshold. This is because it is also the latest version of this attribute, making it a currently active attribute and hence, not eligible for deletion.

## Attributes and Changesets
Each attribute is part of exactly one changeset. Because changes to attributes are done by inserting new ("versions" of) attributes, changes can only occur through a changeset (exception: online layers, generators). 

Because attributes are stored over time, but deleted when they become outdated and too old, this will lead to changesets shrinking in their apparent number of changes over time. Because with time, more and more attributes of a changeset become eligible for deletion, the changeset will contain less and less attribute changes, until it does not contain any changes anymore. At that point, the changeset will be deleted as well. However, as long as there is at least a single attribute (or relation) currently active, its changeset will continue to exist.

## Attribute Name

TODO: nomenclature


## Attribute Value

TODO