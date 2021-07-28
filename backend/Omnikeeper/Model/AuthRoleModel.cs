using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    // TODO: think about caching?
    public class AuthRoleModel : IAuthRoleModel
    {
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;

        public AuthRoleModel(IBaseConfigurationModel baseConfigurationModel, IEffectiveTraitModel effectiveTraitModel)
        {
            this.baseConfigurationModel = baseConfigurationModel;
            this.effectiveTraitModel = effectiveTraitModel;
        }

        public async Task<AuthRole> GetAuthRole(string id, TimeThreshold timeThreshold, IModelContext trans)
        {
            var t = await TryToGetAuthRole(id, timeThreshold, trans);
            if (t.Equals(default))
            {
                throw new Exception($"Could not find AuthRole with ID {id}");
            } else
            {
                return t.Item2;
            }
        }

        public async Task<(Guid,AuthRole)> TryToGetAuthRole(string id, TimeThreshold timeThreshold, IModelContext trans)
        {

            var traitForAuthRoles = CoreTraits.AuthRole;
            // NOTE: we need to flatten the core trait first... is this the best way? Could we maybe also keep core traits as flattened already?
            var flattenedTraitForAuthRoles = RecursiveTraitService.FlattenSingleRecursiveTrait(traitForAuthRoles);

            // derive config layerset from base config
            var baseConfig = await baseConfigurationModel.GetConfigOrDefault(trans);
            var configLayerset = new LayerSet(baseConfig.ConfigLayerset);

            // TODO: better performance possible?
            var AuthRoleCIs = await effectiveTraitModel.CalculateEffectiveTraitsForTrait(flattenedTraitForAuthRoles, configLayerset, new AllCIIDsSelection(), trans, timeThreshold);

            var foundAuthRoleCI = AuthRoleCIs.FirstOrDefault(pci => pci.Value.et.TraitAttributes["id"].Attribute.Value.Value2String() == id);
            if (!foundAuthRoleCI.Equals(default(KeyValuePair<Guid, (MergedCI ci, EffectiveTrait et)>)))
            {
                return (foundAuthRoleCI.Key, EffectiveTrait2AuthRole(foundAuthRoleCI.Value.et));
            }
            return default;
        }

        private AuthRole EffectiveTrait2AuthRole(EffectiveTrait et)
        {
            var idA = et.TraitAttributes["id"];
            var AuthRoleID = idA.Attribute.Value.Value2String();
            var permissions = new string[] { };
            if (et.TraitAttributes.ContainsKey("permissions"))
            {
                var permissionsA = et.TraitAttributes["permissions"];
                if (permissionsA.Attribute.Value is AttributeArrayValueText aavt)
                {
                    permissions = aavt.Values.Select(v => v.Value).ToArray();
                }
            }
            return new AuthRole(AuthRoleID, permissions);
        }

        public async Task<IDictionary<string, AuthRole>> GetAuthRoles(IModelContext trans, TimeThreshold timeThreshold)
        {
            var traitForAuthRoles = CoreTraits.AuthRole;
            // NOTE: we need to flatten the core trait first... is this the best way? Could we maybe also keep core traits as flattened already?
            var flattenedTraitForAuthRoles = RecursiveTraitService.FlattenSingleRecursiveTrait(traitForAuthRoles);

            // derive config layerset from base config
            var baseConfig = await baseConfigurationModel.GetConfigOrDefault(trans);
            var configLayerset = new LayerSet(baseConfig.ConfigLayerset);

            var AuthRoleCIs = await effectiveTraitModel.CalculateEffectiveTraitsForTrait(flattenedTraitForAuthRoles, configLayerset, new AllCIIDsSelection(), trans, timeThreshold);
            var ret = new Dictionary<string, AuthRole>();
            foreach(var (_, AuthRoleET) in AuthRoleCIs.Values)
            {
                var p = EffectiveTrait2AuthRole(AuthRoleET);
                ret.Add(p.ID, p);
            }
            return ret;
        }
    }
}
