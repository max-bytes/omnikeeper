# Application Stack Configuration

Much of omnikeeper's configuration can be managed through the technical frontend. However, certain basic configuration settings, such as the database connection need to be setup before starting omnikeeper. The way this is done is through environment variables. Both the technical frontend and the backend/core use this approach to configure initial settings.

## Backend/Core

### Port

The backend docker image exposes port 80, over which all communication is done.

### Environment variables

- ConnectionStrings__OmnikeeperDatabaseConnection
    - Connection string for connecting to postgres database
    - Example: `Server=db;User Id=db_username;Password=db_password;Database=omnikeeper;Pooling=true`
- Authentication__Audience
    - Keycloak client ID
    - Example: `omnikeeper`
- Authentication__Authority
    - URL to keycloak auth (including realm)
    - Example: `http://keycloak-url.com/auth/realms/acme`
- Authentication__ValidateIssuer
    - Boolean to set whether the backend should validate the issuer in the JWT token or not
    - Default: true
- CORS__AllowedHosts
    - CORS setting, defining what hosts may connect. Should typically be set to the frontend URL
    - Example: `https://omnikeeper-frontend-url.com`
- BaseURL
    - Optional base URL, to run the backend in non-root URLs
    - Default: ` ` (empty string)
    - Example: `/backend`
- ShowPII
    - Boolean, whether or not to show personally identifiable information in exception messages, see https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/wiki/PII
    - Default: false 

### Logs
Backend application logs can be found inside the container at `/app/Logs`. You might want to map this directory to a directory on the docker host.


## Technical Frontend

### Port

The frontend docker image exposes port 80, over which all communication is done.

###  Environment variables

- PUBLIC_URL_DYNAMIC: 
    - Full qualified URL where frontend is reachable
    - example: `https://omnikeeper-frontend-url.com`
- REACT_APP_KEYCLOAK_URL: 
    - URL to keycloak root
    - example: `http://keycloak-url.com`
- REACT_APP_KEYCLOAK_REALM:
    - Keycloak realm
    - example: `acme`
- REACT_APP_KEYCLOAK_CLIENT_ID: 
    - Keycloak client ID
    - example: `omnikeeper`
- REACT_APP_BACKEND_URL:
    - full qualified URL to backend
    - example: `https://omnikeeper-backend-url.com`
- REACT_APP_BASE_NAME
    - Optional base URL, to run the frontend in non-root URLs
    - default: `/`
    - example: `/frontend`
- REACT_APP_AGGRID_LICENCE_KEY
    - Optional, license key for AgGrid Enterprise; if set and valid, enables AgGrid Enterprise features
    - default: ``

### Logs
Frontend application logs can be found inside the container at `/var/log/nginx`. You might want to map this directory to a directory on the docker host.


## Database

omnikeeper requires a recent (11+) postgres database to store its data and configuration. Use the backend environment variable `ConnectionStrings__OmnikeeperDatabaseConnection` to connect the omnikeeper backend to the database.

Make sure the specified database is created beforehand because omnikeeper does not create it. On startup, omnikeeper performs its own database migrations and keeps it in sync with updates. Therefore the database user needs essentially ALL privileges (SELECT, INSERT, CREATE, ...) on the database (TODO: be more concrete).
