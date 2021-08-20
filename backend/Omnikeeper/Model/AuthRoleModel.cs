using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
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
            // derive config layerset from base config
            var baseConfig = await baseConfigurationModel.GetConfigOrDefault(trans);
            var configLayerset = new LayerSet(baseConfig.ConfigLayerset);

            // TODO: better performance possible?
            var AuthRoleCIs = await effectiveTraitModel.CalculateEffectiveTraitsForTrait(CoreTraits.AuthRoleFlattened, configLayerset, new AllCIIDsSelection(), trans, timeThreshold);

            var foundAuthRoleCIs = AuthRoleCIs
                .Where(pci => pci.Value.et.TraitAttributes["id"].Attribute.Value.Value2String() == id)
                .OrderBy(t => t.Key); // we order by GUID to stay consistent even when multiple CIs would match

            var foundAuthRoleCI = foundAuthRoleCIs.FirstOrDefault();
            if (!foundAuthRoleCI.Equals(default(KeyValuePair<Guid, (MergedCI ci, EffectiveTrait et)>)))
            {
                return (foundAuthRoleCI.Key, EffectiveTrait2AuthRole(foundAuthRoleCI.Value.et));
            }
            return default;
        }

        private AuthRole EffectiveTrait2AuthRole(EffectiveTrait et)
        {
            var AuthRoleID = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");
            var permissions = TraitConfigDataUtils.ExtractOptionalArrayTextAttribute(et, "permissions", new string[] { });
            
            return new AuthRole(AuthRoleID, permissions.ToArray());
        }

        public async Task<IDictionary<string, AuthRole>> GetAuthRoles(IModelContext trans, TimeThreshold timeThreshold)
        {
            // derive config layerset from base config
            var baseConfig = await baseConfigurationModel.GetConfigOrDefault(trans);
            var configLayerset = new LayerSet(baseConfig.ConfigLayerset);

            var AuthRoleCIs = await effectiveTraitModel.CalculateEffectiveTraitsForTrait(CoreTraits.AuthRoleFlattened, configLayerset, new AllCIIDsSelection(), trans, timeThreshold);
            var ret = new Dictionary<string, AuthRole>(); // TODO: think about duplicates in ID
            foreach(var (_, AuthRoleET) in AuthRoleCIs.Values)
            {
                var p = EffectiveTrait2AuthRole(AuthRoleET);
                ret.Add(p.ID, p);
            }
            return ret;
        }
    }
}
