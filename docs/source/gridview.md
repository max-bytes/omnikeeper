# Gridview

The gridview feature allows omnikeeper users to view and edit omnikeeper data in a more suitable and efficient way than with the regular technical frontend. It is well suited 
- for creating simple interfaces for viewing and modifying data
- for creating table-based data reports
- as the export interface for doing further data processing outside of omnikeeper
- as a rapid application development tool

 The general idea is that giving users an interface that is Excel datasheet-like allows for an efficient workflow when working with data. A gridview is a table-like user interface, where each row  corresponds to a CI in omnikeeper and each column corresponds to an attribute of that CI (with exceptions). The individual cells contain the values of the attributes of each CI. Using a table for data representations allows users to view a lot of data efficiently, sort, filter and search based on their needs, and it provides a well-known data structure that's compatible with a lot of BI and data management tools.
 
## Example

Consider the following CIs and their attributes (Note: for brevity, the attributes are shown in a denormalized table and the CI-IDs are shortened):

<table class="docutils"><thead><tr><th>CI-ID</th><th>Attribute Name</th><th>Attribute Value</th></tr></thead><tbody>
<tr><td>CI-A</td><td>Attribute 1</td><td>Value A-1</td></tr>
<tr><td>CI-A</td><td>Attribute 2</td><td>Value A-2</td></tr>
<tr><td>CI-A</td><td>Attribute 3</td><td>Value A-3</td></tr>
<tr><td>CI-B</td><td>Attribute 1</td><td>Value B-1</td></tr>
<tr><td>CI-B</td><td>Attribute 3</td><td>Value B-3</td></tr>
<tr><td>CI-C</td><td>Attribute 1</td><td>Value C-1</td></tr>
<tr><td>CI-C</td><td>Attribute 2</td><td>Value C-2</td></tr>
<tr><td>CI-C</td><td>Attribute 3</td><td>Value C-3</td></tr>
<tr><td>CI-C</td><td>Attribute 4</td><td>Value C-4</td></tr>
</tbody></table>

A configured gridview could look as follows:

<table class="docutils"><thead><tr><th>CI-ID</th><th>Attribute 1</th><th>Attribute 2</th><th>Attribute 3</th></tr></thead><tbody><tr><th>CI-A</th><td>Value A-1</td><td>Value A-2</td><td>Value A-3</td></tr><tr><th>CI-B</th><td>Value B-1</td><td>[not set]</td><td>Value B-3</td></tr><tr><th>CI-C</th><td>Value C-1</td><td>Value C-2</td><td>Value C-3</td></tr></tbody></table>

Note how CI-B does not contain an Attribute 2, which results in it being shown as "\[not set\]" in the Gridview. Also note that CI-C contains an attribute with name &quot;Attribute-4&quot;, which is not shown in the Gridview. This is because the configuration of this (examplary) Gridview explicitly only defined to show attributes named Attribute 1, Attribute 2 and Attribute 3, which implicitly means NOT Attribute 4.

## Configuration and Contexts

A gridview's configuration defines how it behaves, how it looks and which data it shows and how. The main parts of a gridview's configuration define:

* which columns it shows
* which CIs are part of the data
* which layer set is used when fetching data from the omnikeeper core
* which layer the data is written to when users change cells in the gridview

A gridview configuration, together with a unique ID, a name and a description form a so-called gridview "context". Multiple contexts can exist within the same omnikeeper instance to allow for different gridviews that fulfill different needs and usecases. The context ID is used to differentiate gridviews from each other and to address them in code and in the REST API.

### Overall Configuration
- show CIID column: whether or not to show a special column at the beginning of the grid that displays the CI-ID of each row
- writeLayer: 
- read layerset and write layer: the base layer set that is used for fetching the data from the omnikeeper core and the (default) layer where to write changes in the gridview cells to. This write-layer can also be overwritten per column. Setting a write-layer configuration to an empty string means that the column/the full GridView should be made read-only.
- trait: a gridview generally does not display ALL CIs that are present in an omnikeeper instance. Rather, filtering is done using a trait. Only CIs that fulfill/have this trait are shown in the gridview. 

### Column Definitions
Central to a gridview's configuration is the definition of its columns. Per each column, you define:
- the source attribute name: specifies the name of the attribute that is shown in this column
- the column's description: determines what text to show in the header of the column
- the source attribute path: optional; gridview supports showing attributes of CIs that are related to the "base CI" via a relation.
- the layer where to write changes in this column to: optional; set to an empty string to disable writing / make this column read-only. If not set, it uses the config setting in the overall configuration.

## Practical Gridview Configuration
At the time of writing, gridview contexts are managed via the technical frontend at `/grid-view`. Click on "Add New Context" to create a new one or edit an already existing gridview. A gridview context is fully specified via a single JSON document. A very basic example context could look like the following:
```
{
  "id": "test_gridview_context",
  "speakingName": "Test Gridview Context",
  "description": "This is an example context for showing how a gridview context is configured",
  "configuration": {
    "showCIIDColumn": true,
    "writeLayer": "layer01",
    "readLayerset": [
      "layer01"
    ],
    "columns": [
      {
        "sourceAttributeName": "__name",
        "columnDescription": "Name of CI"
      }
    ],
    "trait": "test_trait"
  }
}
```
This setup defines a gridview context with ID `test_gridview_context`, selects the CIs that fulfill trait `test_trait` in the layerset `layer01`, and shows their CI-IDs (because of `"showCIIDColumn": true,`) and then the name of the CI in a column. For a detailed documentation of how this JSON document is structured, [refer to its corresponding .Net object](https://github.com/max-bytes/omnikeeper/blob/master/backend/Omnikeeper/GridView/Entity/GridViewConfiguration.cs) in the source code (from/to which the JSON is serialized).

## Performing Data Changes

Due to the way gridviews are set up, each CI/row that is displayed is guaranteed to fulfill the configured trait. When performing data changes, for each changed CI/row, the trait is checked again after the change. If a CI/row does NOT fulfill the trait anymore, the change is rejected and the user is presented an error message. Make sure to consider that when designing traits and gridviews.

### Special case for cell write handling
There is a special case that needs to be taken into account regarding the "writability" of cells: if the configured write-layer for a cell is NOT the same as the top-most read-layer AND the top-most read-layer contains data for this cell, the cell itself (not the whole column!) is made read-only. If this would not be implemented like this, the user could make changes to this cell and write to the configured layer, but would not see the changes because the layer above hides it and shows its own data instead. Hence, cells that have this configuration are read-only.

## Empty Attribute vs. not-set Attribute

Note that there is a difference between
- a set attribute where the attribute value is empty (.e. type is scalar-text, value is empty string &quot;&quot;): the cell is displayed as an empty string in the gridview.
- a not-set attribute, where the attribute is not set at all in a CI: the cell shows the text &quot;\[not set\]&quot;.

Because of this distinction, there are a few peculiarities:
- when editing, both empty and not-set attributes/cells start off with an empty string. BUT, when a user edits a not-set cell and ends editing while keeping the empty string, the cell is NOT converted to an empty attribute. 
- when a user wants to delete an attribute, modifying it from set to not-set in the UI, they need to select the cell(s) and click on the "Set cell to '[not set]'" button.
- an attribute is not-set, but the user wants to convert it to an empty(!) attribute. Without making any changes to accomodate this usecase, the user can only achieve this with a two step workflow: first, they need to write anything (=a non-empty string) into the cell. Then they need to save, transforming the not-set attribute into a set one. Then they can delete the text in the cell again, making it an empty attribute. Because this is a very rare case, this cumbersome workflow is kept for now.

## Datatypes

omnikeeper supports different datatypes for attribute values: Text, Integer, YAML, JSON, ... Additionally, each datatype can be either scalar or in array form. At the time of writing, gridview only supports scalar attribute values and also converts all other datatypes to the omnikeeper datatype text.

## Related Attributes

Gridview columns are capable of displaying attributes that are not part of the "base CI" (=the CI that corresponds to the current row in the gridview), but instead of CIs that are related to the base CI via an omnikeeper relation. For example, to show an attribute with attribute name `Attribute X`, that is present in a CI that is related to the base CI via a relation with predicate ID `is_related_to`, you could define the following column configuration:
```
...
{
    "sourceAttributeName": "Attribute X",
    "sourceAttributePath": [ "<", "is_related_to" ],
    "columnDescription": "Attribute X of related CI",
    "writeLayer": "",
}
...
```
The key piece here is the line `"sourceAttributePath": [ "<", "runs_on" ]`. This two-element array defines that the attribute is not found in the base CI, but instead, omnikeeper should follow an incoming relation (`<` means incoming, `>` outgoing) with predicate ID `is_related_to` to another CI and look for the attribute `Attribute X` there.

If there is no fitting relation or the relation is there, but the CI does not have a correctly named attribute, the resulting cell will be set to &quot;\[not set\]&quot;.

omnikeeper supports multiple relations with the same predicate ID going out from or coming in to the same CI. That means there could be more than one fitting relation per base CI. In these cases, omnikeeper follows the relation where the related CI has the lowest CI-ID. While this sounds strange at first, it at least makes sure that the process of selecting the related CI is somewhat stable.

At the time of writing, there is no support for writing to related attributes. Trying to write to a related attribute in a gridview cell results in an error.

