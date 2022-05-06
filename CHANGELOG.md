
# Change Log
All notable changes to this project will be documented in this file.
 
The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [17.0.0] - 2022-05-06
### Added
- (breaking) reworked odata support based on trait entities
- initial version of insight-discovery-ingest plugin
- generic-json-ingest: implemented SameTargetCIHandling feature
- technical frontend: 
  - added usage stats interface
- added usage stats tracking for attribute-names and relation-predicates
### Changed
- (breaking) reworked usage-stats to include layer-ID per element and removed layer stats type
- improved error handling of generic-json-ingest 
- improved CLB logging
- technical frontend:
  - (breaking) migrated /auth prefix of keycloak URL from hard coded value into env variable
  - update keycloak-js to 18.0.0
- migrated configuration for odata contexts into new data-based form
- performance improvements: added relation selection based on from/to + relationIDs and use throughout codebase 
### Fixed

## [16.0.0] - 2022-04-20

### Added
- GraphQL API:
  - (breaking) reworked trait relation mutations: modifying trait relations are now separate methods and not part of the trait entity mutations anymore, allowing more fine-grained control
  - (re-)added possibility to insert relations when inserting new trait entity
  - implemented trait-hints for trait relations to simplify typed access to related CIs with traits
  - added support for relation-based filters
  - added NonNullGraphType wrapper for certain trait entity attribute fields
  - added GraphQL GET endpoint
- technical frontend:
  - implementation of visualization frontend plugin
- implementation of double attribute value type; closes #189
- implemented basic application health check
- implemented system-test infrastructure and initial test cases; added system-tests to ci/cd pipeline
- implemented attribute selection for name+value filters
- added ComputeLayer config to core traits

### Changed
- GraphQL API:
  - dataloader-related performance improvements and restructurings
  - (breaking) removed min- and max-cardinality from trait relations
  - big performance improvement changes to trait entity filtering
- technical frontend:
  - removed trait select filters for empty and meta @ ci search
  - rework of frontend plugin management and infrastructure
- updated to .Net 6.0
- replacement of Newtonsoft.Json with System.Text.Json throughout the codebase
- performance improvements to JSON attribute value handling
- disabled OData support for now
- switched to smaller alpine based docker image for backend container
- updated SpanJSON to latest version
- updated Graphql-Dotnet to 5.1.0
- removed obsolete REST API endpoints
- performance improvement and memory improvement to GenericJsonIngest ingest

### Fixed
- lots of improvements to JSON handling in various levels
- fixed memory leak related to ModelContexts and using them in GraphQL resolvers
- fixed bug in trait entity GraphQL mutations where errors would still commit the data to the database
- added layer based authz checks for multiple graphQL queries and mutations
- fix for #59
- bugfix for trait entities: updating-by-CIID and deleting-by-CIID had no effect because database transaction was not commited


## [14.0.0] - 2022-03-19

### Changed
- GraphQL API:
  - (breaking) implemented regex options for trait entity attribute filtering

## [13.2.0] - 2022-03-17

### Added
- initial version of visualization plugin

## [13.1.0] - 2022-03-15

### Added
- support for masking of attributes and relations
- GraphQL API:
  - implemented trait entity filtering for text attributes by regex and exact
  - trait entities API: implemented ability to set CI name in mutations
  - implemented automatic reload of trait entities GraphQL schema; fixes #173
- implemented default behavior for Trait Entities to make it look for matching CIs based on IDs only if it can't find a complete entity
- implemented "OtherLayersValueHandling", which allows the Trait Entity mutation APIs to skip attribute/relation writes if the data already exists in other layers. This means that data is not needlessly duplicated.
- implementation and integration of plugin OKPluginVariableRendering; made part of tsa variant

### Changed
- GraphQL API:
  - (breaking) improved GraphQL API for (recursive) traits
  - added safety checks to trait entity mutations 
- technical frontend:
  - implemented tree for traits in CI search view
  - small improvements to traits view
  - store layer settings in local storage
  - added copy buttons for various elements, including CIIDs and changeset-IDs
  - improvements to search result list
  - updated several dependencies, including graphql, ant-design, apollo, react and keycloak-js
- high availability:
  - (breaking) switched from hangfire to Quartz for job scheduling
  - removed distributed memory cache
  - removed latest layer caching

- lots of code refactorings, including areas:
  - completely reworked and simplified mutations in Attribute- and Relation Model
  - tests
- bumped various libraries to their latest versions
- reduced chattiness of CLBJob logger 
- Made PassiveFilesController of GenericJSONIngest properly report its logging category, including context; fixes #174

### Fixed
- fixed regression bug causing attribute deletions to occur for generated attributes on every bulk update
- bugfix for parsing JSON as string from GraphQL API
- bugfix for cross-request user spilling; fixes #172
- Technical frontend:
  - bugfix when removing all generators from layer config
  - fixed issue with auth role permission UI; fixes #171
- bugfixes related to UsageTrackingService, Disposal and Autofac
- improved caching/locking mechanisms of ICurrentUserService implementations
- bugfix in data loader related to effective traits
- bugfix related to masking in BulkUpdate() when removing attributes

## [12.0.1] - 2022-01-21

### Fixed
- bugfix for naming conventions of trait entities
- bugfix regarding trait entity relations

## [12.0.0] - 2022-01-20

### Added 
- GraphQL API:
  - implemented transition from mergedCI to trait entity
  - added API for fetching latest change(set) for a trait entity

### Changed
- technical frontend: 
  - improved search UI: only search on button click, various layout improvements
- (breaking) changed graphql field for attribute value to lowercase
- (breaking ) switched from ElementType to ElementWrapperType when accessing trait entities from ci in GraphQL API
- removed migration scripts for latest tables and layer data, removed old layer_* db tables

### Fixed
- some hangfire stability improvements (hopefully)
- technical frontend:
  - fixed rtl texts with special characters (attribute names, trait-IDs)

## [11.1.0] - 2022-01-17

### Added 
- restart functionality in backend through REST API and frontend through management interface
- implemented querying and mutating of relations through GraphQL API for trait entities

### Changed
- bumped versions of backend libraries:
  - NuGet.Frameworks from 5.8.0 to 6.0.0
  - GraphQL from 4.6.1 to 4.7.1
  - GraphQL Playground from 4.3.0 to 5.2.0
- removed protobuf dependency
- reduced loglevel of UsageDataWriterRunner
- removed graphql playground in production environment

### Fixed
- added permission check for readable layers when performing generic JSON ingest
- bugfix for race condition on trait entity initialization


## [11.0.0] - 2022-01-13

### Added
- Initial implementation of GraphQL API for trait entities:
  - custom type-safe types for all traits
  - querying all/singleByCIID/singleByDataID  
  - mutations: upsert and remove
    - unsupported: mutating trait relations

### Changed
- Big refactoring to the way trait entities and generic trait entities are handled in code
- Generic JSON ingest plugin: 
  - improved features on how to do ID matching: union/intersect nested matching, more matching options
  - improved performance, logging and error handling

### Fixed
- GraphQL query for effective traits: did not return optional trait attributes properly

## [10.0.0] - 2021-12-17

### Added
- ability to change layer description

### Changed
- (breaking) migrated layer-data from dedicated database tables into meta-config layer structure
- (breaking) changed GraphQL API for creating layers and layer-data
- technical frontend: changed layer modification UI in management
- technical frontend: improved layer drawer visualization
- improved hangfire concurrent job handling
- performance improvement: per-request caching of traits

### Fixed
- technical frontend: layer drawer line break bug

## [9.0.0] - 2021-12-09

### Added
- usage tracking for generators
- switched hangfire backend from memory backed to postgres
- database: added unique constraint that ensures that per changeset and layer, only a single change can be made to each attribute/relation
- GraphQL server: implemented possibility for graphql resolver of effective traits (of CI) to specify traitIDs

### Changed
- technical frontend: switched default empty-trait search behavior to "may" instead of "must not"
- performance improvements: graphql-fetching EffectiveTraits of CIs -> fetching only relevant attribute from database
- performance improvements: getting latest layer change
- moved long-running archive old data default interval to once per day
- better console output formatting
- give hangfire jobs IDs so that the are properly stoppable; remove existing hangfire jobs on startup
- internal changes and work to support attribute masks

### Fixed
- technical frontend: nginx bugfix for URLs containing a period
- GraphQL server: when not selecting mergedAttributes, querying effectiveTraits only ever returned the __named effectiveTrait


## [8.0.0] - 2021-12-09

Internal release

## [7.0.0] - 2021-12-01

### Added
- (breaking) implemented functionality for CLBs to skip their run under certain circumstances: when all of their dependent layers have not change since their last run
- technical frontend: gridview: added full text search field to grid
- technical frontend: show CI name in changeset view for attributes
- technical frontend: check latest layer update time in layer statistics

### Changed
- performance improvements: per-request caching of various often-needed data such as layers and meta configuration

## [6.0.0] - 2021-11-25

### Added
- (breaking) rework of CI diffing GraphQL API
- added option to CI diffing: cross CI diffing for comparing 2 different CIs with each other

## [5.0.0] - 2021-11-24

### Added
- (breaking) support for optionally referenced attributes in generators. The decision whether or not a generator creates an attribute for a CI is made by the generator template: if it evaluates to `null`, no attribute is generated.

## [4.0.1] - 2021-11-24

### Fixed
- build process for backend

## [4.0.0] - 2021-11-24

### Added
- (potential breaking) upgrade to .Net 5.0
- (breaking) reworked GraphQL API for CI diffing
- ability for plugins to define their own GraphQL queries and mutations

### Changed
- performance improvement to maintenance task of archiving unused attributes/relations

### Fixed
- technical frontend: fixed wrong badge counts for added and removed attributes in changeset view
- fixed CI diffing GraphQL resolve error

## [3.1.0] - 2021-11-18

### Added
- generic JSON Ingest: graceful error handling for missing relation ends

### Fixed
- better handling of x-forwarded-proto in certain environments (nginx SSL frontloading+docker)

## [3.0.0] - 2021-11-18

### Added
- implemented bulk replace for trait entities
- added support for historical querying of layers

### Fixed
- technical frontend: bugfix for error when editing generators in layer management
- bugfix for swagger API generation error related to AuthRedirectController

## [3.0.0] - 2021-11-15

### Added
- initial implementation of usage stats tracking for effective traits, layers and auth-roles
- implementation of GenericTraitEntities and usage of them for all core traits such as base configuration, predicates and traits themselves
- added support for tuples as ID for GenericTraitEntities
- initial implementation of attribute value type "mask"
- implemented CI diffing in backend, offered GraphQL interface, made technical frontend differ use backend implementation
- added REST endpoint /.well-known/openid-configuration to help clients with authentication
- gridview: support for ag-grid enterprice license (via environment variable)
- gridview: support for copy/paste (when using ag-grid enterprise)
- gridview: support for excel export (when using ag-grid enterprise)

### Changed
- performance improvements to CI search
- performance improvements to relation querying
- performance improvements to attribute mutations: use postgres copy inserts
- performance improvements when querying MergedCIs
- performance improvements to Generic JSON Ingest
- marked some REST APIs obsolete, prefer GraphQL use instead
- usage of Autofac DI instead of Core DI, removed scrutor

### Fixed
- technical frontend: fixed performance issues in various parts of the frontend when displaying larged numbers of attributes/relations/changesets/CIs
- fixed order of layers in layerStack array when returned by GraphQL
- re-added display of stacktrace in GraphQL error response objects
- technical frontend: inreased CLBrain Config cell editor maximum length
- gridview: bugfix for [not-set] issue
- lots of smaller bugfixes

## [2.0.0] - 2021-10-09
 
### Added
- Breaking change: split meta- and base-configuration; made base-configuration a CI inside of ok-config layer(s)
- Option (via environment variable) to add AgGrid enterprise license, which enabled enterprise-only features in the gridview
 
### Changed
- An __okconfig layer is now automatically created on startup if it does not exist AND is set as meta-config layer

## [1.8.0] - 2021-10-07
 
### Added
- support for exporting specific layers in layer export feature
 
### Changed
- switched to SpanJson for performance intense JSON (de-)serializations: GraphQL and Gridview data query
- performance improvements to LayerStatisticsModel
 
### Fixed
- bugfixes for CI search regarding empty trait
- bugfix when building latest tables in omnikeeper ramp-up

## [1.7.1] - 2021-10-05
 
### Changed
- performance improvements to archiving data runner
 
## [1.7.0] - 2021-10-04
 
### Added
- implemented configurable compute layers
 
### Changed
- performance improvements for querying attributes via GraphQL
- removed CompactCI and distinction between that and FullCI; there's now only one more CI class throughout the code base
- simplified GraphQL query schema for querying CIs
- implemented data loader support for fetching effective traits for CIs via GraphQL
 
## [1.6.0] - 2021-10-01
 
### Added
- frontend: added dashboard, showing overall omnikeeper instance statistics
- implemented Generators! See the [documentation](https://github.com/max-bytes/omnikeeper/wiki/generators) for an introduction
- added support for selecting specific attributes when querying CIs
 
### Changed
- performance improvements when searching for CIs with traits
- performance improvement for archiving/deleting unused and empty CIs
- simplified (Base)AttributeModel by a lot
- removed Attribute- and Relation-State, simplified datamodel
 
### Fixed
- layer export: generated attributes are not exported anymore
- bugfix in CI search regarding empty trait
- lots of small bugfixes

## [1.5.1] - 2021-09-20

### Added
- gridview: support for columns from related CI attributes via `sourceAttributePath`. F.e, specify `sourceAttributePath: [">", "runs_on"]` in addition to `sourceAttributeName` to show attribute of CI related via a forward relation with predicate `runs_on`
- implemented layer importing and exporting
- frontend: added GraphiQL playground
- frontend: improved layer operations view
- frontend: added breadcrumb navigation throughout frontend, removed back buttons
- big performance improvements when fetching CIs using large list of selected ciids by using CTEs (common table expressions) instead of Postgres arrays
- big performance improvements by introducing dataloader for GraphQL, batching database calls together
- performance improvements for various data fetching methods 
- performance improvements and memory improvements in data ingest
- performance improvements for effective trait calculations

### Changed
- split relations into outgoing and incoming relations in both backend, frontend and GraphQL API
- gridview: introduced proper column IDs instead of relying on attribue name being unique
- frontend: reduced network traffic when performing CI searches

### Fixed
- bugfixes in data ingest code
- frontend: fixed layout issues

## [1.4.1] - 2021-09-09

### Fixed
- bugfix in gridview frontend when having a context column with a dot (".") in it
- bugfix in frontend: long names in CI search result list are now properly cut off

## [1.4.0] - 2021-09-07

### Added
- big performance improvements to attribute and relations fetching due to introduction of *-latest caching tables
- performance improvements to attribute fetching due to inversion of CIIDSelection, when feasible
- performance improvements to traits fetching
- performance improvements to CI search when specifying traits
- performance improvements to changeset in timerange fetching

### Fixed

- resolving of dependent traits is now consistent and its order is documented
- selection of what constitutes outdated attributes and relations for archiving purposes was wrong, fixed now
 
## [1.3.0] - 2021-09-03

### Added
- traits can now have optional relations
- instance-local caching for effective traits, providing a speedup for all usecases using traits
- added code functionality to get CI(s) with a trait that ALSO have a certain attribute, including attribute-value
- lots of documentation improvements and updates
- added automated tests
- integrated automated tests into CICD pipeline
- technical frontend: implemented trait view
- technical frontend: added badges to various tabs, showing the number of items inside

### Changed
- technical frontend: reworked effective trait view for CI
- technical frontend: reordered main menu items
- reworked archiving/deletion of outdated data (attributes/relations/changesets)
- made automated tests runnable via command line (`dotnet test`)

### Fixed
- fixed bug in PartitionModel regarding timezones
- fixed ansible inventory scan test

## [1.2.0] - 2021-08-26

### Added
- configuration option for PII
- configuration option for authentication option ValidateIssuer
- validation of various elements' IDs, so that they follow the naming rules: traits, predicates, ...
- validation Plugin, still PoC status and lots of work to be done

### Changed
- big rework of TraitConfigData handling
- reworked generic JSON ingest plugin to use omnikeeper data for its own configuration
- reworked grid view to use omnikeeper data for its own configuration
- removed predicate constraints
- rework of Plugin Traits

### Fixed
- technical frontend: fix non-working diffing tool when viewing traits
- technical frontend: fixed hardcoded timeranges for changeset queries
- missing roles key in token now produces warning, not exception
- updated jwtbearer library, fixing vulnerability
<!-- 
git log 1.1.0..1.2.0 --oneline 
-->
