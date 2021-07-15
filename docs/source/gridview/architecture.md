# Architecture and Design

## Overview / Introduction

The goal of the GridView (sub-)project is to allow users of omnikeeper to view and edit the omnikeeper data in a more suitable and more efficient way than with the regular technical frontend. The general idea is that giving users an interface that is Excel datasheet-like allows for an efficient workflow. A gridview is a table-like user interface (implemented using ag-grid), where each row corresponds to a CI in omnikeeper and each column corresponds to an attribute-name. The individual cells contain the attribute-values of the attribute and CI that&#39;s defined by the row and column. A tiny example:

Consider the following CIs and their Attributes:

#### CI-A:

Attribute 1: Value A-1  
Attribute 2: Value A-2  
Attribute 3: Value A-3

#### CI-B:

Attribute 1: Value B-1  
Attribute 3: Value B-3

#### CI-C:

Attribute 1: Value C-1  
Attribute 2: Value C-2  
Attribute 3: Value C-3  
Attribute 4: Value C-4

could result in the following GridView:

<table class="docutils"><thead><tr><th>CI-Name</th><th>Attribute 1</th><th>Attribute 2</th><th>Attribute 3</th></tr></thead><tbody><tr><th>CI-A</th><td>Value A-1</td><td>Value A-2</td><td>Value A-3</td></tr><tr><th>CI-B</th><td>Value B-1</td><td>[not set]</td><td>Value B-3</td></tr><tr><th>CI-C</th><td>Value C-1</td><td>Value C-2</td><td>Value C-3</td></tr></tbody></table>

Note how CI-B does not contain an Attribute 2, which results in it being shown as "\[not set\]" in the GridView. Also note that CI-C contains an Attribute with name &quot;Attribute-4&quot;, which is not shown in the GridView. This is because the configuration of this (examplary) GridView explicitly defined to show Attributes named Attribute 1-3, which implicitly means NOT Attribute 4.

## Configuration and Context

A GridView's configuration is a dataset that defines how a GridView behaves, how it looks and which data it shows and how. Important examples of what is part of a GridView's configuration are:

*   which columns it shows
*   which CIs are part of the data
*   which LayerSet is used when fetching data from the omnikeeper core

Multiple configurations can be used within the same omnikeeper instance to allow for different GridViews that fulfill different needs and usecases.

Sample configuration (in pseudo-YAML, does not mean that the config should ever be in YAML):

```YAML
readLayerset: [2,3,1] # layerset from which to read the omnikeeper data
showCIIDColumn: true # whether or not to show a column 
writeLayer: 1 # is a default write layer, if not set for a column
columns:
 - sourceAttributeName: "hostname"
   columnDescription: "Hostname"
 - sourceAttributeName: "cpu_architecture"
   columnDescription: "CPU Architecture"
   writeLayer: 2
 - sourceAttributeName: "os_family"
   columnDescription: "OS Family"
   writeLayer: null # means whole column is read-only
trait: "host" # single trait that CIs are required to fulfill
```

A GridView configuration, together with a name defines a so-called Context. Contexts are used to differentiate GridViews from each other; for users, in code and for the REST API. Contexts can later be used to add authorization mechanisms, but this is out of scope (for now).

## CI Selection

A GridView generally should not show ALL CIs that are present in an omnikeeper instance. Rather, filtering is done using a &quot;Trait&quot;. Only CIs that fulfill/have this trait are shown in the GridView. 

## Read- and Write Layers

The configuration of a GridView contains a Layerset, specifying which layers in which priority should make up the source data. Additionally, a default write-layer must be specified, which determines where the changes the user makes are written to. This write-layer can also be overwritten per column. Setting a write-layer configuration to null means that the column/the full GridView should be made read-only.

There is a special case that needs to be taken into account: if the configured write-layer for a cell is NOT the same as the top-most read-layer AND the top-most read-layer contains data for this cell, the cell itself (not the whole column!) must be made read-only. If this is not implemented, the user could make changes to this cell and write to the configured layer, but would not see the changes because the layer above hides it and shows its own data instead. For this reason, both the frontend and the backend need to support individual read-only cells, not only read-only columns.

## Changes

When the user changes attributes and chooses to save them, ALL changes are sent to the backend in a single REST call. The backend needs to do a consistency validation of each of the changed CIs. Only if ALL changed CIs pass this validation, the changes should be saved. If at least one does not pass, the FULL change needs to be reverted and no modification should be done to the omnikeeper data. The consistency validation per CI consists of checking whether or not the CI still fulfills/has the configured trait.

If the change fails, the backend should return proper and human-readable error messages, so the user knows what went wrong and how they can fix it. There must (at least) be the possibility to have error messages per CI, so the frontend can assign the error messages to the corresponding row and show them there.

If the change succeeds, the backend should return the full data of all the CIs that have been changed. The frontend must take this new data and fully incorporate it/update its state accordingly.  This looks like it wouldn&#39;t make a difference, because the frontend already has these changes (the user made them via the frontend after all). The reason for doing it is that there are sometimes small, but important differences between the way the frontend displays cells and how the backend stores attribute values.

## Empty Attribute vs. not-set Attribute

Frontend and backend need to be aware of the difference between an empty attribute value (f.e. type is scalar-text, value is empty string &quot;&quot;) and a not-set Attribute, where the Attribute is not present at all for a CI. For an empty Attribute value, the frontend should show it as empty, with an empty cell. Not-set Attributes should have their cell be displayed as &quot;\[not set&quot;\].

Editing both empty and not-set Attributes/cells should start off with an empty string. BUT, when a user edits an not-set cell and ends editing while keeping the empty string, the cell must NOT be converted to an empty Attribute. Also make sure that when performing changes/saving, not-set Attributes are not implicitly converted to empty Attributes. Design the change-DTOs carefully.

Special case 1: the user wants to &quot;delete&quot; an attribute, transforming it from set (containing any value) to not-set. A custom UI interface is needed for this as the regular ag-grid cell editors do not have that functionality. Note that deleting an attribute might lead to a lower layer to shine through and display its data instead.

Special case 2: an attribute is not-set, but the user wants to convert it to an empty attribute. Without making any changes to accomodate this usecase, the user can only achieve this with a two step workflow: first, they need to write anything (a non-empty string) into the cell. Then they need to save, transforming the not-set Attribute into a set one. Then they can delete the text in the cell again, making it an empty Attribute.  Because this is a very rare case, we will leave it at that for now.

## Datatypes

omnikeeper supports different datatypes for Attribute values: Text, Integer, YAML, JSON, ... Additionally, each datatype can be either scalar or in array-form. For the first iterations of GridView, we will only support scalar-text as the only datatype. When other datatypes are encountered within the data, they will need to be converted for displaying. Operations that modify a non-scalar-text Attribute value effectively change the datatype to scalar-text. While support for different datatypes should not be implemented right now, the requirement will surely come at some point in the future and the codebase should be created with that in mind.

## Architecture

Communication over the structure of the REST API interface should happen fully over Swagger. In our case, this means that the backend creates the interface itself, from which a Swagger definition is automatically created. The frontend should use the swagger definition to build its API client. See [https://github.com/swagger-api/swagger-js](https://github.com/swagger-api/swagger-js) (library for using Swagger with JS).

```eval_rst
.. drawio-figure:: overview-gridview-architecture.drawio
   :export-scale: 100
```

DTO structures: see [https://www.mhx.at/gitlab/landscape/registry/snippets/1](https://www.mhx.at/gitlab/landscape/registry/snippets/1)

## (Additional) Technologies

### Frontend:

ag-grid  
swagger-js: [https://github.com/swagger-api/swagger-js](https://github.com/swagger-api/swagger-js)  
try/evaluate: https://ant.design/

### Backend:

as is already used (.Net Core, REST API, ...)
