using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Omnikeeper.Base.Service.CIMappingService;

namespace Tests.Integration.Service
{
    public class CIMappingServiceTest
    {
        [Test]
        public async Task Test()
        {
            var service = new CIMappingService();

            var baseTempCIID = Guid.NewGuid();
            var baseFinalCIID = Guid.NewGuid();

            var targetInterfaceFinalCIID = Guid.NewGuid();
            var secondInterfaceFinalCIID = Increment(targetInterfaceFinalCIID);
            var thirdInterfaceFinalCIID = Increment(secondInterfaceFinalCIID);

            var targetInterfaceCmdbInterfaceHwaddress = new AttributeScalarValueText("01:02:03:04:05:06");

            var searchLayers = new LayerSet("l1");

            var idMethod = CIIdentificationMethodByIntersect.Build(
                new ICIIdentificationMethod[]
                {
                    CIIdentificationMethodByUnion.Build(
                        new ICIIdentificationMethod[]
                        {
                            CIIdentificationMethodByData.BuildFromFragments(new CICandidateAttributeData.Fragment[]
                            {
                                new CICandidateAttributeData.Fragment("cmdb.interface.hwaddress", targetInterfaceCmdbInterfaceHwaddress)
                            }, searchLayers),
                            CIIdentificationMethodByData.BuildFromFragments(new CICandidateAttributeData.Fragment[]
                            {
                                new CICandidateAttributeData.Fragment("inv_scan.network_adapter.mac_address", targetInterfaceCmdbInterfaceHwaddress),
                            }, searchLayers),

                        }
                    ),
                    CIIdentificationMethodByRelatedTempCIID.Build(baseTempCIID, false, "has_interface", searchLayers)
                }
                );

            var ciMappingContext = new Mock<ICIMappingContext>();
            ciMappingContext.Setup(x => x.TryGetMappedTemp2FinalCIID(It.Is<Guid>(t => t == baseTempCIID), out baseFinalCIID)).Returns(true);

            var trans = new Mock<IModelContext>();

            // return in random order, not sorted
            ciMappingContext.Setup(x => x.GetMergedCIIDsByRelation(baseFinalCIID, true, "has_interface", searchLayers, trans.Object)).ReturnsAsync(new Guid[] {
                thirdInterfaceFinalCIID,
                targetInterfaceFinalCIID,
                secondInterfaceFinalCIID,
            });

            // return in random order, not sorted
            ciMappingContext.Setup(x => x.GetMergedCIIDsByAttributeNameAndValue("cmdb.interface.hwaddress", targetInterfaceCmdbInterfaceHwaddress, searchLayers, trans.Object)).ReturnsAsync(new Guid[]
            {
                secondInterfaceFinalCIID,
                thirdInterfaceFinalCIID,
                targetInterfaceFinalCIID,
            });

            // return in random order, not sorted
            // does not contain the target interface CIID
            ciMappingContext.Setup(x => x.GetMergedCIIDsByAttributeNameAndValue("inv_scan.network_adapter.mac_address", targetInterfaceCmdbInterfaceHwaddress, searchLayers, trans.Object)).ReturnsAsync(new Guid[]
            {
                secondInterfaceFinalCIID,
                targetInterfaceFinalCIID,
                thirdInterfaceFinalCIID,
            });

            var r = await service.TryToMatch(idMethod, ciMappingContext.Object, trans.Object, NullLogger.Instance);

            r.Should().BeEquivalentTo(new Guid[] { targetInterfaceFinalCIID, secondInterfaceFinalCIID, thirdInterfaceFinalCIID });
        }

        private static Guid Increment(Guid guid)
        {

            byte[] bytes = guid.ToByteArray();

            byte[] order = { 15, 14, 13, 12, 11, 10, 9, 8, 6, 7, 4, 5, 0, 1, 2, 3 };

            for (int i = 0; i < 16; i++)
            {
                if (bytes[order[i]] == byte.MaxValue)
                {
                    bytes[order[i]] = 0;
                }
                else
                {
                    bytes[order[i]]++;
                    return new Guid(bytes);
                }
            }

            throw new OverflowException("Congratulations you are one in a billion billion billion billion etc...");

        }
    }
}
