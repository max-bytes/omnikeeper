# Authorization

TODO
 ![Authorization Overview](keycloak-authz-concepts-overview.svg)

## AuthRoles

AuthRoles (short for Authorization Role) are the blocks that govern how to map incoming user roles inside the token claims to permissions within omnikeeper. An AuthRole consists of:
- a string-based ID, which identifies the AuthRole and is the key for matching it to user roles
- an array of string-based permission tokens, that specify what a user who has that role is allowed to do within omnikeeper

When a user who has a role that matches the ID of an AuthRole connects, they are assigned the permissions of that AuthRole.

A user with more than one matching AuthRole is assigned the union of all the permissions of the matching AuthRoles.

AuthRoles can be most easily managed through the management user interface of the technical frontend (/manage/auth-roles). However, AuthRoles are themselves regular CIs within omnikeeper's own configuration layer. That makes it possible to manage AuthRoles through working with their CIs and their Attributes directly. Just make sure that AuthRoles conform to the core trait `__meta.config.authRole`.

## Permissions

TODO

## Super User Role

TODO

`__ok_superuser`

## Keycloak

TODO