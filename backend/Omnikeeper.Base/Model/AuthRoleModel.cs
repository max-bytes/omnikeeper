using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    // TODO: think about caching?
    public class AuthRoleModel : IDBasedTraitDataConfigBaseModel<AuthRole, string>, IAuthRoleModel
    {
        public static readonly RecursiveTrait AuthRole = new RecursiveTrait("__meta.config.auth_role", new TraitOriginV1(TraitOriginType.Core),
            new List<TraitAttribute>() {
                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("auth_role.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            },
            new List<TraitAttribute>()
            {
                new TraitAttribute("permissions", CIAttributeTemplate.BuildFromParams("auth_role.permissions", AttributeValueType.Text, true)),
                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            }
        );
        public static readonly GenericTrait AuthRoleFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(AuthRole);

        public AuthRoleModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel, IBaseRelationModel baseRelationModel)
            : base(AuthRoleFlattened, effectiveTraitModel, ciModel, baseAttributeModel, baseRelationModel)
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

        protected override IAttributeValue ID2AttributeValue(string id) => new AttributeScalarValueText(id);

        public async Task<IDictionary<string, AuthRole>> GetAuthRoles(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await GetAll(layerSet, trans, timeThreshold);
        }

        public async Task<(AuthRole authRole, bool changed)> InsertOrUpdate(string id, IEnumerable<string> permissions, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await InsertOrUpdateAttributes(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
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
