# Traits

DRAFT

Traits are used to introduce structure into the data. A trait is essentially a list of requirements that each CI may or may not fulfill. If a CI does, it "has that trait". omnikeeper allows users to define  traits and then query the data for these traits, getting only CIs that have them.


When talking about data types and structures, omnikeeper's data model together with traits represent an inversion of control from many typical data centric applications. It's not the database schema or other data structures that govern what a CI must and must not look like. The data itself "decides" whether or not it exhibits certain properties and therefore which traits it has.



Traits are more powerful than typical datatype hierarchies


A trait is in a way a blueprint or template for CIs. 

Each individual requirement is a required or optional attribute or relation, optionally including required data types, cardinality or other checks. For example, 

 And because of the requirements, each CI that has a trait is also guaranteed to 

In a way, traits introduce a type-system, but without requiring that the data conforms to this system. CIs are still free to model data in whatever way they prefer, but if they want to be considered for a certain trait, they need to fulfill its requirements. 





TODO: talk about how traits are a generalized form of types, because its not a type hierarchy, but more like tags. and how this is better than a hard relational database schema.
