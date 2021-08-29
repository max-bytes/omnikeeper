using Microsoft.Extensions.Logging;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace OKPluginNaemonConfig
{
    public class NamonConfig : CLBBase
    {
        private readonly IRelationModel relationModel;
        private readonly ICIModel ciModel;
        private readonly IEffectiveTraitModel traitModel;
        public NamonConfig(ICIModel ciModel, IAttributeModel atributeModel, ILayerModel layerModel, IEffectiveTraitModel traitModel, IRelationModel relationModel,
                           IChangesetModel changesetModel, IUserInDatabaseModel userModel)
            : base(atributeModel, layerModel, changesetModel, userModel)
        {
            this.ciModel = ciModel;
            this.relationModel = relationModel;
            this.traitModel = traitModel;
        }
        public override async Task<bool> Run(Layer targetLayer, IChangesetProxy changesetProxy, CLBErrorHandler errorHandler, IModelContext trans, ILogger logger)
        {
            logger.LogDebug("Start naemonConfig");

            // function getNaemonInstanceById
            return true;
        }
    }
}
