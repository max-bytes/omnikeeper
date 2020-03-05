using Landscape.Base;
using Landscape.Base.Model;
using LandscapePrototype.Entity.AttributeValues;
using Npgsql;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TestPlugin
{
    public class ComputeLayerBrainMonitoring : IComputeLayerBrain
    {
        private readonly ICIModel ciModel;
        private readonly ILayerModel layerModel;
        private readonly IChangesetModel changesetModel;
        private readonly NpgsqlConnection conn;

        public ComputeLayerBrainMonitoring(ICIModel ciModel, ILayerModel layerModel, IChangesetModel changesetModel, NpgsqlConnection conn)
        {
            this.ciModel = ciModel;
            this.layerModel = layerModel;
            this.changesetModel = changesetModel;
            this.conn = conn;
        }

        public async Task<bool> Run()
        {
            using var trans = await conn.BeginTransactionAsync();
            var layerSet = await layerModel.BuildLayerSet(new[] { "Monitoring" }, trans);
            var monitoringLayerID = layerSet.LayerIDs.First();
            var monitoringCheckModules = await ciModel.GetCIsWithType(layerSet, trans, DateTimeOffset.Now, "Monitoring Check Module");

            var changeset = changesetModel.CreateChangeset(trans);
            foreach(var mcm in monitoringCheckModules)
            {
                await ciModel.InsertAttribute("monitored CIs", AttributeValueText.Build("foo"), monitoringLayerID, mcm.Identity, changeset.Id, trans);
            }

            trans.Commit();

            return true;
        }
    }
}
