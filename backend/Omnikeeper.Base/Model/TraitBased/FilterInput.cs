using GraphQL.DataLoader;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Base.Utils;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.TraitBased
{
    public class FilterInput
    {
        public readonly AttributeFilter[] AttributeFilters;
        public readonly RelationFilter[] RelationFilters;

        public FilterInput(AttributeFilter[] attributeFilters, RelationFilter[] relationFilters)
        {
            AttributeFilters = attributeFilters;
            RelationFilters = relationFilters;
        }
    }

    public static class FilterInputExtensions
    {
        // NOTE: resulting CIIDs do NOT necessarily fulfill any trait requirements, systems using this method need to perform these checks if needed
        public static IDataLoaderResult<ICIIDSelection> Apply(this FilterInput filter, ICIIDSelection ciSelection, IAttributeModel attributeModel, IRelationModel relationModel, ICIModel ciModel, IEffectiveTraitModel effectiveTraitModel, IDataLoaderService dataLoaderService,
            ITraitsProvider traitsProvider, LayerSet layerset, IModelContext trans, TimeThreshold timeThreshold)
        {
            IDataLoaderResult<ICIIDSelection> matchingCIIDs;
            if (!filter.RelationFilters.IsEmpty() && !filter.AttributeFilters.IsEmpty())
            {
                matchingCIIDs = TraitEntityHelper.GetMatchingCIIDsByAttributeFilters(ciSelection, attributeModel, filter.AttributeFilters, layerset, trans, timeThreshold, dataLoaderService)
                    .Then(matchingCIIDs => TraitEntityHelper.GetMatchingCIIDsByRelationFilters(matchingCIIDs, attributeModel, relationModel, ciModel, effectiveTraitModel, traitsProvider, filter.RelationFilters, layerset, trans, timeThreshold, dataLoaderService))
                    .ResolveNestedResults();
            }
            else if (!filter.AttributeFilters.IsEmpty() && filter.RelationFilters.IsEmpty())
            {
                matchingCIIDs = TraitEntityHelper.GetMatchingCIIDsByAttributeFilters(ciSelection, attributeModel, filter.AttributeFilters, layerset, trans, timeThreshold, dataLoaderService);
            }
            else if (filter.AttributeFilters.IsEmpty() && !filter.RelationFilters.IsEmpty())
            {
                matchingCIIDs = TraitEntityHelper.GetMatchingCIIDsByRelationFilters(ciSelection, attributeModel, relationModel, ciModel, effectiveTraitModel, traitsProvider, filter.RelationFilters, layerset, trans, timeThreshold, dataLoaderService);
            }
            else
            {
                matchingCIIDs = new SimpleDataLoader<ICIIDSelection>(token => Task.FromResult<ICIIDSelection>(AllCIIDsSelection.Instance));
            }
            return matchingCIIDs;
        }
    }
}
