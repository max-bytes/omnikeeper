using Keycloak.Net;
using Newtonsoft.Json;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        private bool BuildAttribute(string name, Guid ciid, IAttributeValue value, Guid changesetID, IAttributeSelection attributeSelection, [MaybeNullWhen(false)] out CIAttribute ret)
        {
            if (!attributeSelection.Contains(name))
            {
                ret = null;
                return false;
            }
            // create a deterministic, dependent guid from the ciid + attribute name + value
            var id = GuidUtility.Create(ciid, name + layer.ID.ToString() + value.Value2String()); // NOTE: id must change when the value changes
            ret = new CIAttribute(id, name, ciid, value, AttributeState.New, changesetID);
            return true;
        }

        private IEnumerable<CIAttribute> BuildAttributesFromUser(Keycloak.Net.Models.Users.User user, Guid ciid, Keycloak.Net.Models.Common.Mapping? roleMappings, IAttributeSelection attributeSelection)
        {
            /* with external data sources, we don't have a single source of attributes and hence
                * we don't have a single source of attribute IDs (or relation IDs, or...)
                * we use guids for attribute IDs and all other IDs that can also come from external data sources
            */
            var changesetID = staticChangesetID; // TODO: how to work with changesets when its online access?
            var CIName = (user.FirstName.Length > 0 && user.LastName.Length > 0) ? $"{user.FirstName} {user.LastName}" : user.UserName;

            if (BuildAttribute(ICIModel.NameAttribute, ciid, new AttributeScalarValueText($"User {CIName}"), changesetID, attributeSelection, out var a1)) { yield return a1; }
            if (BuildAttribute("user.email", ciid, new AttributeScalarValueText(user.Email), changesetID, attributeSelection, out var a2)) yield return a2;
            if (BuildAttribute("user.username", ciid, new AttributeScalarValueText(user.UserName), changesetID, attributeSelection, out var a3)) yield return a3;
            if (BuildAttribute("user.first_name", ciid, new AttributeScalarValueText(user.FirstName), changesetID, attributeSelection, out var a4)) yield return a4;
            if (BuildAttribute("user.last_name", ciid, new AttributeScalarValueText(user.LastName), changesetID, attributeSelection, out var a5)) yield return a5;
            if (BuildAttribute("keycloak.id", ciid, new AttributeScalarValueText(user.Id), changesetID, attributeSelection, out var a6)) yield return a6;

            // roles
            if (roleMappings != null && roleMappings.ClientMappings != null)
            {
                if (BuildAttribute("keycloak.client_mappings", ciid, AttributeScalarValueJSON.BuildFromString(JsonConvert.SerializeObject(roleMappings.ClientMappings)), changesetID, attributeSelection, out var a7)) yield return a7;
            }
        }

        public Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, TimeThreshold atTime)
        {
            return Task.FromResult<CIAttribute?>(null); // TODO: not implemented
        }

        public async IAsyncEnumerable<CIAttribute> GetAttributes(ICIIDSelection selection, TimeThreshold atTime, IAttributeSelection attributeSelection)
        {
            if (!atTime.IsLatest) yield break; // we don't have historic information

            var ciids = selection.GetCIIDs(() => mapper.GetAllCIIDs()).ToHashSet();

            foreach (var (ciid, externalID) in mapper.GetIDPairs(ciids))
            {
                var user = await client.GetUserAsync(realm, externalID.ID);
                var roleMappings = await client.GetRoleMappingsForUserAsync(realm, externalID.ID);

                foreach (var a in BuildAttributesFromUser(user, ciid, roleMappings, attributeSelection))
                    yield return a;
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
