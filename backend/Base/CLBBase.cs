﻿using Landscape.Base.Entity;
using Landscape.Base.Model;
using LandscapeRegistry.Entity;
using Microsoft.Extensions.Logging;
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
        protected readonly IAttributeModel attributeModel;
        protected readonly IUserInDatabaseModel userModel;
        protected readonly IChangesetModel changesetModel;
        protected readonly ILayerModel layerModel;
        protected readonly NpgsqlConnection conn;

        public CLBBase(IAttributeModel attributeModel, ILayerModel layerModel, IChangesetModel changesetModel, IUserInDatabaseModel userModel, NpgsqlConnection conn)
        {
            this.attributeModel = attributeModel;
            this.userModel = userModel;
            this.changesetModel = changesetModel;
            this.conn = conn;
            this.layerModel = layerModel;
        }

        protected CLBSettings Settings { get; private set; }

        public string Name => GetType().FullName;

        //public void RunSync(CLBSettings settings, ILogger logger)
        //{
        //    Console.WriteLine("Starting");
        //    var task = Task.Run(async () => await Run(settings, logger));
        //    var x = task.Result; // Must stay here, so the tasks actually gets completed before returning from this method
        //}

        public async Task<bool> Run(CLBSettings settings, ILogger logger)
        {
            try
            {
                Settings = settings;

                using var trans = await conn.BeginTransactionAsync();
                var layerSet = await layerModel.BuildLayerSet(new[] { Settings.LayerName }, trans);
                var layer = await layerModel.GetLayer(Settings.LayerName, trans);
                var username = Name; // HACK: make username the same as CLB name
                var guid = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef1"); // TODO
                var user = await userModel.UpsertUser(username, guid, UserType.Robot, trans);
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);

                var errorHandler = new CLBErrorHandler(trans, Name, layer.ID, changeset.ID, attributeModel);

                var result = await Run(layer, changeset, errorHandler, trans, logger);

                if (result)
                {
                    await errorHandler.RemoveOutdatedErrors();

                    trans.Commit();
                }

                return result;
            } catch (Exception e)
            {
                Console.WriteLine(e.Message); // TODO: proper error handling, use error handler(?)
                return false;
            }
        }

        public abstract Task<bool> Run(Layer targetLayer, Changeset changeset, CLBErrorHandler errorHandler, NpgsqlTransaction trans, ILogger logger);

    }
}
