using Autofac;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
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
using System.Threading.Tasks.Dataflow;

namespace Omnikeeper.Base.DataFlow
{
    public class DataFlowTriggeringBaseAttributeModel : IBaseAttributeModel
    {
        private readonly IBaseAttributeModel model;
        private readonly ILifetimeScope parentLifetimeScope;
        private readonly IChangesetModel changesetModel;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;
        private readonly ILogger<DataFlowTriggeringBaseAttributeModel> logger;

        public DataFlowTriggeringBaseAttributeModel(IBaseAttributeModel model, ILifetimeScope parentLifetimeScope, IChangesetModel changesetModel, ScopedLifetimeAccessor scopedLifetimeAccessor, ILogger<DataFlowTriggeringBaseAttributeModel> logger)
        {
            this.model = model;
            this.parentLifetimeScope = parentLifetimeScope;
            this.changesetModel = changesetModel;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
            this.logger = logger;
        }

        //public class JobRunner
        //{
        //    private readonly IBaseAttributeModel model;
        //    private readonly ILogger<DataFlowTriggeringBaseAttributeModel> logger;
        //    private readonly IBaseAttributeModel baseAttributeModel;
        //    private readonly IModelContextBuilder modelContextBuilder;
        //    private readonly ILifetimeScope lifetimeScope;
        //    private readonly IChangesetModel changesetModel;
        //    private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;

        //    public JobRunner(IBaseAttributeModel model, ILogger<DataFlowTriggeringBaseAttributeModel> logger, IBaseAttributeModel baseAttributeModel, IModelContextBuilder modelContextBuilder,
        //        ILifetimeScope parentLifetimeScope, IChangesetModel changesetModel, ScopedLifetimeAccessor scopedLifetimeAccessor)
        //    {
        //        this.model = model;
        //        this.logger = logger;
        //        this.baseAttributeModel = baseAttributeModel;
        //        this.modelContextBuilder = modelContextBuilder;
        //        this.lifetimeScope = parentLifetimeScope;
        //        this.changesetModel = changesetModel;
        //        this.scopedLifetimeAccessor = scopedLifetimeAccessor;
        //    }
        //    public void Run(PerformContext? context, AttributeChange[] attributeChanges)
        //    {
        //        using (HangfireConsoleLogger.InContext(context))
        //        {
        //            RunAsync(attributeChanges).GetAwaiter().GetResult();
        //        }
        //    }

        //    public async Task RunAsync(AttributeChange[] attributeChanges)
        //    {
        //        await using (var scope = lifetimeScope.BeginLifetimeScope(builder =>
        //        {
        //            builder.Register(builder => new CLBContext(clb)).InstancePerLifetimeScope();
        //            builder.RegisterType<CurrentAuthorizedCLBUserService>().As<ICurrentUserService>().InstancePerLifetimeScope();
        //        }))
        //        {
        //            scopedLifetimeAccessor.SetLifetimeScope(scope);

        //            try
        //            {
        //                using var transUpsertUser = modelContextBuilder.BuildDeferred();
        //                var currentUserService = scope.Resolve<ICurrentUserService>();
        //                var user = await currentUserService.GetCurrentUser(transUpsertUser);
        //                transUpsertUser.Commit();

        //                var changesetProxy = new ChangesetProxy(user.InDatabase, TimeThreshold.BuildLatest(), changesetModel);

        //                var flow = new TestDataFlow();
        //                var block = flow.Define(logger, baseAttributeModel, changesetProxy, modelContextBuilder);
        //                block.Post(attributeChanges);
        //                block.Complete();
        //            }
        //            finally
        //            {
        //                scopedLifetimeAccessor.ResetLifetimeScope();
        //            }
        //        }
        //    }
        //}

        private async Task RunDataFlows(AttributeChange[] attributeChanges, IModelContext trans)
        {
            await using (var scope = parentLifetimeScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterType<CurrentAuthorizedDataFlowUserService>().As<ICurrentUserService>().InstancePerLifetimeScope();
            }))
            {
                scopedLifetimeAccessor.SetLifetimeScope(scope);

                try
                {
                    //trans.DBTransaction!.Save("dataflow");
                    var currentUserService = scope.Resolve<ICurrentUserService>();
                    var user = await currentUserService.GetCurrentUser(trans);

                    // resolve manually to avoid circular DI dependency?
                    //var baseAttributeModel = scope.Resolve<IBaseAttributeModel>();
                    // HACK, TODO: use inner model for now, but we need to find a better way to use the baseattributemodel inside of flows that prevents loops
                    // idea: maybe check if we are inside a data flow scope already, bail if we are?
                    // another point, check if running flows is necessary earlier, do not even get here when no data flows need to be run
                    var baseAttributeModel = model; 

                    var changesetProxy = new ChangesetProxy(user.InDatabase, TimeThreshold.BuildLatest(), changesetModel);

                    var flow = new TestDataFlow();
                    var block = flow.Define(logger, baseAttributeModel, changesetProxy, trans);
                    block.Post(attributeChanges);
                    block.Complete();

                    //trans.DBTransaction!.Release("dataflow");
                }
                catch
                {
                    trans.DBTransaction!.Rollback("dataflow");
                }
                finally
                {
                    scopedLifetimeAccessor.ResetLifetimeScope();
                }
            }
        }

        public class CurrentAuthorizedDataFlowUserService : ICurrentUserService
        {
            public CurrentAuthorizedDataFlowUserService(IUserInDatabaseModel userModel, ILayerModel layerModel, IMetaConfigurationModel metaConfigurationModel)
            {
                this.userModel = userModel;
                LayerModel = layerModel;
                MetaConfigurationModel = metaConfigurationModel;
            }

            public ILayerModel LayerModel { get; }
            public IMetaConfigurationModel MetaConfigurationModel { get; }
            private readonly IUserInDatabaseModel userModel;

            private AuthenticatedUser? cached = null;

            public async Task<AuthenticatedUser> GetCurrentUser(IModelContext trans)
            {
                if (cached == null)
                {
                    cached = await _GetCurrentUser(trans);
                }
                return cached;
            }

            private async Task<AuthenticatedUser> _GetCurrentUser(IModelContext trans)
            {
                // CLBs implicitly have all permissions
                var suar = await PermissionUtils.GetSuperUserAuthRole(LayerModel, trans);

                // upsert user
                var username = $"__dataflow"; // TODO: better username?
                var displayName = username;
                // generate a unique but deterministic GUID
                var clbUserGuidNamespace = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef1");
                var guid = GuidUtility.Create(clbUserGuidNamespace, username);
                var user = await userModel.UpsertUser(username, displayName, guid, UserType.Robot, trans);

                return new AuthenticatedUser(user, new AuthRole[] { suar });
            }
        }

        //private async Task Trigger(CIAttribute newAttribute, Guid ciid, string layerID, bool isRemoved, IModelContext trans)
        //{
        //    //var jobId = BackgroundJob.Enqueue<JobRunner>(x => x.Run(null, new AttributeChange[] { new AttributeChange(newAttribute, ciid, layerID, isRemoved) }));

        //    await RunDataFlows(new AttributeChange[] { new AttributeChange(newAttribute, ciid, layerID, isRemoved) }, trans);
        //}

        public async Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, IAttributeSelection attributeSelection, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            var @base = await model.GetAttributes(selection, attributeSelection, layerIDs, trans, atTime);

            // add in attributes
            //for (int i = 0; i < @base.Length; i++)
            //{
            //    var generated = GetGeneratedForLayerID(selection, attributeSelection, layerIDs[i], atTime);

            //    if (generated != null)
            //    {
            //        foreach(var (ciid, newCIAttributes) in generated)
            //        {
            //            if (@base[i].TryGetValue(ciid, out var existingCIAttributes))
            //            {
            //                // TODO: we are currently overwriting regular attributes with generated attributes... decide if that is the correct approach
            //                foreach (var newCIAttribute in newCIAttributes)
            //                    existingCIAttributes[newCIAttribute.Key] = newCIAttribute.Value;
            //            } else
            //            {
            //                @base[i][ciid] = newCIAttributes;
            //            }
            //        }
            //    }
            //}

            return @base;
        }

        //private IDictionary<Guid, IDictionary<string, CIAttribute>>? GetGeneratedForLayerID(ICIIDSelection selection, IAttributeSelection attributeSelection, string layerID, TimeThreshold atTime)
        //{
        //    if (atTime.IsLatest)
        //    {
        //        if (attributeKeeper.TryGet(layerID, out var foundAttributes))
        //        {
        //            return foundAttributes.Where(kv => selection.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => (IDictionary<string, CIAttribute>)kv.Value.Where(kv => attributeSelection.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));
        //        }
        //        return null;
        //    } else
        //    {
        //        return null;
        //    }
        //}

        public async Task<IEnumerable<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            return await model.GetAttributesOfChangeset(changesetID, getRemoved, trans);
        }

        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await model.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<ISet<Guid>> GetCIIDsWithAttributes(ICIIDSelection selection, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            return await model.GetCIIDsWithAttributes(selection, layerIDs, trans, atTime);
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            var t = await model.InsertAttribute(name, value, ciid, layerID, changeset, origin, trans);

            if (t.changed)
            {
                await RunDataFlows(new AttributeChange[] { new AttributeChange(t.attribute, ciid, layerID, false) }, trans);
            }

            return t;
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            var t = await model.RemoveAttribute(name, ciid, layerID, changeset, origin, trans);

            if (t.changed)
            {
                await RunDataFlows(new AttributeChange[] { new AttributeChange(t.attribute, ciid, layerID, true) }, trans);
            }

            return t;
        }

        public async Task<IEnumerable<(Guid ciid, string fullName)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            // TODO: trigger

            return await model.BulkReplaceAttributes(data, changeset, origin, trans);
        }
    }
}
