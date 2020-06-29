using Keycloak.Net;
using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KeycloakOnlineInboundLayerPlugin
{
    public class KeycloakLayerAccessProxy : IOnlineInboundLayerAccessProxy
    {
        private readonly KeycloakClient client;
        private readonly string realm;
        private readonly ExternalIDMapper mapper;
        private readonly Layer layer;

        public KeycloakLayerAccessProxy(KeycloakClient client, string realm, ExternalIDMapper mapper, Layer layer)
        {
            this.client = client;
            this.realm = realm;
            this.mapper = mapper;
            this.layer = layer;
        }

        private IEnumerable<CIAttribute> BuildAttributesFromUser(Keycloak.Net.Models.Users.User user, Guid ciid, Keycloak.Net.Models.Common.Mapping roleMappings)
        {
            /* TODO: with external data sources, we don't have a single source of attributes and hence
                * we don't have a single source of attribute IDs (or relation IDs, or...)
                * we might need to move attribute IDs and all other IDs that can also come from external data sources to Guids?
                * Or is there another way?
            */
            var changesetID = -1; // TODO: the same for changeset IDs
            var CIName = (user.FirstName != null && user.FirstName.Length > 0 && user.LastName != null && user.LastName.Length > 0) ? $"{user.FirstName} {user.LastName}" : user.UserName;

            CIAttribute BuildAttribute(string name, Guid ciid, IAttributeValue value, long changesetID)
            {
                // create a deterministic, depdendent guid from the ciid + attribute name + value
                //static Guid attributeIDGenerator(Guid ciid, string attributeName, Layer layer, IAttributeValue value) => GuidUtility.Create(ciid, attributeName + layer.ID.ToString() + value.Value2String());
                var id = GuidUtility.Create(ciid, name + layer.ID.ToString());// TODO: determine if we need to factor in value or not
                return CIAttribute.Build(id, name, ciid, value, AttributeState.New, changesetID);
            }

            yield return BuildAttribute("__name", ciid, AttributeScalarValueText.Build($"User {CIName}"), changesetID);
            yield return BuildAttribute("user.email", ciid, AttributeScalarValueText.Build(user.Email), changesetID);
            yield return BuildAttribute("user.username", ciid, AttributeScalarValueText.Build(user.UserName), changesetID);
            yield return BuildAttribute("user.first_name", ciid, AttributeScalarValueText.Build(user.FirstName), changesetID);
            yield return BuildAttribute("user.last_name", ciid, AttributeScalarValueText.Build(user.LastName), changesetID);
            yield return BuildAttribute("keycloak.id", ciid, AttributeScalarValueText.Build(user.Id), changesetID);

            // roles
            if (roleMappings != null)
            {
                yield return BuildAttribute("keycloak.client_mappings", ciid, AttributeScalarValueJSON.Build(JsonConvert.SerializeObject(roleMappings.ClientMappings)), changesetID);
            }

        }

        public async IAsyncEnumerable<CIAttribute> GetAttributes(ISet<Guid> ciids)
        {
            await mapper.Setup();

            foreach (var (ciid, externalID) in mapper.GetIDPairs(ciids))
            {
                var user = await client.GetUserAsync(realm, externalID);
                var roleMappings = await client.GetRoleMappingsForUserAsync(realm, externalID);

                foreach (var a in BuildAttributesFromUser(user, ciid, roleMappings))
                    yield return a;
            }
        }

        public async IAsyncEnumerable<CIAttribute> GetAttributesWithName(string name)
        {
            await mapper.Setup();

            var users = await client.GetUsersAsync(realm, true, null, null, null, null, 99999, null, null); // TODO, HACK: magic number, how to properly get all user IDs?
            foreach (var user in users)
            {
                var ciid = mapper.GetCIID(user.Id);
                if (ciid.HasValue)
                {
                    foreach (var a in BuildAttributesFromUser(user, ciid.Value, null))
                        if (a.Name.Equals(name)) // HACK: we are getting ALL attributes of the user and then discard them again, except for one
                            yield return a;
                }
            }
        }

        public IAsyncEnumerable<Relation> GetRelations(Guid? ciid, IRelationModel.IncludeRelationDirections ird)
        {
            return AsyncEnumerable.Empty<Relation>();// TODO: implement
        }

        public IAsyncEnumerable<Relation> GetRelationsWithPredicateID(string predicateID)
        {
            return AsyncEnumerable.Empty<Relation>();// TODO: implement
        }
    }
}
