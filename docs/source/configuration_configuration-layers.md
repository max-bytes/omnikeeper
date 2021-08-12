# Authorization - Configuration Layers

TODO: write about omnikeeper's self-configuration layers

TODO: talk about potential duplicates within f.e. AuthRole-IDs

## Bootstrapping / Chicken-Egg / Super User Role

Because omnikeeper keeps its own configuration within layers, there's a chicken-egg problem. A fresh omnikeeper instance has no layers, so it does not have its own configuration layer(s) either. But it's not possible to create new layers because no user has the `ok.layer.management` permission yet. Giving a user this permissions requires an AuthRole, which in turn needs a configuration layer...  
The super user role solves this problem because it does not rely on AuthRoles and gives the user all permissions, such as for creating new (configuration) layers.