# Configuration is Data

DRAFT

omnikeeper stores parts of its own configuration, such as Traits, Predicates and AuthRoles inside the regular data structures itself. That means that f.e. a defined Trait exists as its own CI whose attributes define the traits parameters (ID, requirements, ...).  
While this approach may appear strange at first, it enables a lot of positive effects:
- the configuration items are automatically versioned and a history of their changes is kept within omnikeeper's regular structures.
- it becomes possible to apply other features, that work on the regular data structures, to the configuration items. For example, the diffing tool may be used to compare changes in configuration items over time.
- configuration may be managed from outside of omnikeeper and possibly automated, through the regular REST or GraphQL APIs that work with omnikeeper data structures. 
- making omnikeeper use its own data structures for its configuration forces a certain diligence for development and serves as a usecase for itself. In other words, it puts omnikeeper's developers into the same shoes as developers who use omnikeeper as their data store.

In order to keep this self-configuration separate from the regular stored data, at least one separate configuration layer should be used. Normally, only one configuration layer is sufficient and its encouraged to give this layer the ID ``__okconfig``. While possible, avoid mixing omnikeeper config and regular data within a layer.

TODO: write about omnikeeper's self-configuration layers

TODO: talk about potential duplicates within f.e. AuthRole-IDs

## Bootstrapping / Chicken-Egg / Super User Role

Because omnikeeper keeps its own configuration within layers, there's a chicken-egg problem. A fresh omnikeeper instance has no layers, so it does not have its own configuration layer(s) either. But it's not possible to create new layers because no user has the `ok.layer.management` permission yet. Giving a user this permissions requires an AuthRole, which in turn needs a configuration layer...  
The super user role solves this problem because it does not rely on AuthRoles and gives the user all permissions, such as for creating new (configuration) layers.