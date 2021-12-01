# Generators

Generators are a way to create new attributes out of existing ones. Very similar to "calculated fields" (in Excel for example), generators use "templates" (or formulas) to calculate new attribute values and add them besides the existing attributes. In a generator template, you can reference other attributes of the CI and use them for calculations.

Unlike regular attributes, generated attributes are not stored anywhere, but always calculated on-the-fly at the time they are being queried. They are always up-to-date and reference the currently existing attributes as the basis for their calculations. That means, when querying generated attributes in a historic context (i.e. at an earlier point in time), they read the attributes present at that point in time (=the point in time for which the data is queried), NOT the latest set of attributes.

Generated attributes can not override existing attributes on the same layer. They can however hide attributes on the layers below, just like regular attributes can. 

Using a generator is a two-step configuration process. First, you specify the generator itself in the management area of the technical frontend, giving the generator a unique ID, and specifying the attribute name and template. Secondly, you link the generator to one (or more) layers by its ID on the layer management page. A layer can be linked to any number of generators. The order of the links also defines the order in which the generators are "run". But(!), referencing other generated attributes inside a generator template is not fully supported yet, so do not rely on this for now, even when it might appear to work. Refer to [issue #88](https://github.com/max-bytes/omnikeeper/issues/88) for updates on the topic.

Once a generator is linked to a layer, the generated attribute can(!) appear at CIs in the layer. However, linking alone is not enough. The generator's template needs to evaluate to a string. If the template evaluates to null, no generated attribute is created. Note that accessing a non-existant attribute in the base CI already evaluates to null, so if a template is defined as `attributes["potentially-existing-attribute"]`, it will only generate attributes for CIs that have an attribute with the name `potentially-existing-attribute`.

Generator templates are written using a separate, small language called Scriban. Refer to [the Scriban language documentation](https://github.com/scriban/scriban/blob/master/doc/language.md) for a complete reference. To reference other attributes in a template, use the global variable `attributes`. For example, to reference the value of an attribute named `attribute_x`, you write `attributes.attribute_x`. Because attributes can have any name, those names may also contain characters (like spaces or dots) that do not conform to [Scriban's requirements for property variables](https://github.com/scriban/scriban/blob/master/doc/language.md#4-variables). It's not possible to reference these attributes using the regular dot-syntax. But, you can use the alternative bracket-syntax to still reference them. For example, if an attribute has the name `complex.attribute name?`, you can reference it by writing `attributes["complex.attribute name?"]`.

Support for data types in templates is - at the time of writing - limited. The omnikeeper datatypes text, multiline text and integer are supported. Additional types will be properly supported in the future. Furthermore, the datatype returned by a generator is always text. Refer to [issue #86](https://github.com/max-bytes/omnikeeper/issues/86) and [issue #87](https://github.com/max-bytes/omnikeeper/issues/87) for updates on the topic.

## Example usecases
Here are some example usecases when generators can help:
- to combine existing attributes together. For example, suppose you have two attributes describing the name of people, called "first_name" and "last_name". You might want to have an attribute that contains the full name. To achieve that, you create a generator with a template `attributes.first_name + ' ' + attributes.last_name` and bind that generator to the appropriate layer.
- when you have attributes that contain the data you want to work with, but whose values are not quite formatted correctly. For example, suppose you have an attribute that contains the overall monitoring state of a system/CI. Its value is encoded as integers with the following meanings: 0=ok, 1=warning, 2=critical. For a report however, you'd like to show the state as text strings. To achieve that, you create the following generator template:
```
case attributes.state
  when 0
    "ok"
  when 1
    "warning"
  when 2
    "critical"
  else
    "unknown state"
end
```
- do simple calculations. For example, if you have attributes that each contain an integer, you can add them together with a generator. Note however, that the resulting attribute value's type is text, NOT integer. Support for returning different omnikeeper data types is coming.
```
attributes.number_a + attributes.number_b + attribute.number_c
```
