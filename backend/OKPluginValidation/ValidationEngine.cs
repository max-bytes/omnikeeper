using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OKPluginValidation.Validation
{
    public class ValidationEngine : IValidationEngine
    {
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly IValidationIssueModel validationIssueModel;
        private readonly IValidationModel validationModel;
        private readonly IChangesetModel changesetModel;
        private readonly IUserInDatabaseModel userInDatabaseModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly IDictionary<string, IValidationRule> availableValidationRules;

        public ValidationEngine(IModelContextBuilder modelContextBuilder, IValidationIssueModel validationIssueModel, IValidationModel validationModel,
            IChangesetModel changesetModel, IUserInDatabaseModel userInDatabaseModel, IEnumerable<IValidationRule> availableValidationRules, IMetaConfigurationModel metaConfigurationModel)
        {
            this.modelContextBuilder = modelContextBuilder;
            this.validationIssueModel = validationIssueModel;
            this.validationModel = validationModel;
            this.changesetModel = changesetModel;
            this.userInDatabaseModel = userInDatabaseModel;
            this.metaConfigurationModel = metaConfigurationModel;
            this.availableValidationRules = availableValidationRules.ToDictionary(r => r.Name);
        }

        public async Task<bool> Run(ILogger logger)
        {
            var validationWriteLayerID = "__okvalidation"; // TODO
            var validationWriteLayerset = new LayerSet(validationWriteLayerID);

            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(modelContextBuilder.BuildImmediate());

            var timeThreshold = TimeThreshold.BuildLatest();

            var validations = await validationModel.GetValidations(new LayerSet(metaConfiguration.ConfigLayerset), modelContextBuilder.BuildImmediate(), timeThreshold);

            // user handling: get or create
            // TODO: generalize, offer method for upserting a special process user (consolidate with CLB users)
            var username = "__validation.engine";
            var displayName = username;
            // generate a unique but deterministic GUID
            var userGuidNamespace = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef1");
            var guid = GuidUtility.Create(userGuidNamespace, username);
            var user = await userInDatabaseModel.UpsertUser(username, displayName, guid, UserType.Robot, modelContextBuilder.BuildImmediate());

            // perform validation of rules, producing an updated set of issues
            var newIssues = new Dictionary<string, ValidationIssue>();
            foreach (var (validationName, validation) in validations)
            {
                var ruleName = validation.RuleName;

                // find rule
                if (availableValidationRules.TryGetValue(ruleName, out var rule))
                {
                    try
                    {
                        var c = await rule.PerformValidation(validation.RuleConfig, modelContextBuilder.BuildImmediate(), timeThreshold);
                        foreach (var cc in c)
                            newIssues.Add(cc.ID, cc);
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"Running validation with rule {rule.Name} failed: {e.Message}");
                    }
                }
                else
                {
                    logger.LogError($"Error running validation: could not find rule with name {ruleName}");
                }
            }

            // update issues
            try
            {
                using var trans = modelContextBuilder.BuildDeferred();
                var changesetProxy = new ChangesetProxy(user, timeThreshold, changesetModel);
                var oldValidationIssues = await validationIssueModel.GetValidationIssues(validationWriteLayerset, trans, timeThreshold);

                // TODO: implement a bulk update instead of updating each item separately
                foreach (var (_, newIssue) in newIssues)
                    await validationIssueModel.InsertOrUpdate(newIssue.ID, newIssue.Message, newIssue.AffectedCIs, validationWriteLayerset, validationWriteLayerID,
                        new DataOriginV1(DataOriginType.ComputeLayer), changesetProxy, trans);

                var outdatedIssues = oldValidationIssues.Where(kv => !newIssues.ContainsKey(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);

                foreach (var (_, outdatedIssue) in outdatedIssues)
                    await validationIssueModel.TryToDelete(outdatedIssue.ID, validationWriteLayerset, validationWriteLayerID,
                        new DataOriginV1(DataOriginType.ComputeLayer), changesetProxy, trans);

                // TODO: add relations from validation issue to validation

                trans.Commit();
            }
            catch (Exception e)
            {
                logger.LogError($"Updating validation issues failed: {e.Message}");
            }

            return true;
        }
    }
}
