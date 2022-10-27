using Autofac;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public record class ReactiveRunData(IReadOnlyDictionary<string, IReadOnlyList<Changeset>?> UnprocessedChangesets, IReadOnlyDictionary<string, Guid> LatestSeenChangesets,
        ChangesetProxy ChangesetProxy, IModelContext Trans, ILifetimeScope Scope, IssueAccumulator IssueAccumulator) : IDisposable
    {
        public void Dispose()
        {
            Trans.Dispose();
            Scope.Dispose();
        }
    }

    public class ReactiveRunService
    {
        private readonly IChangesetModel changesetModel;
        private readonly ICIModel ciModel;

        public ReactiveRunService(IChangesetModel changesetModel, ICIModel ciModel)
        {
            this.changesetModel = changesetModel;
            this.ciModel = ciModel;
        }

        private async Task<ICIIDSelection> ChangedCIIDs(ReactiveRunData rrd)
        {
            IList<ICIIDSelection> selections = new List<ICIIDSelection>();
            foreach (var perLayer in rrd.UnprocessedChangesets)
            {
                if (perLayer.Value == null)
                    selections.Add(AllCIIDsSelection.Instance);
                else
                {
                    foreach (var perChangeset in perLayer.Value)
                    {
                        var changedCIIDs = await changesetModel.GetCIIDsAffectedByChangeset(perChangeset.ID, rrd.Trans);
                        selections.Add(SpecificCIIDsSelection.Build(changedCIIDs));
                    }
                }
            }
            return CIIDSelectionExtensions.UnionAll(selections);
        }

        public IObservable<ICIIDSelection> ChangedCIIDsObs(IObservable<ReactiveRunData> rrd)
        {
            return rrd
                .Select(async runData => await ChangedCIIDs(runData))
                .Concat();
        }

        public IObservable<IReadOnlyList<MergedCI>> ChangedCIs(IObservable<ICIIDSelection> changedCIIDs, IAttributeSelection attributeSelection, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            return changedCIIDs.Select(async changedCIIDs =>
            {
                var changedCIs = await ciModel.GetMergedCIs(changedCIIDs, layerSet, false, attributeSelection, trans, timeThreshold);
                return changedCIs;
            }).Concat();
        }
    }

    public class ReactiveGenericTraitEntityModel<T> where T : TraitEntity, new()
    {
        private readonly GenericTraitEntityModel<T> model;

        public ReactiveGenericTraitEntityModel(GenericTraitEntityModel<T> model)
        {
            this.model = model;
        }

        public IObservable<IDictionary<Guid, T>> GetNewAndChangedByCIID(IObservable<(ICIIDSelection, ReactiveRunData)> changedCIIDs, LayerSet layerSet)
        {
            return changedCIIDs.Select(async tuple =>
            {
                var (ciSelection, runData) = tuple;
                return await model.GetByCIID(ciSelection, layerSet, runData.Trans, runData.ChangesetProxy.TimeThreshold);
            }).Concat();
        }

        public IObservable<IDictionary<Guid, T>> GetAllByCIID(IObservable<(IDictionary<Guid, T>, ICIIDSelection)> newAndChangedTargetHosts)
        {
            return newAndChangedTargetHosts.Scan((IDictionary<Guid, T>)new Dictionary<Guid, T>(), (dict, tuple) =>
            {
                var (newAndChanged, ciSelection) = tuple;
                switch (ciSelection)
                {
                    case AllCIIDsSelection _:
                        return newAndChanged;
                    case NoCIIDsSelection _:
                        return dict;
                    case SpecificCIIDsSelection s:
                        foreach (var ciid in s.CIIDs)
                        {
                            if (newAndChanged.TryGetValue(ciid, out var entity))
                                dict[ciid] = entity;
                            else
                                dict.Remove(ciid);
                        }
                        return dict;
                    case AllCIIDsExceptSelection e:
                        throw new NotImplementedException(); // TODO: think about and implement
                    default:
                        throw new Exception("Unknown CIIDSelection encountered");
                }
            });
        }
    }
}
