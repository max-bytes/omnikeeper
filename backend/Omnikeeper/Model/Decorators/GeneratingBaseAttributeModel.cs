
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class GeneratingBaseAttributeModel : IBaseAttributeModel
    {
        private readonly IBaseAttributeModel model;
        private readonly IServiceProvider sp;

        public GeneratingBaseAttributeModel(IBaseAttributeModel model, IServiceProvider sp)
        {
            this.model = model;
            this.sp = sp;
        }

        private ISet<string> CalculateAdditionalRequiredDependentAttributes(IEnumerable<GeneratorV1>[] egis, IAttributeSelection baseAttributeSelection)
        {
            var ret = new HashSet<string>();
            for (int i = 0; i < egis.Length; i++)
            {
                foreach (var egi in egis[i])
                    ret.UnionWith(egi.Template.UsedAttributeNames.Where(name => !baseAttributeSelection.ContainsAttributeName(name)));
            }
            return ret;
        }

        public async Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, IAttributeSelection attributeSelection, string[] layerIDs, IModelContext trans, TimeThreshold atTime, IGeneratedDataHandling generatedDataHandling)
        {
            var @base = await model.GetAttributes(selection, attributeSelection, layerIDs, trans, atTime, generatedDataHandling);

            switch (generatedDataHandling)
            {
                case GeneratedDataHandlingExclude:
                    break;
                case GeneratedDataHandlingInclude:
                    var generatorSelection = new GeneratorSelectionAll();

                    // calculate effective generators
                    var effectiveGeneratorProvider = sp.GetRequiredService<IEffectiveGeneratorProvider>(); // use serviceProvider to avoid circular dependency
                    var egis = await effectiveGeneratorProvider.GetEffectiveGenerators(layerIDs, generatorSelection, attributeSelection, trans, atTime);

                    // bail early if there are no egis
                    if (egis.All(egi => egi.IsEmpty()))
                        return @base;

                    // we need to potentially extend the attributeSelection so that it contains all attributes necessary to resolve the generated attributes
                    // the caller is allowed to not know or care about generated attributes and their requirements, so we need to extend here
                    // and also (for the return structure) ignore any additionally fetched attributes that were only fetched to calculate the generated attributes
                    var additionalAttributeNames = attributeSelection switch
                    {
                        NamedAttributesSelection n => CalculateAdditionalRequiredDependentAttributes(egis, attributeSelection),
                        RegexAttributeSelection r => CalculateAdditionalRequiredDependentAttributes(egis, attributeSelection),
                        NamedAttributesWithValueFiltersSelection r => CalculateAdditionalRequiredDependentAttributes(egis, attributeSelection),
                        AllAttributeSelection _ => new HashSet<string>(), // we are fetching all attributes anyway, no need to add additional attributes
                        NoAttributesSelection _ => new HashSet<string>(), // no attributes necessary
                        _ => throw new Exception("Invalid attribute selection encountered"),
                    };
                    var additionalAttributes = (additionalAttributeNames.Count > 0) ?
                        await model.GetAttributes(selection, NamedAttributesSelection.Build(additionalAttributeNames), layerIDs, trans, atTime, generatedDataHandling) :
                        Enumerable.Repeat(ImmutableDictionary<Guid, IDictionary<string, CIAttribute>>.Empty, layerIDs.Length).ToArray();

                    @base = MergeInGeneratedAttributes(@base, additionalAttributes, egis, layerIDs, attributeSelection);

                    // TODO: remove additional attributes again, to be consistent; caller did not ask for them

                    break;
                default:
                    throw new Exception("Unknown generated-data-handling detected");
            }

            return @base;
        }

        private IDictionary<Guid, IDictionary<string, CIAttribute>>[] MergeInGeneratedAttributes(IDictionary<Guid, IDictionary<string, CIAttribute>>[] @base,
            IDictionary<Guid, IDictionary<string, CIAttribute>>[] additionalAttributes, IEnumerable<GeneratorV1>[] egis, string[] layerIDs, IAttributeSelection attributeSelection)
        {
            // TODO: maybe we can find an efficient way to not generate attributes that are guaranteed to be hidden by a higher layer anyway
            var resolver = new GeneratorAttributeResolver();
            for (int i = 0; i < @base.Length; i++)
            {
                var layerID = layerIDs[i];
                foreach (var egi in egis[i])
                {
                    var existingCIs = @base[i];
                    var additionalCIs = additionalAttributes[i];
                    foreach (var ciid in existingCIs.Keys.Union(additionalCIs.Keys))
                    {
                        // TODO: we shouldn't add the generated attributes right away... because that might give the impression that generated templates referencing
                        // other generated attributes is fully supported... which it isnt! Because the recursive dependent attributes are not properly added.
                        var additionals = additionalCIs.GetOr(ciid, ImmutableDictionary<string, CIAttribute>.Empty);
                        var existing = existingCIs.GetOrWithClass(ciid, null);
                        var generatedAttribute = resolver.Resolve(existing != null ? existing.Values : ImmutableList<CIAttribute>.Empty, additionals.Values, ciid, layerID, egi);
                        if (generatedAttribute != null)
                        {
                            if (attributeSelection.ContainsAttribute(generatedAttribute)) // apply attribute selection to generated attribute
                            {
                                if (existing != null)
                                {
                                    // TODO: we are currently overwriting regular attributes with generated attributes... decide if that is the correct approach
                                    existing[egi.AttributeName] = generatedAttribute;
                                }
                                else
                                {
                                    // NOTE: CI is empty (=does not contain any attributes) in the base data, add it and add the generated attribute in there
                                    existingCIs[ciid] = new Dictionary<string, CIAttribute>() { { egi.AttributeName, generatedAttribute } };
                                }
                            }
                        }
                    }
                }
            }

            return @base;
        }


        public async Task<IEnumerable<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            // NOTE: changesets never contain any generated attributes
            return await model.GetAttributesOfChangeset(changesetID, getRemoved, trans);
        }

        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            // NOTE: generating binary attributes is not supported, hence we just pass through here
            return await model.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<ISet<Guid>> GetCIIDsWithAttributes(ICIIDSelection selection, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            // NOTE: because generators can only produce attributes on CIs that already have any, we can assume that the results of the base call is valid here too
            return await model.GetCIIDsWithAttributes(selection, layerIDs, trans, atTime);
        }

        public async Task<(bool changed, Guid changesetID)> BulkUpdate(IList<(Guid ciid, string fullName, IAttributeValue value, Guid? existingAttributeID, Guid newAttributeID)> inserts, IList<(Guid ciid, string name, IAttributeValue value, Guid attributeID, Guid newAttributeID)> removes, string layerID, DataOriginV1 origin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await model.BulkUpdate(inserts, removes, layerID, origin, changesetProxy, trans);
        }
    }
}
