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
    public class AuthRoleModel : TraitDataConfigBaseModel<AuthRole>, IAuthRoleModel
    {
        public AuthRoleModel(IBaseConfigurationModel baseConfigurationModel, IEffectiveTraitModel effectiveTraitModel)
            : base(CoreTraits.AuthRoleFlattened, baseConfigurationModel, effectiveTraitModel)
        {
        }

        public async Task<AuthRole> GetAuthRole(string id, TimeThreshold timeThreshold, IModelContext trans)
        {
            return await Get(id, timeThreshold, trans);
        }

        public async Task<(Guid,AuthRole)> TryToGetAuthRole(string id, TimeThreshold timeThreshold, IModelContext trans)
        {
            return await TryToGet(id, timeThreshold, trans);
        }

        protected override (AuthRole dc, string id) EffectiveTrait2DC(EffectiveTrait et)
        {
            var AuthRoleID = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");
            var permissions = TraitConfigDataUtils.ExtractOptionalArrayTextAttribute(et, "permissions", new string[] { });
            
            return (new AuthRole(AuthRoleID, permissions.ToArray()), AuthRoleID);
        }

        public async Task<IDictionary<string, AuthRole>> GetAuthRoles(IModelContext trans, TimeThreshold timeThreshold)
        {
            return await GetAll(trans, timeThreshold);
        }
    }
}
