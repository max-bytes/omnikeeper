using LandscapeRegistry.Model;
using LandscapeRegistry.Model.Cached;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Entity.Template
{
    public class Template
    {
        public CIType CIType { get; private set; }
        public IImmutableDictionary<string, CIAttributeTemplate> AttributeTemplates { get; private set; }

        public IImmutableDictionary<string, Trait> Traits { get; private set; }

        public static Template Build(CIType ciType, IEnumerable<CIAttributeTemplate> attributes, IEnumerable<Trait> traits)
        {
            return new Template()
            {
                CIType = ciType,
                AttributeTemplates = attributes.ToImmutableDictionary(t => t.Name),
                Traits = traits.ToImmutableDictionary(t => t.Name)
            };
        }
    }
    public class Templates
    {
        private IImmutableDictionary<CIType, Template> templates { get; set; }
        private IImmutableDictionary<string, Trait> traits { get; set; } // TODO: actually check if the traits are fulfilled

        public Template GetTemplate(CIType ciType) => templates.GetValueOrDefault(ciType, null);

        public async static Task<Templates> Build(CIModel ciModel, CachedLayerModel layerModel, NpgsqlTransaction trans)
        {
            var traits = new List<Trait>()
            {
                Trait.Build("ansible_can_deploy_to_it", new Dictionary<string, CIAttributeTemplate>()
                {
                    { "ipAddress", CIAttributeTemplate.BuildFromParams("ipAddress", "this is a description", AttributeValues.AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null)) }
                })
            }.ToImmutableDictionary(t => t.Name);

            // TODO: move the actual data creation somewhere else
            return new Templates()
            {
                traits = traits,
                templates = new List<Template>()
                {
                    Template.Build(await ciModel.GetCIType("Application", trans),
                            new List<CIAttributeTemplate>() {
                                // TODO
                                CIAttributeTemplate.BuildFromParams("name", "This is a description", AttributeValues.AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                            },
                            new List<Trait>()
                            {

                            }
                    ),
                    Template.Build(await ciModel.GetCIType("Naemon Instance", trans),
                            new List<CIAttributeTemplate>() {
                                // TODO
                                CIAttributeTemplate.BuildFromParams("name", "This is a description", AttributeValues.AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                            },
                            new List<Trait>()
                            {
                                traits["ansible_can_deploy_to_it"]
                            }
                    )
                }.ToImmutableDictionary(t => t.CIType)
            };
        }
    }
}
