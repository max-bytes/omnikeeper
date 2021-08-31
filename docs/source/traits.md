# Traits

DRAFT

Traits are a very central concept for omnikeeper. Traits are used to introduce structure into the data. A trait is essentially a list of requirements that each CI may or may not fulfill. If a CI does, it "has that trait".  

## Trait definitions
Trait definitions govern what a trait represents and what a CI "has to have" to be eligible. A trait definition consists of the following parts:
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


## Traits  attributes and relations

Because traits are a complex topic, it makes sense to view them through different lenses that touch on different aspects of them:

## Traits vs. type systems

In a way, traits introduce a type-system, but without requiring that the data conforms to this system. CIs are still free to model data in whatever way they prefer, but if they want to be considered for a certain trait, they need to fulfill its requirements. 

Yet another way to view traits is as a type system. Each trait is also a type and CIs that have a trait are members of that type. The main difference to a typical type system is that the data itself defines what type(s) it represents. Traits define requirements, but if the data does not conform to theses requirements, there's nothing forcing a CI into a type.

## Traits vs. search
Another way to look at traits is that they represent search criteria that partition the CI space. omnikeeper offers the ability to query for and filter CIs according to their traits. Users and systems can use traits to find CIs relevant for their purposes.

## Traits vs. tagging systems


## Traits vs. 



## Trait sources
Traits can be defined and come from different sources.
1. core traits: traits defined by the omnikeeper core itself. Core traits are read only. They used by omnikeeper itself to define its own configuration, such auth roles, predicates and... trait definitions themselves.
2. plugin traits: every omnikeeper plugin has the ability to define its own traits. Their main purpose is to be used by the plugins themselves, but they can also be used by other parts of omnikeeper.
3. configured traits: 


## Traits vs. layersets
Traits and layers (or layersets) are two distinct, yet strongly related concepts. When talking about whether CIs have a trait, the question is always only answerable when a layerset is chosen as well. A CI might have a trait in one layerset, but not in another. 

 omnikeeper allows users to define traits and then query the data for these traits, getting only CIs that have them.


When talking about data types and structures, omnikeeper's data model together with traits represent an inversion of control from many typical data centric applications. It's not the database schema or other data structures that govern what a CI must and must not look like. The data itself "decides" whether or not it exhibits certain properties and therefore which traits it has.



Traits are more powerful than typical datatype hierarchies


A trait is in a way a blueprint or template for CIs. 

Each individual requirement is a required or optional attribute or relation, optionally including required data types, cardinality or other checks. For example, 

 And because of the requirements, each CI that has a trait is also guaranteed to 






TODO: talk about how traits are a generalized form of types, because its not a type hierarchy, but more like tags. and how this is better than a hard relational database schema.
