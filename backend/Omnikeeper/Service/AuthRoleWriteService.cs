using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public class AuthRoleWriteService : IAuthRoleWriteService
    {
        private readonly ICIModel ciModel;
        private readonly IBaseAttributeModel baseAttributeModel;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;
        private readonly IAuthRoleModel authRoleModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;

        public AuthRoleWriteService(IAuthRoleModel authRoleModel, IBaseConfigurationModel baseConfigurationModel, ICIModel ciModel, 
            IBaseAttributeModel baseAttributeModel, ILayerBasedAuthorizationService layerBasedAuthorizationService)
        {
            this.authRoleModel = authRoleModel;
            this.baseConfigurationModel = baseConfigurationModel;
            this.ciModel = ciModel;
            this.baseAttributeModel = baseAttributeModel;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
        }

        public async Task<(AuthRole authRole, bool changed)> InsertOrUpdate(string id, IEnumerable<string> permissions, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans)
        {
            var t = await authRoleModel.TryToGetAuthRole(id, changesetProxy.TimeThreshold, trans);

            var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(trans);
            var writeLayerID = baseConfiguration.ConfigWriteLayer;

            if (!layerBasedAuthorizationService.CanUserWriteToLayer(user, writeLayerID))
                throw new Exception($"User \"{user.Username}\" does not have permission to write to layer {writeLayerID}");

            var changed = false;
            Guid ciid;
            if (t.Equals(default))
            {
                ciid = await ciModel.CreateCI(trans);
                await baseAttributeModel.InsertAttribute("auth_role.id", new AttributeScalarValueText(id), ciid, writeLayerID, changesetProxy, dataOrigin, trans);
                changed = true;
            }
            else
            {
                ciid = t.Item1;
            }


            (_, var tmpChanged) = await baseAttributeModel.InsertAttribute("auth_role.permissions", AttributeArrayValueText.BuildFromString(permissions), ciid, writeLayerID, changesetProxy, dataOrigin, trans);
            changed = changed || tmpChanged;
            var name = $"AuthRole - {id}";
            (_, tmpChanged) = await baseAttributeModel.InsertCINameAttribute(name, ciid, writeLayerID, changesetProxy, dataOrigin, trans);
            changed = changed || tmpChanged;

            var authRole = await authRoleModel.GetAuthRole(id, changesetProxy.TimeThreshold, trans);

            return (authRole, changed);
        }

        public async Task<bool> TryToDelete(string id, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans)
        {
            var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(trans);
            var writeLayerID = baseConfiguration.ConfigWriteLayer;

            if (!layerBasedAuthorizationService.CanUserWriteToLayer(user, writeLayerID))
                throw new Exception($"User \"{user.Username}\" does not have permission to write to layer {writeLayerID}");

            var t = await authRoleModel.TryToGetAuthRole(id, changesetProxy.TimeThreshold, trans);
            if (t.Equals(default))
            {
                return false; // no authRole with this ID exists
            }

            await baseAttributeModel.RemoveAttribute("auth_role.id", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("auth_role.permissions", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("__name", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);

            var tAfterDeletion = await authRoleModel.TryToGetAuthRole(id, changesetProxy.TimeThreshold, trans);
            return tAfterDeletion.Equals(default); // return successfull if authRole does not exist anymore afterwards
        }
    }
}
