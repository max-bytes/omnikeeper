# Authorization

Authorization (as opposed to Authentication) is the process of giving a user permission to access resources within omnikeeper (or decline access).

This page describes what concepts and features omnikeeper offers to support different use cases regarding authorization.

## User Roles

omnikeeper uses a (JWT) token-based authentication method. Tokens are typically created and managed by an IDP (Identity Provider), such as Keycloak. Within a token, so-called "claims" about the user are specified. Among other things, claims can be used to specify roles that the particular user has. omnikeeper uses these roles as the basis for authorization and governing what a user can do and see within omnikeeper.
As an example, the roles within a token suitable for omnikeeper might look like the following:
```json
{
  "...": "...",
  "resource_access": {
      "landscape-omnikeeper": {
        "roles": [
          "Role A",
          "Role B",
          "Administrator"
        ]
      },
  "...": "..."
}
```
The keys `resource_access` and `roles` are hardcoded (specified by Keycloak). The key `landscape-omnikeeper` is dynamic and must equal the audience specified in the omnikeeper [[application configuration|configuration_application-configuation]] (key `Authentication.Audience`).

Managing of user roles is outside the scope of omnikeeper itself. omnikeeper is merely using the defined user roles for authorization decisions. Role management must be done by the IDP.

Note: although strongly related, do not confuse user roles (as specified inside tokens as claims) with AuthRoles (as specified in omnikeeper).

## AuthRoles

An AuthRole (short for Authorization Role) is an item that governs how to map incoming user roles inside the token's claims to permissions within omnikeeper. An AuthRole consists of:
- a string-based ID, which identifies the AuthRole and is the key for matching it to user roles
- an array of string-based permission tokens, that specify what a user who has that role is allowed to do within omnikeeper

When a user connects, who has a role that matches the ID of an AuthRole, they are granted the permissions of that AuthRole.

A user with more than one matching AuthRole is granted the union of all the permissions of the matching AuthRoles.

AuthRoles can be most easily managed through the management user interface of the technical frontend (/manage/auth-roles). However, AuthRoles are themselves regular CIs within omnikeeper's own configuration layer(s). That makes it possible to manage AuthRoles through working with their CIs and their Attributes directly. Just make sure that the CIs conform to the core trait `__meta.config.authRole` to be recognized as AuthRoles.

## Permissions

omnikeeper understands permissions that determine what a user can see and do within omnikeeper. Each permission has a unique textual string that represents it. For example, the permissions for read access to a layer with ID `xyz` is called `ok.layer.xyz#read`. 

Here's a list of all available permissions:
- Layers
    - read permissions: `ok.layer.[layer-ID]#read` (substitute `[layer-ID]` with a correct layer-ID)
    - write permissions: `ok.layer.[layer-ID]#write` (substitute `[layer-ID]` with a correct layer-ID)
- Management: `ok.layer.management`
    - Allows access to the full management interface. Note that - for lots of management tasks - you also need read/write access to the omnikeeper config layers
- ... more permissions to come ...

To keep permissions simple, there is no concept of nested permissions or permission grouping (yet). Case in point: to actually be able to write to a layer, both read and write permissions are required. There is no single permission for read AND write. In other words, granting a layer write permissions without a layer read permissions does not make sense.

## Super User Role

When a user with the special user role `__ok_superuser` connects, it is automatically given ALL available permissions. This works independently of any configured AuthRoles or other roles the user might have.

You do not have to manually create an AuthRole with the ID `__ok_superuser` for this to work. In fact, it's discouraged as this could create confusion and inconsistencies. 

The super user role serves two primary purposes:
- in a simple setup where you do not want to bother with AuthRoles or authorization in general (f.e. during development or in simple production environments)
- to be able to bootstrap omnikeeper. Because omnikeeper keeps its own configuration within layers, there's a chicken-egg problem. See [[Configuration Layers|configuration_configuration-layers]] for an explanation.

## Keycloak

TODO

## Diagram of authorization flow

Example using Keycloak and layer access

 ![Authorization Overview](assets/drawio/authz-overview-Seite-1.svg)