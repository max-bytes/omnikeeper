using Microsoft.Extensions.Logging;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace OKPluginNaemonConfig
{
    public class NaemonConfig : CLBBase
    {
        private readonly IRelationModel relationModel;
        private readonly ICIModel ciModel;
        private readonly IEffectiveTraitModel traitModel;
        public NaemonConfig(ICIModel ciModel, IAttributeModel atributeModel, ILayerModel layerModel, IEffectiveTraitModel traitModel, IRelationModel relationModel,
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

            //attributeModel
            var timeThreshold = TimeThreshold.BuildLatest();
            var layerset = await layerModel.BuildLayerSet(new[] { "testlayer01" }, trans);
            var attrName = "monman-instance.id";

            // no need for this we can add a trait as Max suggested
            var attributesDict = await attributeModel.FindMergedAttributesByFullName(attrName, new AllCIIDsSelection(), layerset, trans, timeThreshold);
            // Since attributesDict returns all attributes which have name monman-instance.id we need to group  
            // based on id value, this means that for earch item we need to create a configuration
            foreach (var attribute in attributesDict)
            {

            }

            //traitModel.CalculateEffectiveTraitsForTrait

            return true;
        }
    }
}
