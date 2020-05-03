using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Landscape.Base.Model.IAttributeModel;

namespace LandscapeRegistry.Model
{
    public class CISearchModel : ICISearchModel
    {
        private readonly IAttributeModel attributeModel;
        private readonly ICIModel ciModel;

        public CISearchModel(IAttributeModel attributeModel, ICIModel ciModel)
        {
            this.attributeModel = attributeModel;
            this.ciModel = ciModel;
        }

        public async Task<IEnumerable<CompactCI>> Search(string searchString, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var finalSS = searchString.Trim();
            // TODO: performance improvements, TODO: use ciModel.getCINames() instead?
            var ciNamesFromNameAttributes = await attributeModel.FindMergedAttributesByFullName(CIModel.NameAttribute, new AllCIIDsAttributeSelection(), false, layerSet, trans, atTime);
            var foundCIIDs = ciNamesFromNameAttributes.Where(kv => kv.Value.Attribute.Value.FullTextSearch(finalSS, System.Globalization.CompareOptions.IgnoreCase))
                .ToDictionary(kv => kv.Key, kv => kv.Value.Attribute.Value.Value2String());

            if (Guid.TryParse(finalSS, out var guid))
                foundCIIDs = (await ciModel.GetCIIDs(trans)).Where(ciid => ciid.Equals(guid)).ToDictionary(ciid => ciid, ciid => ciid.ToString());

            var cis = await ciModel.GetCompactCIs(layerSet, trans, atTime, foundCIIDs.Select(t => t.Key)); // TODO: this messes up the previous sorting :(

            return cis.Select(ci => (ci, text: foundCIIDs[ci.ID])).OrderBy(t => t.text).Take(500).Select(t => t.ci);
        }
    }
}
