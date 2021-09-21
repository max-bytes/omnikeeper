
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

        public async Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, string[] layerIDs, bool returnRemoved, IModelContext trans, TimeThreshold atTime, string? nameRegexFilter = null)
        {
            var @base = await model.GetAttributes(selection, layerIDs, returnRemoved, trans, atTime);
            //var generatorSelection = new GeneratorSelectionAll();
            //@base = await MergeInGeneratedAttributes(@base, selection, generatorSelection, layerIDs, trans, atTime);

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

        public async Task<IDictionary<Guid, CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            var @base = await model.FindAttributesByFullName(name, selection, layerID, trans, atTime);
            //var generatorSelection = new GeneratorSelectionContainingFullItemName(name);
            //@base = await MergeInGeneratedAttributes(@base, selection, generatorSelection, layerID, trans, atTime);

            // TODO: implement

            return @base;
        }

        //private async Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> MergeInGeneratedAttributes(IDictionary<Guid, IDictionary<string, CIAttribute>>[] @base, ICIIDSelection ciidSelection, IGeneratorSelection generatorSelection, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        //{
        //    // NOTE: we use the service provider here to avoid a circular dependency in DI
        //    var effectiveGeneratorProvider = sp.GetRequiredService<IEffectiveGeneratorProvider>();

        //    var egsTuples = await effectiveGeneratorProvider.GetEffectiveGeneratorItems(layerID, ciidSelection, generatorSelection, trans, atTime);
        //    if (!egsTuples.IsEmpty())
        //    {
        //        var existingAttributes = @base.Select(a => a.InformationHash).ToHashSet();
        //        var resolver = new GeneratorAttributeResolver();
        //        foreach (var (item, ci) in egsTuples)
        //        {
        //            var generatedAttribute = resolver.Resolve(ci, layerID, item);
        //            if (generatedAttribute != null)
        //            {
        //                var newAttributeHash = CIAttribute.CreateInformationHash(item.Name, ci.ID);
        //                @base.TryGetValue(ci.ID, out var existingCIAttributes);
        //                // TODO: complex insert stuff
        //                if (!existingAttributes.Contains(newAttributeHash))
        //                {
        //                    @base = @base.Concat(generatedAttribute);
        //                    existingAttributes.Add(newAttributeHash);
        //                }
        //            }
        //        }
        //    }
        //    return @base;
        //}



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
