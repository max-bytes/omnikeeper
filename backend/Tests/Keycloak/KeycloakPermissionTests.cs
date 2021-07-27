using Flurl;
using Flurl.Http;
using Keycloak.Protection.Net;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Omnikeeper.Model.Keycloak;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Keycloak
{
    class KeycloakPermissionTests
    {
        [Test]
        public async Task Test()
        {
            var keycloakURL = "https://localhost:9095/auth";
            var realm = "landscape";
            var clientId = "landscape-omnikeeper-api";
            var accessToken = "eyJhbGciOiJSUzI1NiIsInR5cCIgOiAiSldUIiwia2lkIiA6ICJla3JuRmR4b3dGMGtvamZvdWdVV0JmTFhoaUdfUjRpMlN1Slk3dzJmT1ZnIn0.eyJleHAiOjE2MjcwMzkzMzIsImlhdCI6MTYyNzAzOTAzMiwiYXV0aF90aW1lIjoxNjI3MDI5ODU3LCJqdGkiOiJmMjQ3ZjJhNy03OTQ2LTQ1YTQtOGFjZi0wMjMxYTllZDYzZmUiLCJpc3MiOiJodHRwczovL2xvY2FsaG9zdDo5MDk1L2F1dGgvcmVhbG1zL2xhbmRzY2FwZSIsImF1ZCI6WyJsYW5kc2NhcGUtb21uaWtlZXBlciIsImFjY291bnQiXSwic3ViIjoiOTAyOTRlZTQtNDQzMS00ZTRlLThkMTYtMmRkNzY2YzQxNmNlIiwidHlwIjoiQmVhcmVyIiwiYXpwIjoibGFuZHNjYXBlLW9tbmlrZWVwZXIiLCJub25jZSI6ImM1YmMxMzQ3LTI4ZGUtNDdiOC1hYmFiLWZmNzlmZmQ3YzExNyIsInNlc3Npb25fc3RhdGUiOiJiNTNiNjViOS0wYmRlLTQwMjUtOGZlNy04MjlkZTgyNzJiZGEiLCJhY3IiOiIwIiwiYWxsb3dlZC1vcmlnaW5zIjpbIioiXSwicmVhbG1fYWNjZXNzIjp7InJvbGVzIjpbIm9mZmxpbmVfYWNjZXNzIiwiZGVmYXVsdC1yb2xlcy1sYW5kc2NhcGUiLCJ1bWFfYXV0aG9yaXphdGlvbiJdfSwicmVzb3VyY2VfYWNjZXNzIjp7ImFjY291bnQiOnsicm9sZXMiOlsibWFuYWdlLWFjY291bnQiLCJtYW5hZ2UtYWNjb3VudC1saW5rcyIsInZpZXctcHJvZmlsZSJdfX0sInNjb3BlIjoib3BlbmlkIHByb2ZpbGUgZW1haWwgZ29vZC1zZXJ2aWNlIiwiZW1haWxfdmVyaWZpZWQiOmZhbHNlLCJuYW1lIjoiTWF4aW1pbGlhbiBDc3VrIiwiZ3JvdXBzIjpbXSwiaWQiOiI5MDI5NGVlNC00NDMxLTRlNGUtOGQxNi0yZGQ3NjZjNDE2Y2UiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJtY3N1ayIsImdpdmVuX25hbWUiOiJNYXhpbWlsaWFuIiwiZmFtaWx5X25hbWUiOiJDc3VrIiwiZW1haWwiOiJtYXhpbWlsaWFuLmNzdWtAZ214LmF0In0.evJ84N_NMU7H9cLMMPUltXNilIZlrcpra1UZ6OF9m3c1qMp5mMnh4VTTCMSvUjR0jflYW8I45JY5UrpQk_9gmGcyuzxdVXq45ExyJ1VSz-qdRRq82QX_sbuNkoJ9OfiFvIJhsqwuh1lA69xPqLdYwbrhx9-6Hai_OtiVMDIByFWu-SO3S-CUBKy6PaqScYm0z1D5CcfCcnW6pJeTvWDRDdRxag-fFvQt8iqtxaSNFPMYisWWRrFmGNJSXPAL0MhEPo2gL62R43f4sSyax-l1Dn7E-LixbWPbb6LC7GPBjsfVvmVCoyma83CagV6mCeWKGKIW2SmKT4_XKwjU8tEQuw";

            var permission = "layer_cmdb#write";

            // get specific permission
            try
            {
                var y = await keycloakURL
                    .AppendPathSegment($"/realms/{realm}/protocol/openid-connect/token")
                    .WithHeader("Authorization", $"Bearer {accessToken}")
                    .PostUrlEncodedAsync(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:uma-ticket"),
                        new KeyValuePair<string, string>("response_mode", "decision"),
                        new KeyValuePair<string, string>("audience", clientId),
                        new KeyValuePair<string, string>("permission", permission)
                    })
                    .ReceiveJson()
                    .ConfigureAwait(false);
                bool result = y.result;
            }
            catch (Exception e)
            {
                Console.WriteLine("!");
                //return false;
            }
            //// get all permissions
            //var x = await keycloakURL
            //    .AppendPathSegment($"/realms/{realm}/protocol/openid-connect/token")
            //    .WithHeader("Authorization", $"Bearer {accessToken}")
            //    .PostUrlEncodedAsync(new List<KeyValuePair<string, string>>
            //    {
            //        new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:uma-ticket"),
            //        new KeyValuePair<string, string>("response_mode", "permissions"),
            //        new KeyValuePair<string, string>("audience", clientId)
            //    })
            //    .ReceiveString()
            //    .ConfigureAwait(false);
            //}

            Console.WriteLine("X");
        }
    }
}
