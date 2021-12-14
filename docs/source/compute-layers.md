# Compute Layers

## Introduction

"Compute Layers" are one of the ways to programmatically create new and modify existing data in omnikeeper. Unlike using the GraphQL API, compute layers are more tightly integrated into omnikeeper and hence offer better performance and better control over its execution. In contrast to generators, which are simple, but also very limited in terms of versatility, compute layers are fully featured workers that can be programmed to perform any operation and produce any data. Compute layers get their name because they are assigned to a specific layer, turning a regular layer into a compute layer. Conversely, each layer can be associated with at most one compute layer "brain".

## Compute Layer Brains

The central piece to a compute layer is its "brain", aptly named "compute layer brain" or short "CLB" or "CLBrain". The CLBrain is what is actually performing all the actions: reading data, calculating things, writing data. The CLBrain is called at regular and configurable intervals. 

A CLBrain should write to a single layer only: the layer it is assigned to. However, it may read from any number of layers (including its associated layer). A compute layer should not be written to by any other user/service than the associated brain. In other words, the contents of a compute layer should be fully determined by the CLBrain. You can enforce this by not giving out permissions to write to this layer to anybody (the user that the compute layer uses implicitly has write permissions). Not following this advice will make it much harder to follow and understand the workings of the compute layer and make debugging and finding errors cumbersome.

CLBrains are written with .Net code, so in order to integrate a CLBrain into an omnikeeper instance and make it runnable, it should be written as an [omnikeeper backend plugin](plugins). Have a look at [[this sample plugin|https://github.com/max-bytes/omnikeeper/tree/master/backend/OKPluginCLBDummy]] for an example plugin containing a dummy CLBrain that can be used as the starting point for developing your own CLBrain.

To be able to reference CLBrains, each CLBrain gets a name. By default, when using the provided `CLBBase` class, the name is automatically set to be the name of the class that implements it. In the case of the dummy CLBrain above, this means its name is `CLBDummy`. If you desire, you can choose a different name by overriding the corresponding method of `CLBBase`, but it's recommended to keep it and instead name the implementing class appropriately.

## Compute Layer Configuration
CLBrains are not associated with layers directly. Instead, they are associated through a compute layer configuration, which is what ties it all together. Each omnikeeper instance can have any number of compute layer configurations. Each compute layer configuration consists of
* a unique ID, which is used to associate it with a specific layer
* a CLBrain Name, specifying what brain to use
* a CLBrain configuration (not to be confused with the compute layer configuration itself): this configuration is a JSON based structure that is used to configure the brain. The configuration is passed on each run of the CLBrain. A typical example of how to use this CLBrain configuration is to have an input layerset that specifies which layers the CLBrain should read its data from. How the configuration must be structured is completely governed by the CLBrain. The only requirement omnikeeper imposes is that it is a valid JSON object.

Separating the compute layer configuration from the layer makes it possible to re-use the same compute layer brain with different configurations on different layers. A potential usecase example for testing purposes: you might want to try out how a compute layer behaves with different input data by configuring it with a different input layerset and bind that to a temporary test-layer. Then you can compare the results of the test-layer with the regular output and note differences.

## Architecture Diagram
The following diagram shows the relationship between layers, compute layer configurations and compute layer brains:
![Architecture Diagram of Compute Layers](assets/drawio/overview-compute-layers.svg)