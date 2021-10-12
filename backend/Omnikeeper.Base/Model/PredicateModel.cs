//using Omnikeeper.Base.Entity;
//using Omnikeeper.Base.Entity.DataOrigin;
//using Omnikeeper.Base.Model;
//using Omnikeeper.Base.Service;
//using Omnikeeper.Base.Utils;
//using Omnikeeper.Base.Utils.ModelContext;
//using Omnikeeper.Entity.AttributeValues;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace Omnikeeper.Base.Model
//{
//    // TODO: think about caching?
//    public class PredicateModel : IDBasedTraitDataConfigBaseModel<Predicate, string>, IPredicateModel
//    {
//        public static readonly RecursiveTrait Predicate = new RecursiveTrait("__meta.config.predicate", new TraitOriginV1(TraitOriginType.Core),
//            new List<TraitAttribute>() {
//                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("predicate.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null), new CIAttributeValueConstraintTextRegex(IDValidations.PredicateIDRegex))),
//                new TraitAttribute("wording_from", CIAttributeTemplate.BuildFromParams("predicate.wording_from", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
//                new TraitAttribute("wording_to", CIAttributeTemplate.BuildFromParams("predicate.wording_to", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
//            },
//            new List<TraitAttribute>()
//            {
//                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
//            }
//        );
//        public static readonly GenericTrait PredicateFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(Predicate);

//        public PredicateModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel, IBaseRelationModel baseRelationModel)
//            : base(PredicateFlattened, effectiveTraitModel, ciModel, baseAttributeModel, baseRelationModel)
//        { }

//        public async Task<IDictionary<string, Predicate>> GetPredicates(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
//        {
//            return await GetAll(layerSet, trans, timeThreshold);
//        }

//        protected override (Predicate, string) EffectiveTrait2DC(EffectiveTrait et)
//        {
//            var predicateID = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");
//            var wordingFrom = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "wording_from");
//            var wordingTo = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "wording_to");
//            return (new Predicate(null, predicateID, wordingFrom, wordingTo), predicateID);
//        }

//        protected override IAttributeValue ID2AttributeValue(string id) => new AttributeScalarValueText(id);

//        public async Task<(Predicate predicate, bool changed)> InsertOrUpdate(string id, string wordingFrom, string wordingTo, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
//        {
//            return await InsertOrUpdateAttributes(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
//                ("predicate.id", new AttributeScalarValueText(id)),
//                ("predicate.wording_from", new AttributeScalarValueText(wordingFrom)),
//                ("predicate.wording_to", new AttributeScalarValueText(wordingTo)),
//                (ICIModel.NameAttribute, new AttributeScalarValueText($"Predicate - {id}"))
//            );
//        }

//        public async Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
//        {
//            return await TryToDelete(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
//                "predicate.id",
//                "predicate.wording_from",
//                "predicate.wording_to",
//                ICIModel.NameAttribute
//            );
//        }
//    }
//}
