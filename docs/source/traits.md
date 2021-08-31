# Traits

DRAFT

Traits are a very central concept for omnikeeper. Traits are used to introduce structure into the data. A trait is essentially a list of requirements that each CI may or may not fulfill. If a CI does, it "has that trait".  
A trait is defined by an ID and trait requirement, discussed hereafter:

## Trait requirements
Trait requirements govern what a trait represents and what a CI "has to have" (or at least "should have") to be eligible. Each individual requirement is a required or optional attribute or relation, additionally including requirements about its data type, its value, its cardinality (when talking about relations) or other checks.  
A trait's requirements consist of the following parts:
1. Required attributes form mandatory demands about attributes.
An example of a required attribute might look like the following:  
*attribute with name "hostname" that is of type text and has three or more characters.*  
Only a CI that fulfills this requirement has a chance to be eligible to have this trait. A trait may define multiple required attributes, but must define at least one.
2. Required relations do the same thing as required attributes, but for relations. A typical required relation might be defined as follows:  
*at least one outgoing relation with predicate "is_child_of".*  
One difference to required attributes: a trait does not HAVE to specify any required relations.
3. Optional attributes are - as the name implies - optional. Unlike required attributes, whether or not a CI fulfills optional attributes does not have an impact on trait eligibility. Optional attributes are still useful though - see effective traits.
4. Optional relations - same as optional attributes - just for relations.
5. Dependent traits are a mechanism to extend traits. Defined as a list of trait-names, dependent traits make it possible to extend another trait and build complex traits by combining simple ones. Specifying a dependent trait essentially adds all other requirements (required/optional attributes/relations) to this trait. An example:  
If you have a trait that defines what a "host" is, and you want to add another trait that specifically describes Linux hosts in the sense that Linux hosts are a subset of hosts, it would make sense to define the trait "linux_host", add "host" as a dependent trait and then only add additional requirements that specifically describe a linux host. This could f.e. be a required attribute named "operation_system" with a value of "Linux".  
One thing to keep in mind is that dependent traits are a very tight form of coupling. Any change to the "parent" trait automatically changes the "child" trait as well. Make sure you are certain that this is (and will be in the future) the expected behavior. If you are uncertain, it may be better to not use dependent traits and define the requirements explicitly.  
NOTE/TODO: fix wording; "dependent trait" is actually the wrong word to use, both in code and here. It should actually be called something like "parent trait".

## Trait IDs
Trait IDs are text-based IDs used to uniquely identify each trait. They are used to communicate with omnikeeper and should also be used when talking about/describing the data within omnikeeper. That means they server a double-role and should be thought of as both human- and computer readable. Trait IDs may only contain 
- lowercase characters (a-z)
- digits (0-9)
- underscores (_)
- dots (.)

Dots should be used as a hierarchy and grouping mechanism to pool similar traits together. For example, omnikeeper itself defines so-called meta-traits for its own configuration that are named as follows:
- __meta.config.trait
- __meta.config.predicate
- __meta.config.auth_role

Underscores should be used to separate words, essentially following [snake_case](https://en.wikipedia.org/wiki/Snake_case).

Whenever possible, try to follow these rules for your own trait IDs.

## Effective traits
TODO

## Traits vs. ...

Because traits are a complex topic, it makes sense to view them through different lenses that touch on different aspects of them:

### Traits vs. search
One way to look at traits is that they represent search criteria that partition the CI space. omnikeeper offers the ability to query for and filter CIs according to their traits. Users and systems can use traits to find CIs relevant for their purposes. So even when everything in omnikeeper's base data model is a CI, traits separate and structure this otherwise unstructured list of CIs into accessible groups.

### Traits vs. data types
Another way to view traits is as a type system. Each trait can be seen as a data type and CIs that have a trait are members of that data type. The main difference to a typical type system is that the data itself defines what type(s) it represents. Traits define requirements, but if the data does not conform to theses requirements, there's nothing forcing a CI into a type. CIs are still free to model data in whatever way they prefer, but if they want to be considered for a certain trait, they need to fulfill its requirements.  
When talking about data types and structures, omnikeeper's data model together with traits represent an inversion of control from many typical type-based applications. It's not the database schema or other data structures that govern what a CI must and must not look like. The data itself "decides" whether or not it exhibits certain properties and therefore which traits it has.  
Another important difference is that one CI can exhibit any number of traits, whereas in typical type-based data models, each "entity" needs to be a member of exactly one single type. Looking through that lens, traits are a much more powerful and expressive concept as they don't have this restriction.

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

