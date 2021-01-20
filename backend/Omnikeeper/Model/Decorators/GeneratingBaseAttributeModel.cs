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

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string regex, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            var @base = await model.FindAttributesByName(regex, selection, layerID, trans, atTime);
            var generatorSelection = new GeneratorSelectionContainingRegexItemName(regex);
            @base = await MergeInGeneratedAttributes(@base, selection, generatorSelection, layerID, trans, atTime);

            return @base;
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            var @base = await model.FindAttributesByFullName(name, selection, layerID, trans, atTime);
            var generatorSelection = new GeneratorSelectionContainingFullItemName(name);
            @base = await MergeInGeneratedAttributes(@base, selection, generatorSelection, layerID, trans, atTime);

            return @base;
        }

        public async Task<IEnumerable<Guid>> FindCIIDsWithAttribute(string name, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            // TODO: implement
            return await model.FindCIIDsWithAttribute(name, selection, layerID, trans, atTime);
        }

        public async Task<IDictionary<Guid, string>> GetCINames(ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            // TODO: implement
            return await model.GetCINames(selection, layerID, trans, atTime);
        }

        public async Task<CIAttribute?> GetAttribute(string name, Guid ciid, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            var @base = await model.GetAttribute(name, ciid, layerID, trans, atTime);
            if (@base != null)
                return @base;
            else
            {
                var generatorSelection = new GeneratorSelectionContainingFullItemName(name);
                var r = await MergeInGeneratedAttributes(ImmutableList<CIAttribute>.Empty, SpecificCIIDsSelection.Build(ciid), generatorSelection, layerID, trans, atTime);
                return r.FirstOrDefault();
            }
        }

        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            // NOTE: generating binary attributes is not supported, hence we just pass through here
            return await model.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            var @base = await model.GetAttributes(selection, layerID, trans, atTime);
            var generatorSelection = new GeneratorSelectionAll();
            @base = await MergeInGeneratedAttributes(@base, selection, generatorSelection, layerID, trans, atTime);

            return @base;
        }

        private async Task<IEnumerable<CIAttribute>> MergeInGeneratedAttributes(IEnumerable<CIAttribute> @base, ICIIDSelection ciidSelection, IGeneratorSelection generatorSelection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            // NOTE: we use the service provider here to avoid a circular dependency in DI
            var effectiveGeneratorProvider = sp.GetRequiredService<IEffectiveGeneratorProvider>();

            var egsTuples = await effectiveGeneratorProvider.GetEffectiveGeneratorItems(layerID, ciidSelection, generatorSelection, trans, atTime);
            if (!egsTuples.IsEmpty())
            {
                var existingAttributes = @base.Select(a => a.InformationHash).ToHashSet();
                var resolver = new GeneratorAttributeResolver();
                foreach (var (item, ci) in egsTuples)
                {
                    var newAttributeHash = CIAttribute.CreateInformationHash(item.Name, ci.ID);
                    if (!existingAttributes.Contains(newAttributeHash))
                    {
                        var generatedAttribute = resolver.Resolve(ci, layerID, item);
                        if (generatedAttribute != null)
                        {
                            @base = @base.Concat(generatedAttribute);
                            existingAttributes.Add(newAttributeHash);
                        }
                    }
                }
            }
            return @base;
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, long layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            return await model.InsertAttribute(name, value, ciid, layerID, changesetProxy, origin, trans);
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertCINameAttribute(string nameValue, Guid ciid, long layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            return await model.InsertCINameAttribute(nameValue, ciid, layerID, changesetProxy, origin, trans);
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, long layerID, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await model.RemoveAttribute(name, ciid, layerID, changesetProxy, trans);
        }

        public async Task<IEnumerable<(Guid ciid, string fullName, IAttributeValue value, AttributeState state)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            return await model.BulkReplaceAttributes(data, changesetProxy, origin, trans);
        }
    }
}
