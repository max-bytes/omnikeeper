using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests.Integration.Service
{
    public class ArchiveOutdatedChangesetDataServiceTest : DIServicedTestBase
    {
        [Test]
        public async Task TestArchiveUnusedCIs()
        {
            Guid ciid1, ciid2;
            Layer layer1, layer2;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);
                (layer2, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l2", trans);
                ciid1 = await GetService<ICIModel>().CreateCI(trans);
                ciid2 = await GetService<ICIModel>().CreateCI(trans);
                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset1 = await CreateChangesetProxy();

                await GetService<IAttributeModel>().InsertAttribute("foo", new AttributeScalarValueText("bar1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

                await GetService<ChangesetDataModel>().InsertOrUpdateWithAdditionalAttributes(changeset1, layer1.ID, new List<(string, IAttributeValue value)>()
                {
                    ("cd1", new AttributeScalarValueText("cd1_value"))
                }, new DataOriginV1(DataOriginType.Manual), trans);

                trans.Commit();
            }
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset1 = await CreateChangesetProxy();

                await GetService<IAttributeModel>().InsertAttribute("foo", new AttributeScalarValueText("bar2"), ciid2, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

                await GetService<ChangesetDataModel>().InsertOrUpdateWithAdditionalAttributes(changeset1, layer1.ID, new List<(string, IAttributeValue value)>()
                {
                    ("cd2", new AttributeScalarValueText("cd2_value"))
                }, new DataOriginV1(DataOriginType.Manual), trans);

                trans.Commit();
            }
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset1 = await CreateChangesetProxy();

                await GetService<IAttributeModel>().InsertAttribute("foo", new AttributeScalarValueText("bar3"), ciid1, layer2.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);

                await GetService<ChangesetDataModel>().InsertOrUpdateWithAdditionalAttributes(changeset1, layer2.ID, new List<(string, IAttributeValue value)>()
                {
                    ("cd3", new AttributeScalarValueText("cd3_value"))
                }, new DataOriginV1(DataOriginType.Manual), trans);

                trans.Commit();
            }

            //  nothing should be archived
            using var trans1 = ModelContextBuilder.BuildImmediate();
            int numDeleted1 = await GetService<IArchiveOutdatedChangesetDataService>().Archive(NullLogger.Instance, trans1);
            Assert.AreEqual(0, numDeleted1);

            // delete changes associated with changeset 1
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await GetService<IBaseAttributeRevisionistModel>().DeleteAllAttributes(SpecificCIIDsSelection.Build(ciid1), layer1.ID, trans);
                trans.Commit();
            }

            // changeset-data of changeset 1 should be archived
            int numDeleted2 = await GetService<IArchiveOutdatedChangesetDataService>().Archive(NullLogger.Instance, trans1);
            Assert.AreEqual(1, numDeleted2);

            // 2 changeset-data CIs should still exist
            var changesetDataCIs1 = await GetService<ChangesetDataModel>().GetByCIID(AllCIIDsSelection.Instance, new LayerSet(layer1.ID, layer2.ID), trans1, TimeThreshold.BuildLatest());
            Assert.AreEqual(2, changesetDataCIs1.Count);
        }
    }
}
