using Landscape.Base.Inbound;
using Landscape.Base.Model;
using Landscape.Base.Service;
using Landscape.Base.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tests.OIA
{
    class ExternalIDManagerTest
    {
        public class TestedExternalIDManager : ExternalIDManager<ExternalIDString>
        {
            private readonly IList<ExternalIDString> ids = new List<ExternalIDString>();

            public TestedExternalIDManager(ScopedExternalIDMapper<ExternalIDString> mapper) : base(mapper, TimeSpan.Zero)
            {
            }

            protected async override Task<IEnumerable<ExternalIDString>> GetExternalIDs()
            {
                return ids;
            }

            public TestedExternalIDManager Add(string externalID)
            {
                ids.Add(new ExternalIDString(externalID));
                return this;
            }

            public TestedExternalIDManager RemoveAt(int index)
            {
                ids.RemoveAt(index);
                return this;
            }
        }

        public class TestedScopedExternalIDMapper : ScopedExternalIDMapper<ExternalIDString>
        {
            public TestedScopedExternalIDMapper(string scope, IExternalIDMapPersister persister) : base(scope, persister, (s) => new ExternalIDString(s))
            {
            }

            public override ICIIdentificationMethod GetIdentificationMethod(ExternalIDString externalID) => CIIdentificationMethodNoop.Build();
        }

        [Test]
        public async Task TestBasics()
        {
            var persisterMock = new Mock<IExternalIDMapPersister>();
            var scopedExternalIDMapper = new TestedScopedExternalIDMapper("testscope", persisterMock.Object);
            var eidManager = new TestedExternalIDManager(scopedExternalIDMapper);
            eidManager.Add("eid0").Add("eid1").Add("eid2");

            var ciModelMock = new Mock<ICIModel>();
            var attributeModelMock = new Mock<IAttributeModel>();
            var existingCIs = new List<Guid>();
            var newCIIDs = new Queue<Guid>();
            newCIIDs.Enqueue(Guid.Parse("006DA01F-9ABD-4D9D-80C7-02AF85C822A8"));
            newCIIDs.Enqueue(Guid.Parse("116DA01F-9ABD-4D9D-80C7-02AF85C822A8"));
            newCIIDs.Enqueue(Guid.Parse("226DA01F-9ABD-4D9D-80C7-02AF85C822A8"));

            ciModelMock.Setup(x => x.CIIDExists(It.IsAny<Guid>(), null)).ReturnsAsync((Guid ciid, NpgsqlTransaction trans) => existingCIs.Contains(ciid));
            ciModelMock.Setup(x => x.CreateCI(It.IsAny<Guid>(), null))
                .Callback((NpgsqlTransaction trans, Guid ciid) => existingCIs.Add(ciid))
                .ReturnsAsync((NpgsqlTransaction trans, Guid ciid) => ciid);
            ciModelMock.Setup(x => x.CreateCI(null))
                .ReturnsAsync((NpgsqlTransaction trans) => { var n = newCIIDs.Dequeue(); existingCIs.Add(n); return n; });
            ciModelMock.Setup(x => x.GetCIIDs(null)).ReturnsAsync(() => existingCIs);

            // initial run, creating 3 mapped cis
            var changed = await eidManager.Update(ciModelMock.Object, attributeModelMock.Object, null, NullLogger.Instance);
            Assert.IsTrue(changed);
            Assert.IsEmpty(newCIIDs); // all ciids from the queue have been used
            Assert.AreEqual(3, existingCIs.Count); // new cis have been created
            CollectionAssert.AreEquivalent(scopedExternalIDMapper.GetIDPairs(existingCIs.ToHashSet()), new List<(Guid, ExternalIDString)>()
            {
                (existingCIs[0], new ExternalIDString("eid0")),
                (existingCIs[1], new ExternalIDString("eid1")),
                (existingCIs[2], new ExternalIDString("eid2")),
            });

            // nothing must have changed if called again
            changed = await eidManager.Update(ciModelMock.Object, attributeModelMock.Object, null, NullLogger.Instance);
            Assert.IsFalse(changed);

            // remove one ci from the external source
            eidManager.RemoveAt(1);
            changed = await eidManager.Update(ciModelMock.Object, attributeModelMock.Object, null, NullLogger.Instance);
            Assert.IsTrue(changed);
            Assert.AreEqual(3, existingCIs.Count); // there should still be all three cis present in the model
            CollectionAssert.AreEquivalent(scopedExternalIDMapper.GetIDPairs(existingCIs.ToHashSet()), new List<(Guid, ExternalIDString)>()
            {
                (existingCIs[0], new ExternalIDString("eid0")),
                (existingCIs[2], new ExternalIDString("eid2")),
            });

            // add a new ci in external source
            newCIIDs.Enqueue(Guid.Parse("336DA01F-9ABD-4D9D-80C7-02AF85C822A8"));
            eidManager.Add("eid3");
            changed = await eidManager.Update(ciModelMock.Object, attributeModelMock.Object, null, NullLogger.Instance);
            Assert.IsTrue(changed);
            Assert.AreEqual(4, existingCIs.Count);
            CollectionAssert.AreEquivalent(scopedExternalIDMapper.GetIDPairs(existingCIs.ToHashSet()), new List<(Guid, ExternalIDString)>()
            {
                (existingCIs[0], new ExternalIDString("eid0")),
                (existingCIs[2], new ExternalIDString("eid2")),
                (existingCIs[3], new ExternalIDString("eid3")),
            });

            // delete cis from model, should be recreated on update, if mapped
            existingCIs.RemoveAt(2); // this ci (22xxx...) is still mapped and must be re-created
            existingCIs.RemoveAt(1); // this ci (11xxx...) is not mapped anymore, should stay removed
            changed = await eidManager.Update(ciModelMock.Object, attributeModelMock.Object, null, NullLogger.Instance);
            Assert.IsTrue(changed);
            Assert.AreEqual(3, existingCIs.Count);
            CollectionAssert.AreEquivalent(scopedExternalIDMapper.GetIDPairs(existingCIs.ToHashSet()), new List<(Guid, ExternalIDString)>()
            {
                (existingCIs[0], new ExternalIDString("eid0")),
                (existingCIs[1], new ExternalIDString("eid3")),
                (existingCIs[2], new ExternalIDString("eid2")), // re-added ci is added at the end of the existingCIs
            });
        }


        public class TestedExternalIDManager2 : ExternalIDManager<ExternalIDGuid>
        {
            private readonly IList<ExternalIDGuid> ids = new List<ExternalIDGuid>();

            public TestedExternalIDManager2(ScopedExternalIDMapper<ExternalIDGuid> mapper) : base(mapper, TimeSpan.Zero)
            {
            }

            protected async override Task<IEnumerable<ExternalIDGuid>> GetExternalIDs()
            {
                return ids;
            }

            public TestedExternalIDManager2 Add(Guid externalID)
            {
                ids.Add(new ExternalIDGuid(externalID));
                return this;
            }

            public TestedExternalIDManager2 RemoveAt(int index)
            {
                ids.RemoveAt(index);
                return this;
            }
        }
        public class TestedScopedExternalIDMapper2 : ScopedExternalIDMapper<ExternalIDGuid>
        {
            public TestedScopedExternalIDMapper2(string scope, IExternalIDMapPersister persister) : base(scope, persister, (s) => new ExternalIDGuid(Guid.Parse(s)))
            {
            }

            public override ICIIdentificationMethod GetIdentificationMethod(ExternalIDGuid externalID) => CIIdentificationMethodByCIID.Build(externalID.ID);
        }

        [Test]
        public async Task TestExternalGuidsAsCIIDs()
        {
            var persisterMock = new Mock<IExternalIDMapPersister>();
            var scopedExternalIDMapper = new TestedScopedExternalIDMapper2("testscope", persisterMock.Object);
            var eidManager = new TestedExternalIDManager2(scopedExternalIDMapper);
            eidManager.Add(Guid.Parse("006DA01F-9ABD-4D9D-80C7-02AF85C822A8")).Add(Guid.Parse("116DA01F-9ABD-4D9D-80C7-02AF85C822A8")).Add(Guid.Parse("226DA01F-9ABD-4D9D-80C7-02AF85C822A8"));

            var ciModelMock = new Mock<ICIModel>();
            var attributeModelMock = new Mock<IAttributeModel>();
            var existingCIs = new List<Guid>();

            ciModelMock.Setup(x => x.CIIDExists(It.IsAny<Guid>(), null)).ReturnsAsync((Guid ciid, NpgsqlTransaction trans) => existingCIs.Contains(ciid));
            ciModelMock.Setup(x => x.CreateCI(It.IsAny<Guid>(), null))
                .Callback((NpgsqlTransaction trans, Guid ciid) => existingCIs.Add(ciid))
                .ReturnsAsync((NpgsqlTransaction trans, Guid ciid) => ciid);
            ciModelMock.Setup(x => x.GetCIIDs(null)).ReturnsAsync(() => existingCIs);

            // initial run, creating 3 mapped cis
            var changed = await eidManager.Update(ciModelMock.Object, attributeModelMock.Object, null, NullLogger.Instance);
            Assert.IsTrue(changed);
            Assert.AreEqual(3, existingCIs.Count); // new cis have been created
            ciModelMock.Verify(x => x.CreateCI(null), Times.Never); // cis are never created with a new ciid, the external ones are used
            CollectionAssert.AreEquivalent(scopedExternalIDMapper.GetIDPairs(existingCIs.ToHashSet()), new List<(Guid, ExternalIDGuid)>()
            {
                (existingCIs[0], new ExternalIDGuid(Guid.Parse("006DA01F-9ABD-4D9D-80C7-02AF85C822A8"))),
                (existingCIs[1], new ExternalIDGuid(Guid.Parse("116DA01F-9ABD-4D9D-80C7-02AF85C822A8"))),
                (existingCIs[2], new ExternalIDGuid(Guid.Parse("226DA01F-9ABD-4D9D-80C7-02AF85C822A8"))),
            });
        }
    }
}
