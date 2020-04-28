using Landscape.Base.Model;
using LandscapeRegistry.Entity.AttributeValues;
using Npgsql;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Landscape.Base.Entity
{
    public class Templates
    {
        private IImmutableDictionary<string, Template> templates { get; set; }

        public Template GetTemplate(string ciTypeID) => templates.GetValueOrDefault(ciTypeID, null);

        public async static Task<Templates> Build(ICIModel ciModel, ITraitsProvider traitsProvider, NpgsqlTransaction trans)
        {
            var traits = await traitsProvider.GetTraits(trans);
            // TODO: move the actual data creation somewhere else
            return new Templates()
            {
                templates = new List<Template>()
                {
                    Template.Build("Application",
                            new List<CIAttributeTemplate>() {
                                // TODO
                                CIAttributeTemplate.BuildFromParams("name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                            },
                            new List<RelationTemplate>() {},
                            new List<Trait>() {}
                    ),
                    //Template.Build("Naemon Instance",
                    //        new List<CIAttributeTemplate>() {
                    //            // TODO
                    //            CIAttributeTemplate.BuildFromParams("monitoring.naemon.instance_name", "This is a description", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                    //        },
                    //        new List<RelationTemplate>() {},
                    //        new List<Trait>()
                    //        {
                    //            traits.traits["ansible_can_deploy_to_it"]
                    //        }
                    //),
                    Template.Build("Ansible Host Group",
                            new List<CIAttributeTemplate>() {
                                CIAttributeTemplate.BuildFromParams("automation.ansible_group_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                            },
                            new List<RelationTemplate>() {},
                            new List<Trait>() {}
                    )
                }.ToImmutableDictionary(t => t.CITypeID)
            };
        }
    }
}
