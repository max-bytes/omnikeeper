using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NUnit.Framework;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;

namespace Tests.Integration
{
    public class DBBackedTestBase
    {
        private NpgsqlConnectionWrapper? connWrapper;
        private ModelContextBuilder? modelContextBuilder;

        protected ModelContextBuilder ModelContextBuilder => modelContextBuilder!;

        [SetUp]
        public virtual void Setup()
        {
            DBSetup.Setup();

            var dbcb = new DBConnectionBuilder();
            connWrapper = dbcb.BuildFromUserSecrets(GetType().Assembly, true);
            modelContextBuilder = new ModelContextBuilder(connWrapper);
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (connWrapper != null)
                connWrapper.Dispose();
        }
    }
}
