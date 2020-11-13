using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NUnit.Framework;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.Integration
{
    class DBBackedTestBase
    {
        private NpgsqlConnection? conn;
        private ModelContextBuilder? modelContextBuilder;

        protected ModelContextBuilder ModelContextBuilder => modelContextBuilder!;

        [SetUp]
        public virtual void Setup()
        {
            DBSetup.Setup();

            var dbcb = new DBConnectionBuilder();
            conn = dbcb.Build(DBSetup.dbName, false, true);
            modelContextBuilder = new ModelContextBuilder(null, conn, NullLogger<IModelContext>.Instance);
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (conn != null)
                conn.Close();
        }
    }
}
