# Data Ingest

DRAFT

How to get data into omnikeeper?

## General

- in this document, the focus is on bulk updates for importing data from other data sources, not small changes
- Import always updates the full layer
  - passed data therefore needs to be complete, no partial imports possible
  - any data that is not contained within the data that is passed will be deleted from the layer
  - omnikeeper figures out necessary changes and applies only those in a single changeset
  - data that is the same in the import and what’s already in the layer will not lead to a change in omnikeeper
  - goal for imports is to be “idempotent”: a second import of exactly the same data should not lead to any more changes (=no second changeset will be created)

## Generic Raw Ingest

via plugin OKPluginGenericJSONIngest

- JSON based REST POST endpoint: api/v1/ingest/genericJSON/data
- Data is passed in a single request with a (huge) JSON structure
- JSON structure is strictly defined. Example:

    ```json
    {
      "cis": [
        {
          "tempID": "foo-01",
          "idMethod": {
            "type": "OKPluginGenericJSONIngest.InboundIDMethodByData, OKPluginGenericJSONIngest",
            "attributes": [
              "foo"
            ]
          },
          "sameTempIDHandling": "DropAndWarn",
          "sameTargetCIHandling": "Error",
          "noFoundTargetCIHandling": "CreateNew",
          "attributes": [
            {
              "name": "foo",
              "value": {
                "type": "Text",
                "isArray": false,
                "values": [
                  "bar"
                ]
              }
            }
          ]
        },
        {
            "tempID": "foo-02",
            "...": "..."
        }
      ],
      "relations": [
        {
          "from": "foo-01",
          "predicate": "related_to",
          "to": "foo-02"
        }
      ]
    }
    ```

- Explanations:
  - cis: array of all candidate-CIs in the import
    - tempID: ID that identifies this candidate-CI. Should be unique over all candidate-CIs within the JSON document. Does NOT have any relevance outside this document.
    - idMethod:
      - describes the method how this candidate-CI should be matched against existing data within the read-layerset in order to find a matching CI
      - further details, see the IDMethod chapter
    - sameTempIDHandling:
      - governs how the import should react when detecting that this candidate-CI has the same tempID as another candidate-CI within the JSON document
      - possible values:
        - Drop: drop the candidate-CI
        - DropAndWarn: same as Drop, but also generate a warning/issue
    - sameTargetCIHandling:
      - governs how the import should behave in case when multiple candidate-CIs target the same CI (=the idMethod returned the same target CI for multiple candidate-CIs)
      - possible values:
        - Error: signal an error and cancel the whole import
        - Drop: drop this candidate-CI
        - DropAndWarn: same as Drop, but also generate a warning/issue
        - Evade: switch the candidate-CI to a completely new CI that will be created during the import
        - EvadeAndWarn: same as Evade, but also generate a warning/issue
        - Merge: try to merge this candidate-CI with the previous candidate-CI that targets the same CI
  - noFoundTargetCIHandling
    - governs how the import should behave in case when the candidate-CI matches no target CI (=the idMethod found no target CI)
    - possible values:
      - CreateNew: create a brand new CI
      - CreateNewAndWarn: same as CreateNew, but also generate a warning/issue
      - Drop: drop the candidate-CI, do not import it
      - DropAndWarn: same as Drop, but also generate a warning/issue
  - attributes:
    - array of attributes that belong to this candidate-CI
    - name:
    - value:
      - type: governs the attribute’s type. Possible values:
        - Text, MultilineText, Integer, JSON, YAML, Image, Mask, Double, Boolean, DateTimeWithOffset
      - isArray: whether or not the `values` field should be treated as an array or as a scalar value
      - values: the actual values, string-encoded; always an array, even when the actual value is a scalar. In that case, use an array with a single item
  - relations: array of all relation-candidates in the import
    - from: tempID of the candidate-CI from which this relation-candidate starts
    - predicate: predicateID of this relation-candidate; predicateID needs to fulfill regex `^[a-z0-9_.]+$`
    - to: tempID of the candidate-CI at which this relation-candidate ends
- In certain cases, such as when duplicate tempIDs are encountered, the processing order is relevant. The document is read top-to-bottom, meaning that CI- and relation-candidates further up in the document are processed first.
- “Warn” in this context (like with sameTempIDHandling) means that an ok-issue will be generated.

## IDMethod

The IDMethod is the configuration that governs how omnikeeper should try to find a suitable target-CI for a given candidate-CI. A target-CI is a CI already existing in omnikeeper’s current data.

Note that all IDMethods actually produce an ordered lists of CIIDs, not just a single CIID. The order determines the “fitness” of the target-CIs, the best fitting target-CI being the first item in the list.  Only in the end of the process, the final target-CI for a candidate is chosen by taking the first item in the list. While that distinction seems pointless for simple usecases, it actually makes a difference in more complex scenarios involving aggregate IDMethods. All IDMethods will produce a consistent ordering, that - barring any other way of ordering the fitting CIs - will use the CIID as the last order criteria. Ordering by CIID as a last resort means that the importer at the very least stays persistent between imports and does not change the mapping on each import.

The possible IDMethods:

- ByAttribute:
  - the most basic IDMethod: given a list of attribute-names, it tries to find a target-CI by looking for CIs that contain the specified attributes AND those attributes have the same value as the attributes of the candidate-CI, for each attribute respectively.
- ByData
  - similar to ByAttribute, but allows specifying full attributes that will be used for comparison instead of refering to attributes in the candidate-CI by name. Useful in scenarios when the attribute(s) to look for are not actually contained within the attributes of the candidate-CI
- ByRelatedTempID
  - find target-CIs that are related to a candidate-CI (specified via their tempID) through a relation with specified predicateID and direction.
- ByTemporaryCIID
  - find target-CI that isalready associated with a candidate-CI specified by a tempID. Current name is misleading, should better be named “ByTempID”.
- Aggregates:
  - ByUnion
    - concats the lists of resulting CIIDs of the inner IDMethods. This is similar to a logical “OR” operation, the main difference being that it also maintains the ordering.
  - ByIntersect
    - performs an intersect on the lists of resulting CIIDs of the inner IDMethods. In other words, this IDMethod returns only those CIIDs that are in the result list of every inner IDMethod. This is similar to a logical “AND” operation, the main difference being that it also maintains the ordering.

The results of the IDMethod are one of three possibilities. If the IDMethod finds…

- …a single target-CI that fits, the importer will write the attributes of the candidate-CI to that target-CI
- …no target-CI that fits, the noFoundTargetCIHandling property comes into play
- …multiple target-CIs that fit, the importer will pick the first target-CI in the result list, which corresponds to the best fitting CI, and will write the attributes of the candidate-CI to that target-CI

## Generic JSON Ingest

via plugin OKPluginGenericJSONIngest

TODO

### JSON ingest data flow chart

![JSON ingest data flow chart](assets/drawio/generic-json-ingest-flow.svg)

## Custom Plugins

### Insight Discovery Scan Ingest

via plugin OKPluginInsightDiscoveryScanIngest

TODO

### Ansible Inventory Scan Ingest (outdated)

via plugin OKPluginAnsibleInventoryScanIngest

TODO

## Misc

### Concept for third-party component and overall flow

![Concept for third-party component and overall flow](assets/drawio/generic-json-ingest-overall-flow.svg)
