# Attributes

Attributes are the central building block for storing data in omnikeeper. Each attribute has a name and a value. While the name is a simple text string, the value is more complex, has a data type and stores the actual data. Each attribute is placed in exactly one CI and exactly one layer.
At every moment in time, an attribute is uniquely identified throughout an omnikeeper instance when looking at its name, its CI and its layer. In other words, there cannot be more than one attribute with the same name, CI and layer (at a particular point in time). There can (and typically will) be more than one attribute with the same name at different CIs and/or different layers.

## Attribute Name

Each attribute needs a name. To support as many usecases as possible, attribute names do no have any restrictions in terms of allowed characters or length (other than not being empty). Given that omnikeeper often needs to import foreign data, letting attributes have any name was an important goal to make integration as painless as possible. It allows importers to keep the original names intact, such as the column names of database tables or the keys/paths in a structured data format such as JSON or XML.

However, if you do not value the ability to keep the original names but want to use a consistent naming convention, you might benefit from following these recommendations, even when there are no strictly enforced naming rules:
- stick to lowercase characters and numbers, avoid uppercase or special characters.
- underscores ("_") should be used to separate words.
- dots (".") should be used as a hierarchy and grouping mechanism to pool similar attributes together. The dot does have a semantic meaning within omnikeeper's technical frontend. It will group attributes with the same prefix together and display them under a common group name.
- in general, follow the [snake_case](https://en.wikipedia.org/wiki/Snake_case) convention.

## Attribute Value

TODO

### Attribute Value Types

TODO

### Attribute Value Arrays

## Special attributes
### __name

TODO

## Attributes over time
omnikeeper stores attributes with time in mind. In addition to accessing the currently active attributes, omnikeeper allows users to look at attributes at earlier points in time as well. This is made possible by storing all earlier "versions" of every attribute that omnikeeper knows about.

Each attribute has a timestamp at which it was created. If a new attribute is added with the same name, CI and layer as an existing attribute, the new attribute effectively overwrites the old one. However, the old attribute is not totally gone, it merely stops being the active attribute for this combination of name, CI and layer at the timestamp when the new attribute is added. Querying omnikeeper with a timestamp before the new attribute was added would still yield the old attribute.

There is a mechanism to limit how long (outdated) attributes are stored. In the base configuration, a time threshold can be set. Attributes that are older than this threshold AND are not currently active will be deleted by a regularly running process. Setting this threshold to 0 effectively disables omnikeeper's history and only keeps the latest set of attributes.

The following diagram shows how attributes appear over time:

 ![Attributes over time](assets/drawio/overview-attributes-time.svg)

Depending on what timestamp is used for querying, a different set of attributes would be returned. When querying for the latest timestamp, the returned attribute values would be "1", "3", "7" and "5" (in order of diagram columns, left-to-right). But when querying for an earlier point in time, attributes added later would not appear in the result and older attributes would show instead.

Note how in the third column, the attribute with value "7" is not eligible for deletion even though it is older than the threshold. This is because it is also the latest version of this attribute, making it a currently active attribute and hence, not eligible for deletion.

## Attributes and Changesets
Each attribute is part of exactly one changeset. Because changes to attributes are done by inserting new ("versions" of) attributes, changes can only occur through a changeset (exception: online layers, generators). 

Because attributes are stored over time, but deleted when they become outdated and too old, this leads to changesets shrinking in their apparent number of changes over time. Because with time, more and more attributes of a changeset become eligible for deletion, the changeset will contain less and less attribute changes, until it does not contain any changes anymore. At that point, the changeset will be deleted as well. However, as long as there is at least a single attribute (or relation) currently active, its changeset will continue to exist.
