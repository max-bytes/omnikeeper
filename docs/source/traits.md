# Traits

DRAFT

Traits are a very central concept for omnikeeper. Traits are used to introduce structure into the data. A trait is essentially a list of requirements that each CI may or may not fulfill. If a CI does, it "has that trait".  

## Trait requirements
Trait requirements govern what a trait represents and what a CI "has to have" to be eligible. A trait's requirements consists of the following parts:
1. Required attributes form mandatory demands about attributes.
An example of a required attribute might look like the following:  
*attribute with name "hostname" that is of type text and has three or more characters.*  
Only a CI that fulfills this requirement has a chance to be eligible for this trait. A trait may define multiple required attributes, but must define at least one.
2. Required relations do the same thing as required attributes, but for relations. A typical required relation might be defined as follows:  
*at least one outgoing relation with predicate "is_child_of".*
3. Optional attributes are - as the name implies - optional. Unlike with required attributes, whether or not a CI fulfills optional attributes does not have an impact on trait eligibility. Optional attributes are still useful though - see Effective traits.
4. Optional relations - same as optional attributes - just for relations.
5. Dependent traits are a mechanism to extend traits. Defined as a list of trait-names, dependent traits 

## Effective traits
TODO

Because traits are a complex topic, it makes sense to view them through different lenses that touch on different aspects of them:

## Traits vs. type systems

Yet another way to view traits is as a type system. Each trait can be seen as a type and CIs that have a trait are members of that type. The main difference to a typical type system is that the data itself defines what type(s) it represents. Traits define requirements, but if the data does not conform to theses requirements, there's nothing forcing a CI into a type. CIs are still free to model data in whatever way they prefer, but if they want to be considered for a certain trait, they need to fulfill its requirements.  
When talking about data types and structures, omnikeeper's data model together with traits represent an inversion of control from many typical type-based applications. It's not the database schema or other data structures that govern what a CI must and must not look like. The data itself "decides" whether or not it exhibits certain properties and therefore which traits it has.  
Another important difference is that one CI can exhibit any number of traits, whereas in typical type-based data models, each "entity" needs to be a member of exactly one single type. Looking through that lens, traits are a much more powerful and expressive concept as they don't have this restriction.

## Traits vs. tagging systems
Traits share some similarities with [tags and tagging systems](https://en.wikipedia.org/wiki/Tag_(metadata)). Tags are metadata that is used to label and describe entities and allows it to be searched for, just like traits for CIs in omnikeeper. An entity can have any number of tags, just as CIs can have any number of traits.  
The biggest difference between typical tagging systems and omnikeeper's tag system is that tags are normally applied manually and by humans, leading to a certain fuzziness in its explanatory power. Traits on the other hand are applied fully automatically by application of its requirements to the CI's data. Provided that the trait is properly defined, omnikeeper keeps track of which CIs exhibit which traits rigorously.

## Traits vs. search
Another way to look at traits is that they represent search criteria that partition the CI space. omnikeeper offers the ability to query for and filter CIs according to their traits. Users and systems can use traits to find CIs relevant for their purposes. So even when everything in omnikeeper's base data model is a CI, traits separate and structure this otherwise unstructured list of CIs.


## Traits vs. change
TODO: Write about changes in data structures and requirements -> traits are flexible enough.


## Traits vs. layersets
Traits and layers (or layersets) are two distinct, yet strongly related concepts. When talking about whether CIs have a trait, the question is always only answerable when a layerset is chosen as well. A CI might have a trait in one layerset, but not in another. 

## Trait sources
Traits can be defined and come from different sources.
1. core traits: traits defined by the omnikeeper core itself. Core traits are read only. They are used by omnikeeper itself to define its own configuration items, such auth roles, predicates or even trait requirements themselves.
2. plugin traits: every omnikeeper plugin has the ability to define its own traits. Their primary purpose is to be used by the plugins themselves, but they can also be used by other parts of omnikeeper.
3. configured traits: additional traits may also be configured, either manually via the technical frontend UI or via API.





Traits are more powerful than typical datatype hierarchies


A trait is in a way a blueprint or template for CIs. 

Each individual requirement is a required or optional attribute or relation, optionally including required data types, cardinality or other checks. For example, 

 






TODO: talk about how traits are a generalized form of types, because its not a type hierarchy, but more like tags. and how this is better than a hard relational database schema.
