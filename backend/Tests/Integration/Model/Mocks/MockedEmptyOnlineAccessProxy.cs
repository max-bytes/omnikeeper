//using Landscape.Base.Entity;
//using Landscape.Base.Inbound;
//using Landscape.Base.Model;
//using Landscape.Base.Utils;
//using Moq;
//using Npgsql;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using static Landscape.Base.Model.IRelationModel;

//namespace Tests.Integration.Model.Mocks
//{
//    class MockedEmptyOnlineAccessProxy : Mock<IOnlineAccessProxy>
//    {
//        public MockedEmptyOnlineAccessProxy()
//        {
//            Setup(_ => _.GetAttributes(It.IsAny<ICIIDSelection>(), It.IsAny<LayerSet>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>())).Returns(AsyncEnumerable.Empty<(CIAttribute attribute, long layerID)>());
//            //Setup(_ => _.GetAttributesWithName(It.IsAny<string>(), It.IsAny<LayerSet>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>())).Returns(AsyncEnumerable.Empty<(CIAttribute attribute, long layerID)>());
//            //Setup(_ => _.GetRelations(It.IsAny<IRelationSelection?>(), It.IsAny<LayerSet>(), It.IsAny<NpgsqlTransaction>(), It.IsAny<TimeThreshold>())).Returns(AsyncEnumerable.Empty<(Relation relation, long layerID)>());
//        }

//        public static IOnlineAccessProxy O => new MockedEmptyOnlineAccessProxy().Object;
//    }

//}
