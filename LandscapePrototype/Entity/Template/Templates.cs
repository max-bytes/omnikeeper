using LandscapePrototype.Model;
using LandscapePrototype.Model.Cached;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.Template
{
    public class Templates
    {
        public class CITypeLayerKey
        {
            public CIType CIType { get; private set; }
            public long LayerID { get; private set; }

            public override int GetHashCode() => HashCode.Combine(CIType, LayerID);
            public override bool Equals(object obj)
            {
                if (obj is CITypeLayerKey other)
                {
                    return CIType.Equals(other.CIType) && LayerID.Equals(other.LayerID);
                }
                else return false;
            }
            public static CITypeLayerKey Build(CIType ciType, long layerID)
            {
                return new CITypeLayerKey()
                {
                    CIType = ciType,
                    LayerID = layerID
                };
            }
        }

        private IImmutableDictionary<CITypeLayerKey, CIAttributesTemplate> CIAttributeTemplates { get; set; }

        public CIAttributesTemplate GetAttributesTemplate(CIType ciType, long layerID) => CIAttributeTemplates.GetValueOrDefault(CITypeLayerKey.Build(ciType, layerID));

        public async static Task<Templates> Build(CIModel ciModel, CachedLayerModel layerModel, NpgsqlTransaction trans)
        {
            // TODO: move the actual data creation somewhere else
            return new Templates()
            {
                CIAttributeTemplates = new List<CIAttributesTemplate>()
                {
                    CIAttributesTemplate.Build(await ciModel.GetCIType("Application", trans), await layerModel.GetLayer("CMDB", trans),
                        new List<CIAttributeTemplate>() {
                            // TODO
                            CIAttributeTemplate.Build("name", AttributeValues.AttributeValueType.Text)
                        }),
                    CIAttributesTemplate.Build(await ciModel.GetCIType("Naemon Instance", trans), await layerModel.GetLayer("Monitoring Definitions", trans),
                        new List<CIAttributeTemplate>() {
                            // TODO
                            CIAttributeTemplate.Build("name", AttributeValues.AttributeValueType.Text)
                        })
                }.ToImmutableDictionary(t => CITypeLayerKey.Build(t.CIType, t.Layer.ID))
            };
        }
    }
}
