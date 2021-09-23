
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

        public async Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, string[] layerIDs, bool returnRemoved, IModelContext trans, TimeThreshold atTime, IAttributeSelection attributeSelection)
        {
            var @base = await model.GetAttributes(selection, layerIDs, returnRemoved, trans, atTime, attributeSelection);
            var generatorSelection = new GeneratorSelectionAll();
            @base = MergeInGeneratedAttributes(@base, generatorSelection, layerIDs, attributeSelection);

            return @base;
        }

        public async Task<IDictionary<Guid, CIAttribute>[]> FindAttributesByFullName(string name, ICIIDSelection selection, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            var @base = await model.FindAttributesByFullName(name, selection, layerIDs, trans, atTime);
            // TODO: find a way to force calculation of specified attribute even if its not in @base
            //var generatorSelection = new GeneratorSelectionContainingFullItemName(name);
            //@base = await MergeInGeneratedAttributes(@base, selection, generatorSelection, layerID, trans, atTime);

            // TODO: implement

            return @base;
        }

        private IDictionary<Guid, IDictionary<string, CIAttribute>>[] MergeInGeneratedAttributes(IDictionary<Guid, IDictionary<string, CIAttribute>>[] @base, 
            IGeneratorSelection generatorSelection, string[] layerIDs, IAttributeSelection attributeSelection)
        {
            // TODO: maybe we can find an efficient way to not generate attributes that are guaranteed to be hidden by a higher layer anyway
            var resolver = new GeneratorAttributeResolver();
            for (int i = 0; i < @base.Length; i++)
            {
                var layerID = layerIDs[i];
                var egis = effectiveGeneratorProvider.GetEffectiveGeneratorItems(layerID, generatorSelection, attributeSelection);
                if (!egis.IsEmpty())
                {
                    foreach (var (ciid, existingCIAttributes) in @base[i])
                    {
                        foreach (var egi in egis)
                        {
                            var generatedAttribute = resolver.Resolve(existingCIAttributes, ciid, layerID, egi);
                            if (generatedAttribute != null)
                            {
                                existingCIAttributes.Add(egi.Name, generatedAttribute);
                            }
                        }
                    }
                }


                //var egsTuples = 
                //if (!egsTuples.IsEmpty())
                //{
                //    var resolver = new GeneratorAttributeResolver();
                //    foreach (var (item, ci) in egsTuples)
                //    {
                //        if ()
                //        {
                //            // TODO: complex insert stuff
                //            if (!existingCIAttributes.ContainsKey(item.Name))
                //            {
                //                var generatedAttribute = resolver.Resolve(ci, layerID, item);
                //                if (generatedAttribute != null)
                //                {
                //                    existingCIAttributes.Add(item.Name, generatedAttribute);
                //                }
                //            }
                //        } else
                //        {
                //            var generatedAttribute = resolver.Resolve(ci, layerID, item);
                //            if (generatedAttribute != null)
                //            {
                //                @base[i][ci.ID] = new Dictionary<string, CIAttribute>() { { item.Name, generatedAttribute } };
                //            }
                //        }
                //    }
                //}
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
