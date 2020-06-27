using Keycloak.Net;
using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using LandscapeRegistry.Entity.AttributeValues;
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

        public KeycloakLayerAccessProxy(KeycloakClient client, string realm, ExternalIDMapper mapper)
        {
            this.client = client;
            this.realm = realm;
            this.mapper = mapper;
        }

        private IEnumerable<CIAttribute> BuildAttributesFromUser(Keycloak.Net.Models.Users.User user, Guid ciid)
        {
            /* TODO: this is a HUGE issue! With external data sources, we don't have a single source of attributes and hence
                * we don't have a single source of attribute IDs (or relation IDs, or...)
                * we might need to move attribute IDs and all other IDs that can also come from external data sources to Guids?
                * Or is there another way?
            */
            Guid attributeIDGenerator() => Guid.NewGuid();
            var changesetID = -1; // TODO: the same for changeset IDs
            var name = (user.FirstName != null && user.FirstName.Length > 0 && user.LastName != null && user.LastName.Length > 0) ? $"{user.FirstName} {user.LastName}" : user.UserName;
            yield return CIAttribute.Build(attributeIDGenerator(), "__name", ciid, AttributeScalarValueText.Build($"User {name}"), AttributeState.New, changesetID);
            yield return CIAttribute.Build(attributeIDGenerator(), "user.keycloak_id", ciid, AttributeScalarValueText.Build(user.Id), AttributeState.New, changesetID);
            yield return CIAttribute.Build(attributeIDGenerator(), "user.email", ciid, AttributeScalarValueText.Build(user.Email), AttributeState.New, changesetID);
            yield return CIAttribute.Build(attributeIDGenerator(), "user.username", ciid, AttributeScalarValueText.Build(user.UserName), AttributeState.New, changesetID);
            yield return CIAttribute.Build(attributeIDGenerator(), "user.first_name", ciid, AttributeScalarValueText.Build(user.FirstName), AttributeState.New, changesetID);
            yield return CIAttribute.Build(attributeIDGenerator(), "user.last_name", ciid, AttributeScalarValueText.Build(user.LastName), AttributeState.New, changesetID);
        }

        public async IAsyncEnumerable<CIAttribute> GetAttributes(ISet<Guid> ciids)
        {
            await mapper.Setup();

            foreach (var (ciid, externalID) in mapper.GetIDPairs(ciids))
            {
                var user = await client.GetUserAsync(realm, externalID);

                foreach (var a in BuildAttributesFromUser(user, ciid))
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
                    foreach (var a in BuildAttributesFromUser(user, ciid.Value))
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
