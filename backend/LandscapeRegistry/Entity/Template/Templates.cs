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
    public class Templates
    {
        private IImmutableDictionary<CIType, CIAttributesTemplate> CIAttributeTemplates { get; set; }

        public CIAttributesTemplate GetAttributesTemplate(CIType ciType) => CIAttributeTemplates.GetValueOrDefault(ciType, null);

        public async static Task<Templates> Build(CIModel ciModel, CachedLayerModel layerModel, NpgsqlTransaction trans)
        {
            // TODO: move the actual data creation somewhere else
            return new Templates()
            {
                CIAttributeTemplates = new List<CIAttributesTemplate>()
                {
                    CIAttributesTemplate.Build(await ciModel.GetCIType("Application", trans),
                        new List<CIAttributeTemplate>() {
                            // TODO
                            CIAttributeTemplate.BuildFromParams("name", "This is a description", AttributeValues.AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        }),
                    CIAttributesTemplate.Build(await ciModel.GetCIType("Naemon Instance", trans),
                        new List<CIAttributeTemplate>() {
                            // TODO
                            CIAttributeTemplate.BuildFromParams("name", "This is a description", AttributeValues.AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        })
                }.ToImmutableDictionary(t => t.CIType)
            };
        }
    }
}
