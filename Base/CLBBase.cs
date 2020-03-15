using Landscape.Base.Model;
using LandscapePrototype.Entity;
using LandscapePrototype.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base
{
    public abstract class CLBBase : IComputeLayerBrain
    {
        protected readonly ICIModel ciModel;
        protected readonly IUserModel userModel;
        protected readonly IChangesetModel changesetModel;
        protected readonly ILayerModel layerModel;
        protected readonly NpgsqlConnection conn;

        public CLBBase(ICIModel ciModel, ILayerModel layerModel, IChangesetModel changesetModel, IUserModel userModel, NpgsqlConnection conn)
        {
            this.ciModel = ciModel;
            this.userModel = userModel;
            this.changesetModel = changesetModel;
            this.conn = conn;
            this.layerModel = layerModel;
        }

        protected CLBSettings Settings { get; private set; }

        public string Name => GetType().FullName;

        protected async Task<bool> RunMiddle(CLBSettings settings)
        {
            try
            {
                Settings = settings;

                using var trans = await conn.BeginTransactionAsync();
                var layerSet = await layerModel.BuildLayerSet(new[] { Settings.LayerName }, trans);
                var layerID = layerSet.LayerIDs.First(); // TODO: better way to get layerID from name -> dedicated function
                var username = Name; // HACK: make username the same as CLB name
                var guid = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef1"); // TODO
                var user = await userModel.CreateOrUpdateFetchUser(username, guid, UserType.Robot, trans);
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);

                var errorHandler = new CLBErrorHandler(trans, Name, layerID, changeset.ID, ciModel);

                var result = await Run(layerID, changeset, errorHandler, trans);

                await errorHandler.RemoveOutdatedErrors();

                return result;
            } catch (Exception e)
            {
                Console.WriteLine(e.Message); // TODO: proper error handling, use error handler(?)
                return false;
            }
        }

        public abstract Task<bool> Run(long layerID, Changeset changeset, CLBErrorHandler errorHandler, NpgsqlTransaction trans);

        public void RunSync(CLBSettings settings)
        {
            Console.WriteLine("Starting");
            var task = Task.Run(async () => await RunMiddle(settings));
            var x = task.Result; // Must stay here, so the tasks actually gets completed before returning from this method
        }
    }
}
