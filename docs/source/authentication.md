# Authentication

TODO

## Disabling Authentication 

Authentication (and authorization) can be fully disabled through environment variables in backend and frontend:

### Disable authentication in backend

Set the following environment variables:
```
Authentication__debugAllowAll=true
Authorization__debugAllowAll=true
```

### Disable authentication in frontend

Set the following environment variable:
```
REACT_APP_DISABLE_AUTH=true
```

After disabling authentication and authorization, omnikeeper works without an IDP, such as Keycloak. Note that all user interactions (reads, writes) will be attributed to the same user "anonymous".

## Authentication using Keycloak

TODO

## Authentication using Authentik

TODO

### Scope Mapping Example

```python
import random
import uuid

# generate a consistent UUID from the user's uid
rnd = random.Random()
rnd.seed(user.uid)
user_uuid = uuid.UUID(int=rnd.getrandbits(128), version=4)

return {
  "id": user_uuid,
  "resource_access": {
    "omnikeeper": {
      "roles": ["__ok_superuser"] # TODO
    }
  }
}
```
