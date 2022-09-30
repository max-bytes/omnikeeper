# Issues

DRAFT

omnikeeper has the concept of "issues" (sometimes called "ok-issues" to distinguish from other concepts named issues). Issues are a generic way for processes and omnikeeper itself to document problems that occur. This can be problems in the data during ingest or during processing (f.e. when running a compute layer). Or it can be problems in the configuration of omnikeeper itself. There is also the possibility to create so-called "validators", whose purpose is to find problems in the data and report them as issues.

Issues are - in typical omnikeeper fashion - CIs. CIs that fulfill the core trait `__meta.issue.issue`. These issues "live" in a special layer named `__okissues`. But the technical frontend also has a dedicated section specifically for issues, which should help browsing and discovering them.

## Structure of an issue

- message
- affected CIs
- type
  - possible values:
    - DataIngest
    - ComputeLayerBrain
    - Validator
- context
- group
- ID
- first occurance

## Validators

TODO