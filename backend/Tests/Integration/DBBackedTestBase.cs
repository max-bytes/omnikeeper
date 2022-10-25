using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NUnit.Framework;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;

namespace Tests.Integration
{
    public class DBBackedTestBase
    {
        private NpgsqlConnection? conn;
        private ModelContextBuilder? modelContextBuilder;

        protected ModelContextBuilder ModelContextBuilder => modelContextBuilder!;

        [SetUp]
        public virtual void Setup()
        {
            DBSetup.Setup();

            var dbcb = new DBConnectionBuilder();
            conn = dbcb.BuildFromUserSecrets(GetType().Assembly, true);
            modelContextBuilder = new ModelContextBuilder(conn);
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (conn != null)
                conn.Close();
        }
    }
}
