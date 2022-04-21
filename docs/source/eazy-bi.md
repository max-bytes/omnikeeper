# EazyBI integration

Data in omnikeeper can be imported into EazyBI for analysis and reporting. Through omnikeeper's GraphQL API, arbitrary data can be exported. On EazyBI's side, the REST API importer can be retrofitted to work with GraphQL. 

## Authentication

omnikeeper uses Keycloak, which in turn offers OAuth 2.0 authentication. EazyBI supports OAuth 2.0 using Client Secret authentication. Therefore it's necessary to configure Keycloak with a client that has its `access-type` set to `confidental`. See <https://stackoverflow.com/a/44791499> for further details.

## example EazyBI import settings

What follows is an example import of a specific set of trait entities from omnikeeper into EazyBI. The example assumes that a corresponding trait is already defined in omnikeeper. Different usecases may be implemented by adjusting the example's GraphQL query and import settings.

- REST API importer
- Source Data URL (replace `[[omnikeeper-url]]` with the correct URL): `[[omnikeeper-url]]/graphql?query=query{traitEntities(layers:["layer_1"]){test_trait{all{entity{attributeA, attributeB}}}}}`
- Authentication Parameters
  - Authentication type: OAuth 2.0
  - ClientID: as defined in Keycloak (default: `omnikeeper`)
  - Client secret: as defined in Keycloak
  - Authorize and Token URL: as defined by Keycloak; can also be found at `[[omnikeeper-url]]/.well-known/openid-configuration`
- Content Parameters
  - Content Type: JSON
  - Data path: `$.data.traitEntities.test_trait.all`
  - Custom JavaScript code: none
- Field mapping: as desired