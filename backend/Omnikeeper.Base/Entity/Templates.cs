//using Omnikeeper.Entity.AttributeValues;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Threading.Tasks;

//namespace Omnikeeper.Base.Entity
//{
//    public class Templates
//    {
//        private IImmutableDictionary<string, Template> TemplateDict { get; set; }

//        public Templates(IImmutableDictionary<string, Template> templateDict)
//        {
//            TemplateDict = templateDict;
//        }

//        public Template? GetTemplate(string ciTypeID)
//        {
//            if (TemplateDict.TryGetValue(ciTypeID, out var f))
//                return f;
//            return null;
//        }

//        public static Task<Templates> Build()
//        {
//            //var traits = await traitsProvider.GetTraits(trans);
//            // TODO: move the actual data creation somewhere else
//            return Task.FromResult(new Templates(new List<Template>()
//                {
//                    //Template.Build("Application",
//                    //        new List<CIAttributeTemplate>() {
//                    //            // TODO
//                    //            CIAttributeTemplate.BuildFromParams("name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
//                    //        },
//                    //        new List<RelationTemplate>() {},
//                    //        new List<Trait>() {}
//                    //),
//                    //Template.Build("Naemon Instance",
//                    //        new List<CIAttributeTemplate>() {
//                    //            // TODO
//                    //            CIAttributeTemplate.BuildFromParams("monitoring.naemon.instance_name", "This is a description", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
//                    //        },
//                    //        new List<RelationTemplate>() {},
//                    //        new List<Trait>()
//                    //        {
//                    //            traits.traits["ansible_can_deploy_to_it"]
//                    //        }
//                    //),
//                    new Template("Ansible Host Group",
//                            new List<CIAttributeTemplate>() {
//                                CIAttributeTemplate.BuildFromParams("automation.ansible_group_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
//                            },
//                            new List<RelationTemplate>() {},
//                            new List<RecursiveTrait>() {}
//                    )
//                }.ToImmutableDictionary(t => t.CITypeID)
//            ));
//        }
//    }
//}
