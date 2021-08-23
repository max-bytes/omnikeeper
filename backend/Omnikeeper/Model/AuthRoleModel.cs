using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
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
    public class AuthRoleModel : TraitDataConfigBaseModel<AuthRole>, IAuthRoleModel
    {
        public AuthRoleModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel)
            : base(CoreTraits.AuthRoleFlattened, effectiveTraitModel, ciModel, baseAttributeModel)
        {
        }

        public async Task<AuthRole> GetAuthRole(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            return await Get(id, layerSet, timeThreshold, trans);
        }

        public async Task<(Guid, AuthRole)> TryToGetAuthRole(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            return await TryToGet(id, layerSet, timeThreshold, trans);
        }

        protected override (AuthRole dc, string id) EffectiveTrait2DC(EffectiveTrait et)
        {
            var AuthRoleID = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");
            var permissions = TraitConfigDataUtils.ExtractOptionalArrayTextAttribute(et, "permissions", new string[] { });

            return (new AuthRole(AuthRoleID, permissions.ToArray()), AuthRoleID);
        }

        public async Task<IDictionary<string, AuthRole>> GetAuthRoles(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await GetAll(layerSet, trans, timeThreshold);
        }

        public async Task<(AuthRole authRole, bool changed)> InsertOrUpdate(string id, IEnumerable<string> permissions, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await InsertOrUpdate(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                ("auth_role.id", new AttributeScalarValueText(id)),
                ("auth_role.permissions", AttributeArrayValueText.BuildFromString(permissions)),
                (ICIModel.NameAttribute, new AttributeScalarValueText($"AuthRole - {id}"))
            );
        }

        public async Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await TryToDelete(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                "auth_role.id",
                "auth_role.permissions",
                ICIModel.NameAttribute
            );
        }
    }
}
