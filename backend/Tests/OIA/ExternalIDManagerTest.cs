using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.OIA
{
    class ExternalIDManagerTest
    {
        public class TestedExternalIDManager : ExternalIDManager<ExternalIDString>
        {
            private readonly IList<(ExternalIDString, ICIIdentificationMethod)> ids = new List<(ExternalIDString, ICIIdentificationMethod)>();

            public TestedExternalIDManager(ScopedExternalIDMapper<ExternalIDString> mapper) : base(mapper, TimeSpan.Zero)
            {
            }

            protected override Task<IEnumerable<(ExternalIDString, ICIIdentificationMethod)>> GetExternalIDs()
            {
                return Task.FromResult(ids.AsEnumerable());
            }

            public TestedExternalIDManager Add(string externalID)
            {
                ids.Add((new ExternalIDString(externalID), CIIdentificationMethodNoop.Build()));
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
            public TestedScopedExternalIDMapper(IScopedExternalIDMapPersister persister) : base(persister, (s) => new ExternalIDString(s))
            {
            }
        }

        [Test]
        public async Task TestBasics()
        {
            var persisterMock = new Mock<IScopedExternalIDMapPersister>();
            persisterMock.Setup(x => x.Load(It.IsAny<IModelContext>())).ReturnsAsync(() => new Dictionary<Guid, string>());
            persisterMock.Setup(x => x.Persist(It.IsAny<IDictionary<Guid, string>>(), It.IsAny<IModelContext>())).ReturnsAsync(() => true);
            var scopedExternalIDMapper = new TestedScopedExternalIDMapper(persisterMock.Object);

            var eidManager = new TestedExternalIDManager(scopedExternalIDMapper);
            eidManager.Add("eid0").Add("eid1").Add("eid2");

            var ciModelMock = new Mock<ICIModel>();
            var attributeModelMock = new Mock<IAttributeModel>();
            var existingCIs = new List<Guid>();
            var newCIIDs = new Queue<Guid>();
            newCIIDs.Enqueue(Guid.Parse("006DA01F-9ABD-4D9D-80C7-02AF85C822A8"));
            newCIIDs.Enqueue(Guid.Parse("116DA01F-9ABD-4D9D-80C7-02AF85C822A8"));
            newCIIDs.Enqueue(Guid.Parse("226DA01F-9ABD-4D9D-80C7-02AF85C822A8"));

            var trans = new Mock<IModelContext>().Object;

            ciModelMock.Setup(x => x.CIIDExists(It.IsAny<Guid>(), It.IsAny<IModelContext>())).ReturnsAsync((Guid ciid, IModelContext trans) => existingCIs.Contains(ciid));
            ciModelMock.Setup(x => x.CreateCI(It.IsAny<Guid>(), It.IsAny<IModelContext>()))
                .Callback((Guid ciid, IModelContext trans) => existingCIs.Add(ciid))
                .ReturnsAsync((Guid ciid, IModelContext trans) => ciid);
            ciModelMock.Setup(x => x.CreateCI(It.IsAny<IModelContext>()))
                .ReturnsAsync((IModelContext trans) => { var n = newCIIDs.Dequeue(); existingCIs.Add(n); return n; });
            ciModelMock.Setup(x => x.GetCIIDs(It.IsAny<IModelContext>())).ReturnsAsync(() => existingCIs);

            await scopedExternalIDMapper.Setup(trans);

            // initial run, creating 3 mapped cis
            var (changed, successful) = await eidManager.Update(ciModelMock.Object, attributeModelMock.Object, new CIMappingService(), trans, NullLogger.Instance);
            Assert.IsTrue(successful);
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
            (changed, successful) = await eidManager.Update(ciModelMock.Object, attributeModelMock.Object, new CIMappingService(), trans, NullLogger.Instance);
            Assert.IsTrue(successful);
            Assert.IsFalse(changed);

            // remove one ci from the external source
            eidManager.RemoveAt(1);
            (changed, successful) = await eidManager.Update(ciModelMock.Object, attributeModelMock.Object, new CIMappingService(), trans, NullLogger.Instance);
            Assert.IsTrue(successful);
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
            (changed, successful) = await eidManager.Update(ciModelMock.Object, attributeModelMock.Object, new CIMappingService(), trans, NullLogger.Instance);
            Assert.IsTrue(successful);
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
            (changed, successful) = await eidManager.Update(ciModelMock.Object, attributeModelMock.Object, new CIMappingService(), trans, NullLogger.Instance);
            Assert.IsTrue(successful);
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
            private readonly IList<(ExternalIDGuid, ICIIdentificationMethod)> ids = new List<(ExternalIDGuid, ICIIdentificationMethod)>();

            public TestedExternalIDManager2(ScopedExternalIDMapper<ExternalIDGuid> mapper) : base(mapper, TimeSpan.Zero)
            {
            }

            protected override Task<IEnumerable<(ExternalIDGuid, ICIIdentificationMethod)>> GetExternalIDs()
            {
                return Task.FromResult(ids.AsEnumerable());
            }

            public TestedExternalIDManager2 Add(Guid externalID)
            {
                ids.Add((new ExternalIDGuid(externalID), CIIdentificationMethodByCIID.Build(externalID)));
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
            public TestedScopedExternalIDMapper2(IScopedExternalIDMapPersister persister) : base(persister, (s) => new ExternalIDGuid(Guid.Parse(s)))
            {
            }
        }

        [Test]
        public async Task TestExternalGuidsAsCIIDs()
        {
            var persisterMock = new Mock<IScopedExternalIDMapPersister>();
            persisterMock.Setup(x => x.Load(It.IsAny<IModelContext>())).ReturnsAsync(() => new Dictionary<Guid, string>());
            persisterMock.Setup(x => x.Persist(It.IsAny<IDictionary<Guid, string>>(), It.IsAny<IModelContext>())).ReturnsAsync(() => true);
            var scopedExternalIDMapper = new TestedScopedExternalIDMapper2(persisterMock.Object);
            var eidManager = new TestedExternalIDManager2(scopedExternalIDMapper);
            eidManager.Add(Guid.Parse("006DA01F-9ABD-4D9D-80C7-02AF85C822A8")).Add(Guid.Parse("116DA01F-9ABD-4D9D-80C7-02AF85C822A8")).Add(Guid.Parse("226DA01F-9ABD-4D9D-80C7-02AF85C822A8"));

            var ciModelMock = new Mock<ICIModel>();
            var attributeModelMock = new Mock<IAttributeModel>();
            var existingCIs = new List<Guid>();
            var trans = new Mock<IModelContext>().Object;

            ciModelMock.Setup(x => x.CIIDExists(It.IsAny<Guid>(), It.IsAny<IModelContext>())).ReturnsAsync((Guid ciid, IModelContext trans) => existingCIs.Contains(ciid));
            ciModelMock.Setup(x => x.CreateCI(It.IsAny<Guid>(), It.IsAny<IModelContext>()))
                .Callback((Guid ciid, IModelContext trans) => existingCIs.Add(ciid))
                .ReturnsAsync((Guid ciid, IModelContext trans) => ciid);
            ciModelMock.Setup(x => x.GetCIIDs(It.IsAny<IModelContext>())).ReturnsAsync(() => existingCIs);

            await scopedExternalIDMapper.Setup(trans);

            // initial run, creating 3 mapped cis
            var (changed, successful) = await eidManager.Update(ciModelMock.Object, attributeModelMock.Object, new CIMappingService(), trans, NullLogger.Instance);
            Assert.IsTrue(successful);
            Assert.IsTrue(changed);
            Assert.AreEqual(3, existingCIs.Count); // new cis have been created
            ciModelMock.Verify(x => x.CreateCI(It.IsAny<IModelContext>()), Times.Never); // cis are never created with a new ciid, the external ones are used
            CollectionAssert.AreEquivalent(scopedExternalIDMapper.GetIDPairs(existingCIs.ToHashSet()), new List<(Guid, ExternalIDGuid)>()
            {
                (existingCIs[0], new ExternalIDGuid(Guid.Parse("006DA01F-9ABD-4D9D-80C7-02AF85C822A8"))),
                (existingCIs[1], new ExternalIDGuid(Guid.Parse("116DA01F-9ABD-4D9D-80C7-02AF85C822A8"))),
                (existingCIs[2], new ExternalIDGuid(Guid.Parse("226DA01F-9ABD-4D9D-80C7-02AF85C822A8"))),
            });
        }
    }
}
