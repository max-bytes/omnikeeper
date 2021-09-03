
# Change Log
All notable changes to this project will be documented in this file.
 
The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).
 
<!--
## [Unreleased] - 2021-09-03
 
### Added
*Empty*
 
### Changed
*Empty*
 
### Fixed
*Empty*

-->
 
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