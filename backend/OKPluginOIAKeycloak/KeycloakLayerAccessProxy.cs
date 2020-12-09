using Keycloak.Net;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OKPluginOIAKeycloak
{
    public class KeycloakLayerAccessProxy : ILayerAccessProxy
    {
        private readonly KeycloakClient client;
        private readonly string realm;
        private readonly KeycloakScopedExternalIDMapper mapper;
        private readonly Layer layer;

        private static readonly Guid staticChangesetID = GuidUtility.Create(new Guid("a09018d6-d302-4137-acae-a81f2aa1a243"), "keycloak");

        public KeycloakLayerAccessProxy(KeycloakClient client, string realm, KeycloakScopedExternalIDMapper mapper, Layer layer)
        {
            this.client = client;
            this.realm = realm;
            this.mapper = mapper;
            this.layer = layer;
        }

        private IEnumerable<CIAttribute> BuildAttributesFromUser(Keycloak.Net.Models.Users.User user, Guid ciid, Keycloak.Net.Models.Common.Mapping? roleMappings)
        {
            /* with external data sources, we don't have a single source of attributes and hence
                * we don't have a single source of attribute IDs (or relation IDs, or...)
                * we use guids for attribute IDs and all other IDs that can also come from external data sources
            */
            var changesetID = staticChangesetID; // TODO: how to work with changesets when its online access?
            var CIName = (user.FirstName.Length > 0 && user.LastName.Length > 0) ? $"{user.FirstName} {user.LastName}" : user.UserName;

            CIAttribute BuildAttribute(string name, Guid ciid, IAttributeValue value, Guid changesetID)
            {
                // create a deterministic, dependent guid from the ciid + attribute name + value
                var id = GuidUtility.Create(ciid, name + layer.ID.ToString());// TODO: determine if we need to factor in value or not
                return new CIAttribute(id, name, ciid, value, AttributeState.New, changesetID);
            }

            yield return BuildAttribute(ICIModel.NameAttribute, ciid, new AttributeScalarValueText($"User {CIName}"), changesetID);
            yield return BuildAttribute("user.email", ciid, new AttributeScalarValueText(user.Email), changesetID);
            yield return BuildAttribute("user.username", ciid, new AttributeScalarValueText(user.UserName), changesetID);
            yield return BuildAttribute("user.first_name", ciid, new AttributeScalarValueText(user.FirstName), changesetID);
            yield return BuildAttribute("user.last_name", ciid, new AttributeScalarValueText(user.LastName), changesetID);
            yield return BuildAttribute("keycloak.id", ciid, new AttributeScalarValueText(user.Id), changesetID);

            // roles
            if (roleMappings != null && roleMappings.ClientMappings != null)
            {
                yield return BuildAttribute("keycloak.client_mappings", ciid, AttributeScalarValueJSON.BuildFromString(JsonConvert.SerializeObject(roleMappings.ClientMappings)), changesetID);
            }

        }

        public async Task<CIAttribute?> GetAttribute(string name, Guid ciid, TimeThreshold atTime)
        {
            if (!atTime.IsLatest) return null; // we don't have historic information

            var externalID = mapper.GetExternalID(ciid);
            if (!externalID.HasValue)
                return null;

            var user = await client.GetUserAsync(realm, externalID.Value.ID);
            var roleMappings = await client.GetRoleMappingsForUserAsync(realm, externalID.Value.ID);

            var attributes = BuildAttributesFromUser(user, ciid, roleMappings);
            return attributes.FirstOrDefault(a => a.Name.Equals(name));
        }

        public Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, TimeThreshold atTime)
        {
            return Task.FromResult<CIAttribute?>(null); // TODO: not implemented
        }

        public async IAsyncEnumerable<CIAttribute> GetAttributes(ICIIDSelection selection, TimeThreshold atTime)
        {
            if (!atTime.IsLatest) yield break; // we don't have historic information

            IEnumerable<Guid> GetCIIDs(ICIIDSelection selection)
            {
                return selection switch
                {
                    AllCIIDsSelection _ => mapper.GetAllCIIDs(),
                    SpecificCIIDsSelection multiple => multiple.CIIDs,
                    _ => throw new NotImplementedException()
                };
            }

            var ciids = GetCIIDs(selection).ToHashSet();


            foreach (var (ciid, externalID) in mapper.GetIDPairs(ciids))
            {
                var user = await client.GetUserAsync(realm, externalID.ID);
                var roleMappings = await client.GetRoleMappingsForUserAsync(realm, externalID.ID);

                foreach (var a in BuildAttributesFromUser(user, ciid, roleMappings))
                    yield return a;
            }
        }

        public async IAsyncEnumerable<CIAttribute> FindAttributesByFullName(string name, ICIIDSelection selection, TimeThreshold atTime)
        {
            if (!atTime.IsLatest) yield break; // we don't have historic information

            var users = await client.GetUsersAsync(realm, true, null, null, null, null, 99999, null, null); // TODO, HACK: magic number, how to properly get all user IDs?
            foreach (var user in users)
            {
                var ciid = mapper.GetCIID(new ExternalIDString(user.Id));
                if (ciid.HasValue && selection.Contains(ciid.Value)) // HACK: we get ALL users and discard a lot of them again
                {
                    foreach (var a in BuildAttributesFromUser(user, ciid.Value, null))
                        if (a.Name.Equals(name)) // HACK: we are getting ALL attributes of the user and then discard them again, except for one
                            yield return a;
                }
            }
        }

        public async IAsyncEnumerable<CIAttribute> FindAttributesByName(string regex, ICIIDSelection selection, TimeThreshold atTime)
        {
            if (!atTime.IsLatest) yield break; // we don't have historic information

            switch (selection)
            {
                case SpecificCIIDsSelection mcs:
                    {
                        foreach (var ciid in mcs.CIIDs)
                        {
                            var externalID = mapper.GetExternalID(ciid);
                            if (!externalID.HasValue)
                                continue;

                            var user = await client.GetUserAsync(realm, externalID.Value.ID);
                            var roleMappings = await client.GetRoleMappingsForUserAsync(realm, externalID.Value.ID);

                            foreach (var a in BuildAttributesFromUser(user, ciid, roleMappings))
                                if (Regex.IsMatch(a.Name, regex))
                                    yield return a;
                        }
                        break;
                    }
                case AllCIIDsSelection _:
                    {
                        var users = await client.GetUsersAsync(realm, true, null, null, null, null, 99999, null, null); // TODO, HACK: magic number, how to properly get all user IDs?
                        foreach (var user in users)
                        {
                            var id = mapper.GetCIID(new ExternalIDString(user.Id));
                            if (id.HasValue)
                            {
                                foreach (var a in BuildAttributesFromUser(user, id.Value, null))
                                    if (Regex.IsMatch(a.Name, regex)) // HACK: we are getting ALL attributes of the user and then discard many of them again
                                        yield return a;
                            }
                        }
                        break;
                    }
            }
        }

        public IAsyncEnumerable<Relation> GetRelations(IRelationSelection rl, TimeThreshold atTime)
        {
            return AsyncEnumerable.Empty<Relation>();// TODO: implement
        }

        public Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, TimeThreshold atTime)
        {
            return Task.FromResult<Relation?>(null);// TODO: implement
        }
    }
}
