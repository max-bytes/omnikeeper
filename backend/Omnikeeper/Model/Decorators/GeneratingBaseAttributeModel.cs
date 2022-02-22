
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
                    ret.UnionWith(egi.Template.UsedAttributeNames.Where(name => !baseAttributeSelection.Contains(name)));
            }
            return ret;
        }

        public async Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, IAttributeSelection attributeSelection, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            var @base = await model.GetAttributes(selection, attributeSelection, layerIDs, trans, atTime);

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
                AllAttributeSelection _ => new HashSet<string>(), // we are fetching all attributes anyway, no need to add additional attributes
                NoAttributesSelection _ => new HashSet<string>(), // no attributes necessary
                _ => throw new Exception("Invalid attribute selection encountered"),
            };
            var additionalAttributes = (additionalAttributeNames.Count > 0) ? await model.GetAttributes(selection, NamedAttributesSelection.Build(additionalAttributeNames), layerIDs, trans, atTime) : null;

            @base = MergeInGeneratedAttributes(@base, additionalAttributes, egis, layerIDs);

            // TODO: remove additional attributes again, to be consistent; caller did not ask for them

            return @base;
        }

        private IDictionary<Guid, IDictionary<string, CIAttribute>>[] MergeInGeneratedAttributes(IDictionary<Guid, IDictionary<string, CIAttribute>>[] @base,
            IDictionary<Guid, IDictionary<string, CIAttribute>>[]? additionalAttributes, IEnumerable<GeneratorV1>[] egis, string[] layerIDs)
        {
            // TODO: maybe we can find an efficient way to not generate attributes that are guaranteed to be hidden by a higher layer anyway
            var resolver = new GeneratorAttributeResolver();
            for (int i = 0; i < @base.Length; i++)
            {
                var layerID = layerIDs[i];
                foreach (var egi in egis[i])
                {
                    foreach (var (ciid, existingCIAttributes) in @base[i])
                    {
                        // TODO, HACK: this approach of adding additionalAttributes is not 100% clean
                        // this only adds the additional attributes if the base CI contains any attributes... or not?
                        // in any case, from this POV, it would make more sense to already pass in all attributes in a single structure instead of 
                        // having to merged them here... but this would require changes to the fetch parameters, making it fetch the additional attributes in one fetch

                        // TODO: we shouldn't add the generated attributes right away... because that might give the impression that generated templates referencing
                        // other generated attributes is fully supported... which it isnt! Because the recursive dependent attributes are not properly added.
                        var additionals = additionalAttributes?[i].GetOr(ciid, ImmutableDictionary<string, CIAttribute>.Empty);
                        var generatedAttribute = resolver.Resolve(existingCIAttributes.Values, additionals?.Values, ciid, layerID, egi);
                        if (generatedAttribute != null)
                        {
                            // TODO: we are currently overwriting regular attributes with generated attributes... decide if that is the correct approach
                            existingCIAttributes[egi.AttributeName] = generatedAttribute;
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


        public async Task<(IList<(Guid ciid, string fullName, IAttributeValue value, Guid? existingAttributeID, Guid newAttributeID)> inserts,
            IDictionary<string, CIAttribute> outdatedAttributes)> 
            PrepareForBulkUpdate<F>(IBulkCIAttributeData<F> data, IModelContext trans, TimeThreshold readTS)
        {
            return await model.PrepareForBulkUpdate(data, trans, readTS);
        }

        public async Task<(bool changed, Guid changesetID)> BulkUpdate(IList<(Guid ciid, string fullName, IAttributeValue value, Guid? existingAttributeID, Guid newAttributeID)> inserts, IList<(Guid ciid, string name, IAttributeValue value, Guid attributeID, Guid newAttributeID)> removes, string layerID, DataOriginV1 origin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await model.BulkUpdate(inserts, removes, layerID, origin, changesetProxy, trans);
        }
    }
}
