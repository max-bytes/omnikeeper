# omnikeeper documentation

Welcome to the documentation for omnikeeper.

**Disclaimer:** *omnikeeper is still in development. That's why the documentation is still lacking or downright missing in many aspects. However, documentation is an ongoing effort and will see lots of improvements and additions going forward.*

# Introduction - What is omnikeeper?

omnikeeper is a general-purpose and highly flexible data store solution. omnikeeper's initial usecase: a store for application and infrastructure configuration data. This puts omnikeeper in the vicinity of databases for [ITIL](https://en.wikipedia.org/wiki/ITIL) configuration management or in short: [CMDB](https://en.wikipedia.org/wiki/Configuration_management_database)s. But while omnikeeper shares a lot of similarities with CMDBs, there are key concepts that set it apart. And because of its simple, but generic data structures, omnikeeper supports a lot more usecases that CMDBs do. To get a better sense of what omnikeeper is, lets briefly look at how it stores and models data:

# Data structures
The data structures of omnikeeper are very simple. There are attributes, CIs and relations:

## Attributes
The smallest building block are called "attributes". An attribute has a name and a value. Attribute-values are strongly typed with a datatype such as text or integer, but also more powerful types, such as JSON or YAML. In addition to scalar values, array values are also possible. Every attribute is bound to exactly one CI.

## CIs
CIs (singular CI, short for "Configuration Item") are the central element of omnikeeper. You may think of a CI as a specific "thing" or "entity". CIs are identified by a [UUID](https://en.wikipedia.org/wiki/Universally_unique_identifier)called CI-ID or CIID. CIs themselves contain no data though. The way to add information to CIs is by adding attributes and relations.

## Relations
Relations relate exactly two CIs to each other. Relations are directed, that means a relation has a start and an end, or a "from" and a "to" half. You can think of a relation as a directed link from one CI to another.  
A relation is further specified by a "predicate", which defines the nature of the relation. In the relation, you only specify the predicate-ID, which is a unique string referring to the predicate. Predicates are optional structures that define a relation further.
An example of a relation might be to define a parent-child relationship between two CIs. In that case, it might make sense to use the predicate-ID `is_child_of` and make it "go" from the child-CI to the parent-CI.

## ...and that's it
All stored data in omnikeeper breaks down into these three basic structures. Starting from a simple data model like this has a lot of advantages and makes supporting lots of usecases possible.

This approach to modelling data is not novel whatsoever. It is most well-known under the name [Entity-Attribute-Value Model](https://en.wikipedia.org/wiki/Entity%E2%80%93attribute%E2%80%93value_model). Many applications have used this or a similar approach to data modelling. However, omnikeeper introduces novel concepts and includes tools and features that make it unique.

# Motivation - why does omnikeeper exist and what makes it special?

In the realm of data modelling and applications that work with said data, there's one thing that's certain: change. Everything changes all the time: requirements of stakeholders and users, their access patterns, their usecases, ... and therefore, the data itself needs to change as well.  
This constant change does not only happen during the development, but extends beyond that to the whole lifecycle. In fact, the more successful an application or service is, the more pressure there will be to change it, to improve it, to add to it. This means that no matter how well you design your data model up front, there will be changes and they will be significant.  
What can make or break an application that is forced to change is its data model. A data model that is too rigid and too hard to change and adapt becomes a burden and can force costly refactorings or make full blown reworks necessary. On the other hand, a data model that is too limp and does not offer enough structure is not ideal either. Data handling becomes cumbersome, the data itself becomes fragmented and structures are lost.  
omnikeeper claims to be - for a set of usecases - a sweet spot between the two extremes, offering the right balance between structure and flexibility.  
omnikeeper tackles structuring of data from a different perspective. Instead of forcing incoming data into a very tight corset of pre-made structures and data-types, it only expects the data to conform to the simple CI-attribute-relation model described above. Apart from that, data is generally free to exist in whatever shape it prefers. To still be able to introduce structure and reason about the data in a more well-defined way, omnikeeper offers features such as [[traits|traits]] and [[layers|layers]]. [[Follow this link to read more about omnikeeper's data model and its design|datamodel]].

Another important topic when talking about data and change is the dimension of time. A lot of applications only look at the current state of the data and do not view it through the lens of time. omnikeeper is built from the ground up with time as its own important dimension. Data in omnikeeper is by default kept historically and any data query you can make may also specify a time to get the data at that exact point in time. Making time a central aspect enables a lot of usecases, such as comparing the data at two points in time, highlighting differences or allowing data audits to take place.

# Usecases for omnikeeper

- a data store for application and infrastructure configuration
- an application prototyping framework or a data layer for applications that allows data models to stay in flux and evolve during development and beyond
- as an intermediate component in a data pipeline between data producers and consumers, offering data mapping, selection and prioritization capabilities

# What is omnikeeper NOT

- a full-fledged, auto-of-the-box, [ITIL](https://en.wikipedia.org/wiki/ITIL)-compatible [CMDB](https://en.wikipedia.org/wiki/Configuration_management_database)
- a data store for high-volume streaming data that changes constantly and rapidly, such as time-series data generated from IT monitoring. 

# Sample Setup / Test Stack

For a fully functional, self-contained and docker-powered omnikeeper stack that is ready to use for first experiments, visit https://github.com/max-bytes/omnikeeper-stack
