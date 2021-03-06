# Build and Packaging

omnikeeper is built and distributed as two main docker images, one backend/core image and one technical frontend image. Additionally, omnikeeper supports [[plugins|plugins]] that enhance the featureset of omnikeeper. These plugins are built separately and then added to omnikeeper during buildtime. 

All plugins are published in the [[omnikeeper github packages section|https://github.com/orgs/max-bytes/packages?repo_name=omnikeeper]].

During the build process, client libraries for working with omnikeeper's REST API are generated as well. These client libraries are created for different languages/technologies such as:
- Python
- Java
- Go
- Powershell
- ...
Distribution of these client libraries is done via their own Github repository. For example, the Java client library is located here: https://github.com/max-bytes/omnikeeper-client-java

## Variants

omnikeeper's plugin based architecture makes it a bit tricky to distribute omnikeeper as fully-built docker containers that already include plugins, because it's not defined which plugins should be part of the final image. For this reason, the build and deployment process introduces so-called "variants". Variants bundle omnikeeper itself with a set of matching plugins. Each variant has a distinct name that is used for identification and to distinguish between the variants. A successfully built variant is published on the Github docker repository under the name `variants/backend/[variant-name]` and  `variants/frontend/[variant-name]`.

## Pipeline chart
The following chart shows the full build and packaging process, starting from the left as source code and ending on the right with published artifacts and deployed containers.
![Pipeline chart for building and deploying](assets/drawio/build-deploy-pipeline.svg)
