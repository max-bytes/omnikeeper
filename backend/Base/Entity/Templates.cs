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
        private IImmutableDictionary<CIType, Template> templates { get; set; }

        public Template GetTemplate(CIType ciType) => templates.GetValueOrDefault(ciType, null);

        public async static Task<Templates> Build(ICIModel ciModel, ITraitsProvider traitsProvider, NpgsqlTransaction trans)
        {
            var traits = await traitsProvider.GetTraits(trans);
            // TODO: move the actual data creation somewhere else
            return new Templates()
            {
                templates = new List<Template>()
                {
                    Template.Build(await ciModel.GetCITypeByID("Application", trans, null),
                            new List<CIAttributeTemplate>() {
                                // TODO
                                CIAttributeTemplate.BuildFromParams("name", "This is a description", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                            },
                            new List<RelationTemplate>() {},
                            new List<Trait>() {}
                    ),
                    Template.Build(await ciModel.GetCITypeByID("Naemon Instance", trans, null),
                            new List<CIAttributeTemplate>() {
                                // TODO
                                CIAttributeTemplate.BuildFromParams("name", "This is a description", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                            },
                            new List<RelationTemplate>() {},
                            new List<Trait>()
                            {
                                traits.traits["ansible_can_deploy_to_it"]
                            }
                    ),
                    Template.Build(await ciModel.GetCITypeByID("Ansible Host Group", trans, null),
                            new List<CIAttributeTemplate>() {
                                CIAttributeTemplate.BuildFromParams("automation.ansible_group_name", "This is a description", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                            },
                            new List<RelationTemplate>() {},
                            new List<Trait>() {}
                    )
                }.ToImmutableDictionary(t => t.CIType)
            };
        }
    }
}
