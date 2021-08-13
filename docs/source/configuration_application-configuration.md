# Application Configuration

Much of omnikeeper's configuration can be managed through the technical frontend. However, certain basic configuration settings, such as the database connection need to be setup before starting omnikeeper. The way this is done is through environment variables. Both the technical frontend and the backend/core use this approach to configure initial settings.

## Backend/Core environment variables

- ConnectionStrings__LandscapeDatabaseConnection
    - Connection string for connecting to postgres database
    - Example: `Server=db;User Id=db_username;Password=db_password;Database=omnikeeper;Pooling=true`
- Authentication__Audience
    - Keycloak client ID
    - Example: `omnikeeper`
- Authentication__Authority
    - URL to keycloak auth (including realm)
    - Example: `http://keycloak-url.com/auth/realms/acme`
- CORS__AllowedHosts
    - CORS setting, defining what hosts may connect. Should typically be set to the frontend URL
    - Example: `https://omnikeeper-frontend-url.com`
- BaseURL
    - Optional base URL, to run the backend in non-root URLs
    - Default: ` ` (empty string)
    - Example: `/backend`

## Technical Frontend environment variables:
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


TODO: write about how to configure omnikeeper core docker container... environment variables, config files, mappings, ...
