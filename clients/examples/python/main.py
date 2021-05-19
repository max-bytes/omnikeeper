import time
import okclient
from pprint import pprint
from okclient.api import ci_api
from okclient.model.bulk_ci_attribute_layer_scope_dto import BulkCIAttributeLayerScopeDTO
from okclient.model.ci_attribute_dto import CIAttributeDTO
from oauthlib.oauth2 import LegacyApplicationClient
from requests_oauthlib import OAuth2Session

apiVersion = "1"
username='omnikeeper-client-library-test' 
password='omnikeeper-client-library-test'
serverURL = "https://acme.omnikeeper-dev.bymhx.at/backend"

# get access token via oauth from keycloak
oauth = OAuth2Session(client=LegacyApplicationClient(client_id='landscape-omnikeeper'))
token = oauth.fetch_token(
    token_url='https://auth-dev.mhx.at/auth/realms/acme/protocol/openid-connect/token',
    username=username, 
    password=password, 
    client_id='landscape-omnikeeper')

# configure API client
configuration = okclient.Configuration(
    host = serverURL
)
configuration.access_token = token['access_token']


# Enter a context with an instance of the API client
with okclient.ApiClient(configuration) as api_client:
    # Create an instance of the API class
    api_instance = ci_api.CIApi(api_client)
    
    try:
        # bulk replace all attributes in specified layer
        ciids = api_instance.get_all_ciids(apiVersion)

        print(ciids)

    except okclient.ApiException as e:
        print("Exception when calling CIApi->get_all_ciids: %s\n" % e)
