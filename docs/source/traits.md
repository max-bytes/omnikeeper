# Traits

DRAFT

Traits are a very central concept for omnikeeper. Traits introduce structure into the list of existing CIs, categorizing them and allowing users to search, filter and explore them. A trait is essentially a list of requirements that every CI may or may not fulfill. If a CI does fulfill a trait's requirements, it "has" that trait. A CI's traits can tell us more about its properties and purpose.  
While omnikeeper comes with a few predefined traits, a typical omnikeeper usecase will definitely involve custom-made traits. Any user or process can - given the correct permissions - add traits to an omnikeeper instance or modify existing ones. A defined trait applies immediately and can be used right away by all users and processes. Just as the data in an omnikeeper instance evolves, its traits are also expected to change over time.  
Before diving deeper into what traits can do, let's look at how a trait is shaped. A trait is defined by a trait ID and trait requirements, discussed hereafter:

## Trait IDs
Trait IDs are text-based IDs used to uniquely identify each trait within an omnikeeper instance. They are used by users and processes for communicating with omnikeeper and should also be used when talking about/describing the data within omnikeeper. Trait IDs serve a double-role and should be thought of as both human- and computer readable. As an example, a trait that is used to identify Windows hosts in an IT inventory usecase might be given the name `host_windows`. Because of their importance, trait IDs should be named thoughtfully  
### Naming convention 
For technical and practical reasons, trait IDs must follow a naming convention:  
Trait IDs may only contain 
- lowercase characters (a-z)
- digits (0-9)
- underscores (_)
- dots (.)

Dots should be used as a hierarchy and grouping mechanism to pool similar traits together. For example, omnikeeper itself defines so-called meta-traits for its own configuration that are named as follows:
- __meta.config.trait
- __meta.config.predicate
- __meta.config.auth_role

Underscores should be used to separate words, following the [snake_case](https://en.wikipedia.org/wiki/Snake_case) convention.

The trait ID should - as best as possible - describe the property it models. For many usecases, a (compound) noun fits best. The singular is highly preferred over the plural. Example: use `host_windows`, do NOT use `hosts_windows`. Some usecases might better suit the use of an adjective, often with the suffix `able`, to describe that something can be done with this CI. Example: use `automation.ansible_targetable` to describe CIs that can be targeted with ansible automation (also note the use of the `automation` prefix and dot).  
When leveraging dependent traits to form a trait type hierarchy tree, it is recommended that the "child" traits keep the "parent" trait as a prefix. Example: parent trait `host`, child traits `host_windows` and `host_linux`.

Whenever possible, try to follow these rules for your own traits when specifying their ID.

## Trait requirements
Trait requirements govern what a trait represents and what a CI "has to have" or "can have" to be eligible. An individual requirement is a required or optional attribute or optional relation, additionally including requirements about its data type, its value, its cardinality (when talking about relations) or other checks.  
A trait's requirements consist of the following parts:
1. Required attributes form mandatory demands about attributes.
An example of a required attribute might look like the following:  
*attribute with name "hostname" that is of type text and has three or more characters.*  
Only a CI that fulfills this requirement has a chance to be eligible to have this trait. A trait may define multiple required attributes, but must define at least one.
2. Optional attributes are - as the name implies - optional. Unlike required attributes, whether or not a CI fulfills optional attributes does not have an impact on trait eligibility. Optional attributes are still useful though - see trait entities.
3. Optional relations - same as optional attributes - just for relations. A typical optional relation definition might look as follows:  
*match outgoing relations with predicate "is_child_of".*  
4. Dependent traits are a mechanism to extend traits. Defined as a list of trait-names, dependent traits make it possible to extend another trait and build complex traits by combining simple ones. Specifying a dependent trait essentially adds all other requirements (required/optional attributes/relations) to this trait. An example:  
If you have a trait that defines what a "host" is, and you want to add another trait that specifically describes Linux hosts in the sense that Linux hosts are a subset of hosts, it would make sense to define the trait "linux_host", add "host" as a dependent trait and then only add additional requirements that specifically describe a linux host. This could f.e. be a required attribute named "operation_system" with a value of "Linux".  
See the the [source code file](../blob/master/backend/Omnikeeper.Base/Service/RecursiveTraitService.cs) for a technical explanation of how dependent traits are resolved and in what order.  
One thing to keep in mind is that dependent traits are a very tight form of coupling. Any change to the "parent" trait automatically changes the "child" trait as well. Make sure you are certain that this is (and will be in the future) the expected behavior. If you are uncertain, it may be better to not use dependent traits and define the requirements explicitly.  
NOTE/TODO: fix wording; "dependent trait" is actually the wrong word to use, both in code and here. It should actually be called something like "parent trait".

## Trait Entities
Whenever a CI fulfills a trait, it is guaranteed to have certain attributes, as defined in the trait requirements. When looking at a CI through the lens of a trait, it only "sees" the data relevant to it. Like putting a mask over a picture hides the parts occluded by the mask's shape, putting a trait "mask" over a CI removes unimportant data and keeps only some parts of it visible. For a trait, the still visible parts of the CI is called the "trait entity".  
While a trait entity is computed from the underlying CI's data, its data is much more structured than that of the CI itself. Because the trait ensured its requirements are met, the trait entity is a very well defined piece of data, very much like a row in a relational database table or a data type in a programming languages. Put differently, trait entities are the result of traits and the data they are applied to.
Trait entities allow users and processes to work with omnikeeper's data in a structured way, just as they would work with the data in typical schema-driven applications that define fixed data types.  
Trait entities are the reason why optional attributes/relations are useful. While optional attributes/relations do not have an effect on the filtering mechanism of a trait, they are part of the resulting trait entity just as their required counterparts are. The only difference: because the existance of their underlying attributes/relations is not mandatory, they MAY also NOT be present in the trait entity. Here is a diagram showing the construction of trait entities in a simple example:

![Example for how trait entities are constructed](assets/drawio/trait-entities-applied.svg)

### Trait Entity IDs

TODO

### Working with trait entities

The main interface for working with trait entities is through the GraphQL interface. omnikeeper creates the relevant GraphQL types for each trait dynamically. This allows for structured querying of this data. As an example, consider the following GraphQL query for fetching all "host" trait entities (continuing the example from above):
```graphql
query {
  traitEntities(layers: ["layer_1", "layer_2"]) {
    host {
      all {
        entity {
          hostname
          name
          os
        }
      }
    }
  }
} 
```
Executing this query against an omnikeeper GraphQL instance could return the following response:
```json
"traitEntities": {
  "host": {
    "all": [
        {
          "entity": {
            "hostname": "server01",
            "name": "CI 001",
            "os": "Linux"
          }
        },
        {
          "entity": {
            "hostname": "server02",
            "name": "CI 002"
          }
        }
    ]
  }
}
```

The following diagram shows the structures that data passes through from the core data all the way to the client:
![Overview of how trait entites work](assets/drawio/overview-trait-entities.svg)

## Changes to traits

TODO: write about the different changes that can be applied to a trait and what effects this has: backwards-compatible vs. backwards-incompatible changes, migration strategies, ...

## Traits vs. ...

Because traits are a complex topic, it makes sense to view them through different lenses that touch on different aspects of them:

### Traits vs. search
One way to look at traits is that they represent search criteria that partition the CI space. omnikeeper offers the ability to query for and filter CIs according to their traits. Users and processes can use traits to find CIs relevant for their purposes. So even when everything in omnikeeper's base data model is a CI, traits separate and structure this otherwise unstructured list of CIs into accessible groups.

### Traits vs. data types
Another way to view traits is as a type system. Each trait can be seen as a data type and CIs that have a trait are members of that data type. Or actually, the emerging **trait entities** are members of that data type.  
The main difference to a typical type system is that the data itself defines what type(s) it represents. Traits define requirements, but if the data does not conform to theses requirements, there's nothing forcing a CI into a type. CIs are still free to model data in whatever way they prefer, but if they want to be considered for a certain trait, they need to fulfill its requirements.  
When talking about data types and structures, omnikeeper's data model together with traits represent an inversion of control from many typical type-based applications. It's not the database schema or other data structures that govern what a CI must and must not look like. The data itself "decides" whether or not it exhibits certain properties and therefore which traits it has.  
Another important difference is that one CI can exhibit any number of traits, whereas in typical type-based data models, each "entity" needs to be a member of exactly one single type. Looking through that lens, traits are a more powerful and expressive concept as they don't have this restriction.

### Traits vs. tagging systems
Traits share some similarities with [tags and tagging systems](https://en.wikipedia.org/wiki/Tag_(metadata)). Tags are metadata that is used to label and describe entities and allows it to be searched for, just like traits for CIs in omnikeeper. An entity can have any number of tags, just as CIs can have any number of traits.  
The biggest difference between typical tagging systems and omnikeeper's tag system is that tags are normally applied manually and by humans, leading to a certain fuzziness in its explanatory power. Traits on the other hand are applied fully automatically by application of its requirements to the CI's data. Provided that the trait is properly defined, omnikeeper keeps track of which CIs exhibit which traits rigorously.

### Traits vs. change
TODO: Write about changes in data structures and requirements -> traits are flexible enough.

### Traits vs. layersets
Traits and layers (or layersets) are two distinct, yet strongly related concepts. When talking about whether CIs have a trait, the question is always only answerable when a layerset is chosen as well. A CI might have a trait in one layerset, but not in another. 

### Traits vs. blueprints/templates
TODO: A trait is in a way a blueprint or template for CIs. 

## Trait sources
Traits can be defined and come from different sources.
1. core traits: traits defined by the omnikeeper core itself. Core traits are read only. They are used by omnikeeper itself to define its own configuration items, such auth roles, predicates or even trait requirements themselves.
2. plugin traits: every omnikeeper plugin has the ability to define its own traits. Their primary purpose is to be used by the plugins themselves, but they can also be used by other parts of omnikeeper.
3. configured traits: additional traits may also be configured, either manually via the technical frontend UI or via API.

