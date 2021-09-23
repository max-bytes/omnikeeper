
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Templating;
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
        private readonly IEffectiveGeneratorProvider effectiveGeneratorProvider;

        public GeneratingBaseAttributeModel(IBaseAttributeModel model, IEffectiveGeneratorProvider effectiveGeneratorProvider)
        {
            this.model = model;
            this.effectiveGeneratorProvider = effectiveGeneratorProvider;
        }

        private ISet<string> CalculateAdditionalRequiredDependentAttributes(string[] layerIDs, IGeneratorSelection generatorSelection, IAttributeSelection baseAttributeSelection)
        {
            var ret = new HashSet<string>();
            for (int i = 0; i < layerIDs.Length; i++)
            {
                var layerID = layerIDs[i];
                var egis = effectiveGeneratorProvider.GetEffectiveGeneratorItems(layerID, generatorSelection, baseAttributeSelection);
                if (!egis.IsEmpty())
                {
                    foreach (var egi in egis)
                        ret.UnionWith(egi.Value.UsedAttributeNames.Where(name => !baseAttributeSelection.Contains(name)));
                }
            }
            return ret;
        }

        public async Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, string[] layerIDs, bool returnRemoved, IModelContext trans, TimeThreshold atTime, IAttributeSelection attributeSelection)
        {

            var @base = await model.GetAttributes(selection, layerIDs, returnRemoved, trans, atTime, attributeSelection);

            var generatorSelection = new GeneratorSelectionAll();

            // we need to potentially extend the attributeSelection so that it contains all attributes necessary to resolve the generated attributes
            // the caller is allowed to not know or care about generated attributes and their requirements, so we need to extend here
            // and also (for the return structure) ignore any additionally fetched attributes that were only fetched to calculate the generated attributes
            var additionalAttributeNames = attributeSelection switch
            {
                NamedAttributesSelection n => CalculateAdditionalRequiredDependentAttributes(layerIDs, generatorSelection, attributeSelection),
                RegexAttributeSelection r => CalculateAdditionalRequiredDependentAttributes(layerIDs, generatorSelection, attributeSelection),
                AllAttributeSelection _ => new HashSet<string>(), // we are fetching all attributes anyway, no need to add additional attributes
                _ => throw new Exception("Invalid attribute selection encountered"),
            };
            var additionalAttributes = (additionalAttributeNames.Count > 0) ? await model.GetAttributes(selection, layerIDs, false, trans, atTime, NamedAttributesSelection.Build(additionalAttributeNames)) : null;

            @base = MergeInGeneratedAttributes(@base, additionalAttributes, generatorSelection, layerIDs, attributeSelection);

            return @base;
        }

        private IDictionary<Guid, IDictionary<string, CIAttribute>>[] MergeInGeneratedAttributes(
            IDictionary<Guid, IDictionary<string, CIAttribute>>[] @base, IDictionary<Guid, IDictionary<string, CIAttribute>>[]? additionalAttributes,
            IGeneratorSelection generatorSelection, string[] layerIDs, IAttributeSelection attributeSelection)
        {
            // TODO: maybe we can find an efficient way to not generate attributes that are guaranteed to be hidden by a higher layer anyway
            var resolver = new GeneratorAttributeResolver();
            for (int i = 0; i < @base.Length; i++)
            {
                var layerID = layerIDs[i];
                var egis = effectiveGeneratorProvider.GetEffectiveGeneratorItems(layerID, generatorSelection, attributeSelection);
                foreach (var egi in egis)
                {
                    foreach (var (ciid, existingCIAttributes) in @base[i])
                    {
                        // TODO, HACK: this approach of adding additionalAttributes is not 100% clean
                        // this only adds the additional attributes if the base CI contains any attributes... or not?
                        // in any case, from this POV, it would make more sense to already pass in all attributes in a single structure instead of 
                        // having to merged them here... but this would require changes to the fetch parameters, making it fetch the additional attributes in one fetch
                        var additionals = additionalAttributes?[i].GetOr(ciid, ImmutableDictionary<string, CIAttribute>.Empty);
                        var generatedAttribute = resolver.Resolve(existingCIAttributes.Values, additionals?.Values, ciid, layerID, egi);
                        if (generatedAttribute != null)
                        {
                            existingCIAttributes[egi.Name] = generatedAttribute;
                        }
                    }
                }
            }

            return @base;
        }


        public async Task<IEnumerable<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, IModelContext trans)
        {
            // NOTE: changesets never contain any generated attributes
            return await model.GetAttributesOfChangeset(changesetID, trans);
        }

        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            // NOTE: generating binary attributes is not supported, hence we just pass through here
            return await model.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
        }


        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            return await model.InsertAttribute(name, value, ciid, layerID, changeset, origin, trans);
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            return await model.RemoveAttribute(name, ciid, layerID, changeset, origin, trans);
        }

        public async Task<IEnumerable<(Guid ciid, string fullName)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            return await model.BulkReplaceAttributes(data, changeset, origin, trans);
        }
    }
}
