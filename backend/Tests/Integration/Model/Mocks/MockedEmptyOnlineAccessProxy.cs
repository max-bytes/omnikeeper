using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Moq;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tests.Integration.Model.Mocks
{
    class MockedEmptyOnlineAccessProxy : Mock<IOnlineAccessProxy>
    {
        public MockedEmptyOnlineAccessProxy()
        {
            Setup(_ => _.GetAttributes(It.IsAny<ISet<Guid>>(), It.IsAny<LayerSet>(), It.IsAny<NpgsqlTransaction>())).Returns(AsyncEnumerable.Empty<(CIAttribute attribute, long layerID)>());
            Setup(_ => _.GetAttributesWithName(It.IsAny<string>(), It.IsAny<LayerSet>(), It.IsAny<NpgsqlTransaction>())).Returns(AsyncEnumerable.Empty<(CIAttribute attribute, long layerID)>());
        }

        public static IOnlineAccessProxy O => new MockedEmptyOnlineAccessProxy().Object;
    }

}
