using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static OKPluginOIASharepoint.Config;

namespace OKPluginOIASharepoint.Tests
{
    public class Tests
    {
        private static readonly Guid ExternalSharepointTenantID = new Guid("98061435-3c72-44d1-b37a-057e21f42801");
        private static readonly Guid ExternalSharepointClientID = new Guid("3d6e9642-5430-438c-b435-34d35b323b3a");
        private static readonly Guid ExternalSharepointListID = new Guid("37800a8f-0107-445b-b70b-c783ba5a5ce3");
        private static readonly string ExternalSharepointClientSecret = "/w5tskeWck6EV2sx6Mue1tkL+dSw42VdMHlPS5plohw=";
        private static readonly string ExternalSharepointBaseURL = "mhxconsulting.sharepoint.com";
        private static readonly string ExternalSharepointSite = "play2";

        [Test]
        public async Task TestLayerAccessProxy()
        {
            var config = new Config(ExternalSharepointTenantID, ExternalSharepointBaseURL, ExternalSharepointSite, ExternalSharepointClientID,
                ExternalSharepointClientSecret, true, new TimeSpan(100), "sharepoint_test",
                new Config.ListConfig[] { new Config.ListConfig(ExternalSharepointListID,
                    new Config.ListColumnConfig[] {
                        new Config.ListColumnConfig("Title", "title_attribute"),
                        new Config.ListColumnConfig("Surname", "last_name"),
                        new Config.ListColumnConfig("Surname", ICIModel.NameAttribute)
                    }, new string[] { "last_name" }, new string[] { "0" })
                });
            var oia = new OnlineInboundAdapter.Builder().Build(config, new Mock<IConfiguration>().Object, new Mock<IScopedExternalIDMapper>().Object, NullLoggerFactory.Instance);

            // TODO: mock instead?
            var layer = Layer.Build("testlayer", "0", Color.White, AnchorState.Active, ComputeLayerBrainLink.Build(""), OnlineInboundAdapterLink.Build(""));

            var lap = (oia.CreateLayerAccessProxy(layer) as LayerAccessProxy)!;

            // test GetAttributes()
            var fakeCIID1 = Guid.NewGuid();
            var fakeCIID2 = Guid.NewGuid();
            var aa = await lap.GetAttributes(new List<(Guid, SharepointExternalListItemID)>()
                {
                    (fakeCIID1, new SharepointExternalListItemID(ExternalSharepointListID, new Guid("5c52717c-bcee-4c30-9070-45d85a37dce8"))),
                    (fakeCIID2, new SharepointExternalListItemID(ExternalSharepointListID, new Guid("d0c4062d-281e-4c90-9f09-f32c1f94934d")))
                }).ToListAsync();

            aa.Should().BeEquivalentTo(new List<CIAttribute>()
            {
                lap.BuildAttributeFromValue(ICIModel.NameAttribute, "Steiner", fakeCIID1),
                lap.BuildAttributeFromValue("title_attribute", "3", fakeCIID1),
                lap.BuildAttributeFromValue("last_name", "Steiner", fakeCIID1),
                lap.BuildAttributeFromValue(ICIModel.NameAttribute, "Tibbot", fakeCIID2),
                lap.BuildAttributeFromValue("title_attribute", "5", fakeCIID2),
                lap.BuildAttributeFromValue("last_name", "Tibbot", fakeCIID2),
            }, options => options.WithoutStrictOrdering());
        }

        [Test]
        public async Task TestSharepointClient()
        {
            var config = new Config(ExternalSharepointTenantID, ExternalSharepointBaseURL, ExternalSharepointSite, ExternalSharepointClientID,
                ExternalSharepointClientSecret, true, new TimeSpan(100), "sharepoint_test",
                new Config.ListConfig[] { });
            var accessTokenGetter = new AccessTokenGetter(config);

            var client = new SharepointClient(config.siteDomain, config.site, accessTokenGetter);

            var items = await client.GetListItems(ExternalSharepointListID, new string[] { "Title" }).ToListAsync();

            items.Should().BeEquivalentTo(listItems.Select(t => (t.itemGuid, Dyn2Dict(t.data))), options => options.WithStrictOrdering());

            //foreach (var (itemGuid, data) in items)
            //{
            //    Console.WriteLine($"(new Guid(\"{itemGuid}\"), new {{ Title = \"{((dynamic)data).Title}\" }}),");
            //}

            var invalidListGuid = new Guid("11111111-1111-1111-1111-111111111111");
            Assert.ThrowsAsync<WebException>(async () =>
            {
                await client.GetListItems(invalidListGuid, new string[] { "Title" }).ToListAsync(); // NOTE: ToList() is necessary to force yielding function to evaluate
            });

            // test single item
            var singleItem = await client.GetListItem(ExternalSharepointListID, new Guid("fa715c1c-8c22-4f7d-a4be-76c252fc3212"), new string[] { "Title" });
            Assert.AreEqual(Dyn2Dict(listItems[1].data), singleItem);
        }

        private class ExposedExternalIDManager : ExternalIDManager
        {
            public ExposedExternalIDManager(IDictionary<Guid, Config.CachedListConfig> cachedListConfigs, SharepointClient client, ScopedExternalIDMapper mapper, TimeSpan preferredUpdateRate, ILogger logger) : base(cachedListConfigs, client, mapper, preferredUpdateRate, logger)
            {
            }

            public async Task<IEnumerable<(SharepointExternalListItemID externalID, ICIIdentificationMethod idMethod)>> ExposeGetExternalIDs()
            {
                return await GetExternalIDs();
            }
        }


        [Test]
        public async Task TestSuccessfulExternalIDManager()
        {
            var config = new Config(ExternalSharepointTenantID, ExternalSharepointBaseURL, ExternalSharepointSite, ExternalSharepointClientID,
                ExternalSharepointClientSecret, true, new TimeSpan(100), "sharepoint_test",
                new Config.ListConfig[] { new Config.ListConfig(ExternalSharepointListID,
                    new Config.ListColumnConfig[] {
                        new Config.ListColumnConfig("Title", "title_attribute"),
                        new Config.ListColumnConfig("Surname", "last_name"),
                        new Config.ListColumnConfig("Surname", ICIModel.NameAttribute)
                    }, new string[] { "last_name" }, new string[] { "0" })});
            var accessTokenGetter = new AccessTokenGetter(config);

            var client = new SharepointClient(config.siteDomain, config.site, accessTokenGetter);

            var cachedListConfigs = config.listConfigs.ToDictionary(lc => lc.listID, lc => new CachedListConfig(lc)); ;
            var m = new ExposedExternalIDManager(cachedListConfigs, client, new ScopedExternalIDMapper(new Mock<IScopedExternalIDMapPersister>().Object), new TimeSpan(0), NullLogger.Instance);

            var eIDs = await m.ExposeGetExternalIDs();

            var expected = listItems.Select(li => (new SharepointExternalListItemID(ExternalSharepointListID, li.itemGuid),
                    CIIdentificationMethodByData.BuildFromFragments(new CICandidateAttributeData.Fragment[] { new CICandidateAttributeData.Fragment("last_name", new AttributeScalarValueText((li.data.Title as string)!)) }, new LayerSet("0"))
                )
            );

            eIDs.Should().BeEquivalentTo(expected);
        }


        [Test]
        public void TestFailedExternalIDManager()
        {
            var config = new Config(ExternalSharepointTenantID, "nonexisting", ExternalSharepointSite, ExternalSharepointClientID,
                ExternalSharepointClientSecret, true, new TimeSpan(100), "sharepoint_test",
                new Config.ListConfig[] { new Config.ListConfig(ExternalSharepointListID,
                    new Config.ListColumnConfig[] {
                        new Config.ListColumnConfig("Title", "title_attribute"),
                        new Config.ListColumnConfig("Surname", "last_name"),
                        new Config.ListColumnConfig("Surname", ICIModel.NameAttribute)
                    }, new string[] { "last_name" }, new string[] { "0" })});
            var accessTokenGetter = new AccessTokenGetter(config);

            var client = new SharepointClient(config.siteDomain, config.site, accessTokenGetter);

            var cachedListConfigs = config.listConfigs.ToDictionary(lc => lc.listID, lc => new CachedListConfig(lc)); ;
            var m = new ExposedExternalIDManager(cachedListConfigs, client, new ScopedExternalIDMapper(new Mock<IScopedExternalIDMapPersister>().Object), new TimeSpan(0), NullLogger.Instance);

            Assert.ThrowsAsync<WebException>(async () =>
            {
                await m.ExposeGetExternalIDs();
            });
        }

        [Test]
        public void TestExternalIDSerialization()
        {
            var listID = Guid.NewGuid();
            var itemID = Guid.NewGuid();
            var id = new SharepointExternalListItemID(listID, itemID);
            var s = id.SerializeToString();
            var deserialized = SharepointExternalListItemID.Deserialize(s);
            Assert.AreEqual(id, deserialized);
        }

        public Dictionary<string, object> Dyn2Dict(dynamic dynObj)
        {
            var dictionary = new Dictionary<string, object>();
            foreach (PropertyDescriptor? propertyDescriptor in TypeDescriptor.GetProperties(dynObj))
            {
                if (propertyDescriptor != null)
                {
                    object obj = propertyDescriptor.GetValue(dynObj);
                    dictionary.Add(propertyDescriptor.Name, obj);
                }
            }
            return dictionary;
        }

        private (Guid itemGuid, dynamic data)[] listItems = new (Guid, dynamic)[] {
            (new Guid("89f63694-a50a-442b-b999-102bbc755b54"), new { Title = "1" }),
            (new Guid("fa715c1c-8c22-4f7d-a4be-76c252fc3212"), new { Title = "2" }),
            (new Guid("5c52717c-bcee-4c30-9070-45d85a37dce8"), new { Title = "3" }),
            (new Guid("df20091b-166b-45f0-8a80-56a0d2c5e673"), new { Title = "4" }),
            (new Guid("d0c4062d-281e-4c90-9f09-f32c1f94934d"), new { Title = "5" }),
            //(new Guid("2eb4a9cd-f344-4e7d-b252-ff607716e019"), new { Title = "6" }),
            //(new Guid("b82fe2f6-21df-4eac-a527-91952182efd5"), new { Title = "7" }),
            //(new Guid("b71851e0-8077-44a5-8a8d-1b008ae6274a"), new { Title = "8" }),
            //(new Guid("b80bb67b-c752-4bc9-a372-e992f9a9ee27"), new { Title = "9" }),
            //(new Guid("385bfd87-c7f3-4605-8d5b-9df7961ffaf2"), new { Title = "10" }),
            //(new Guid("adb5918a-de26-4f30-b045-3d9d9fd2aa86"), new { Title = "11" }),
            //(new Guid("877860ac-b64a-4f0b-b5f8-6ed364012dfa"), new { Title = "12" }),
            //(new Guid("091aabb9-ee35-4244-99e8-7c0b41368828"), new { Title = "13" }),
            //(new Guid("8bab3f3d-42b9-43d3-8df0-90a3d53c751d"), new { Title = "14" }),
            //(new Guid("29a2e4ca-a677-447a-9f06-66e3f2a5a4a0"), new { Title = "15" }),
            //(new Guid("43e2bac5-8633-44d6-97de-8a91cb0b9509"), new { Title = "16" }),
            //(new Guid("52fdd0a1-f18d-4f06-bb26-e4fee3de18a2"), new { Title = "17" }),
            //(new Guid("47caed7b-9f9c-42ba-8ddc-e29c45f92435"), new { Title = "18" }),
            //(new Guid("44a695c3-1f89-4da4-96d4-a4028138a42a"), new { Title = "19" }),
            //(new Guid("d85db02a-8fd0-4ded-b62d-c0639c1eb380"), new { Title = "20" }),
            //(new Guid("60f94676-b5e5-4d77-8196-61363a726b8b"), new { Title = "21" }),
            //(new Guid("0bbb037a-9905-4d41-8c0d-c7aed3bbcd4c"), new { Title = "22" }),
            //(new Guid("01f49cd1-ea62-4a79-85f5-600dca126d13"), new { Title = "23" }),
            //(new Guid("3a75cc26-0e29-46a4-81e7-c15ff851a3dd"), new { Title = "24" }),
            //(new Guid("9ad11352-4efa-49cf-b163-ae7f6fb42bc4"), new { Title = "25" }),
            //(new Guid("bb6d02c7-6021-45a1-b093-a87e8c848303"), new { Title = "26" }),
            //(new Guid("e68484fb-e329-4774-aa80-21f6f33b1244"), new { Title = "27" }),
            //(new Guid("5574c99b-23f2-4b6c-850e-fefac3b1bdca"), new { Title = "28" }),
            //(new Guid("987b0f27-7856-489a-94a0-9be0356cafee"), new { Title = "29" }),
            //(new Guid("6476f756-5181-4240-ba85-72aa54f6f6f9"), new { Title = "30" }),
            //(new Guid("13ebfdb7-d8d9-41a6-b680-d2e0737532ca"), new { Title = "31" }),
            //(new Guid("c481938c-5a31-4654-b58e-259b6bbf3b7c"), new { Title = "32" }),
            //(new Guid("84e40fab-a32f-43f5-8aaf-01ac49a62d23"), new { Title = "33" }),
            //(new Guid("4f7bacc8-8c49-4b10-8c84-b081dd4c51a2"), new { Title = "34" }),
            //(new Guid("3c6317eb-3dfd-4e4b-af36-7977f38bb2d0"), new { Title = "35" }),
            //(new Guid("41d1662d-2b2a-40b6-9ae1-c6a2bad8327d"), new { Title = "36" }),
            //(new Guid("6e5b5f69-4396-4927-aef9-50a2231ac963"), new { Title = "37" }),
            //(new Guid("bf72e7ad-4f30-42eb-aed1-1a73518c9c9c"), new { Title = "38" }),
            //(new Guid("2da37073-5cd1-4790-8396-23541e6c1509"), new { Title = "39" }),
            //(new Guid("b666cde4-ecff-4295-9cf3-20dc13599e24"), new { Title = "40" }),
            //(new Guid("76058d3e-36d0-42c8-8cf9-253f3ac763d6"), new { Title = "41" }),
            //(new Guid("70734afe-3a90-4692-aedb-720b5b3e3e15"), new { Title = "42" }),
            //(new Guid("04f28db1-bbbd-4078-9aca-e8ab8894cae6"), new { Title = "43" }),
            //(new Guid("9b9e813b-c4a8-4f02-888d-734bb7b200d5"), new { Title = "44" }),
            //(new Guid("7e1cb0ad-dd88-432d-9031-d4c00b0c0341"), new { Title = "45" }),
            //(new Guid("b22b4d6f-33d7-4ab4-8799-5f53394a2b51"), new { Title = "46" }),
            //(new Guid("119d671c-f086-4c18-b831-c00ec50f7afa"), new { Title = "47" }),
            //(new Guid("2a00a5de-a995-4f11-a6e0-ebc2df95df38"), new { Title = "48" }),
            //(new Guid("b51384dd-da74-4093-825c-ae76bb925444"), new { Title = "49" }),
            //(new Guid("ec0417a1-111f-43c5-8587-f6b15cba6d2d"), new { Title = "50" }),
            //(new Guid("f24e1a71-8b7c-44e5-a0e5-70af57827afe"), new { Title = "51" }),
            //(new Guid("e4068a8c-5767-4cb3-bfa5-536a2fa0f9e0"), new { Title = "52" }),
            //(new Guid("d8705988-56dc-40a3-86bc-15c45d881be0"), new { Title = "53" }),
            //(new Guid("b060836c-6e66-4e6f-b216-31bace8efc28"), new { Title = "54" }),
            //(new Guid("a340cb58-5487-42fa-90b7-d74aba582f8e"), new { Title = "55" }),
            //(new Guid("bbcf4733-0f2a-4905-b78a-075d8c038ea3"), new { Title = "56" }),
            //(new Guid("99657234-e9cd-4094-862a-a0a87cbfaa19"), new { Title = "57" }),
            //(new Guid("bfe62e6e-9e8f-4079-8032-81e716292827"), new { Title = "58" }),
            //(new Guid("5dd36182-1c07-4cb6-9a56-3dafa6ccc763"), new { Title = "59" }),
            //(new Guid("5992bfee-e626-448b-b835-2ad63ebc2c77"), new { Title = "60" }),
            //(new Guid("e0034038-763e-44b1-ba30-50f4c89f98ea"), new { Title = "61" }),
            //(new Guid("c9a44eb8-5559-4e62-b192-89966460e027"), new { Title = "62" }),
            //(new Guid("c1ead86a-9f87-4a91-a0f0-71f90a6fcd6a"), new { Title = "63" }),
            //(new Guid("91d53c3d-18a8-4cbc-bf0d-e0ada929e239"), new { Title = "64" }),
            //(new Guid("980a50d0-c389-4343-a445-a1c3d529de54"), new { Title = "65" }),
            //(new Guid("ac91763e-f84a-4932-a70c-9ad163da282f"), new { Title = "66" }),
            //(new Guid("a6962fe2-6af0-4961-be2e-c5044227417f"), new { Title = "67" }),
            //(new Guid("0034b491-01c4-4c1e-b513-c4da6217b9e2"), new { Title = "68" }),
            //(new Guid("578cc73c-5041-40b9-84b9-cb81a5bcc8a8"), new { Title = "69" }),
            //(new Guid("4c4ff965-b8ab-4058-9da0-9fdb7de7f8de"), new { Title = "70" }),
            //(new Guid("f9ff6c7e-0d68-436a-8fe8-a79e469ee12b"), new { Title = "71" }),
            //(new Guid("9d503a69-7992-464f-a707-6566af890ae5"), new { Title = "72" }),
            //(new Guid("4262c9f2-4447-45e2-ad7d-d7f1dd57429c"), new { Title = "73" }),
            //(new Guid("694e38c4-6b84-4a34-83d7-8fca3bc088fc"), new { Title = "74" }),
            //(new Guid("1f396329-5496-4173-b5b2-05de8d00ccb9"), new { Title = "75" }),
            //(new Guid("7dfdc766-3a5b-4624-9d0e-98a9a948b9e3"), new { Title = "76" }),
            //(new Guid("bed472ce-2db9-437b-9c5a-3454dc417dc6"), new { Title = "77" }),
            //(new Guid("c5a7b444-5564-4674-a38b-d84f06fcf239"), new { Title = "78" }),
            //(new Guid("d70fbdc1-47cc-4868-bbef-cffacd0dc960"), new { Title = "79" }),
            //(new Guid("848344c0-48f7-4332-b44d-45c0b95e4ae8"), new { Title = "80" }),
            //(new Guid("af09012a-02e9-4227-9b36-08974f4657b1"), new { Title = "81" }),
            //(new Guid("41709c56-74ee-415f-8853-985b93afcbff"), new { Title = "82" }),
            //(new Guid("96006f77-4bfe-43ea-9561-684428fcc715"), new { Title = "83" }),
            //(new Guid("21dc4581-a060-4400-92a6-834b6151942d"), new { Title = "84" }),
            //(new Guid("4ab61169-b5d3-431f-8900-5abca4c15cc3"), new { Title = "85" }),
            //(new Guid("f92d5d99-4f96-42e6-bdaf-f768b4e5730d"), new { Title = "86" }),
            //(new Guid("809816b7-5395-4fee-bab0-5a40a450f0fb"), new { Title = "87" }),
            //(new Guid("d91913d0-0e52-4189-981e-406cb661572a"), new { Title = "88" }),
            //(new Guid("360b9506-bb37-485d-801b-939d7a691620"), new { Title = "89" }),
            //(new Guid("434db518-163d-4490-98d1-5ef5ecbbc840"), new { Title = "90" }),
            //(new Guid("e834aab3-9ab8-485c-93f6-58d12cc43b37"), new { Title = "91" }),
            //(new Guid("54a5b303-1c54-437a-899d-d357560ecb05"), new { Title = "92" }),
            //(new Guid("33a5e56c-eebb-461f-bdbd-7c8b19c97c2a"), new { Title = "93" }),
            //(new Guid("ec780860-9e81-4491-bc1b-068fa9212343"), new { Title = "94" }),
            //(new Guid("e7e63290-dbd1-4b9e-843a-7b29cfff5402"), new { Title = "95" }),
            //(new Guid("5e732cf8-ab95-41dc-8fc9-1ac56e64a3eb"), new { Title = "96" }),
            //(new Guid("8af2758b-9fd7-40a6-b967-1f70a5cfbe2f"), new { Title = "97" }),
            //(new Guid("50daec8c-9d2a-4b3e-af81-5bd43a78f62f"), new { Title = "98" }),
            //(new Guid("d2bb7f65-7465-4de5-bd0f-ebe4db0630ae"), new { Title = "99" }),
            //(new Guid("4ff02c89-81ec-4c7d-85a7-48e1555831e1"), new { Title = "100" }),
            //(new Guid("90905411-f5cc-4312-bfa5-92a9fff57bbc"), new { Title = "101" }),
            //(new Guid("11f717cb-b77a-4fa2-880f-d6699339d5f0"), new { Title = "102" }),
            //(new Guid("c190d93e-3d71-4923-8fe4-645db552d1c4"), new { Title = "103" }),
            //(new Guid("78bfd3a3-cb17-451a-901b-e18c786db658"), new { Title = "104" }),
            //(new Guid("ad4fa006-09e2-4c02-b752-c2a0d4f37c26"), new { Title = "105" }),
            //(new Guid("7abcf787-2b80-487d-ba5a-451ccaffee67"), new { Title = "106" }),
            //(new Guid("321d8652-f30e-4e3d-bfad-cdac2abe844a"), new { Title = "107" }),
            //(new Guid("c2d45961-d65e-49f3-81b2-955e5e3f8377"), new { Title = "108" }),
            //(new Guid("5055e164-6dc9-4e23-a263-0a09b00718ca"), new { Title = "109" }),
            //(new Guid("35739616-fe08-41a2-86b9-a761ad4271ad"), new { Title = "110" }),
            //(new Guid("8130bac4-6623-4b06-97b3-b9c179eb5a46"), new { Title = "111" }),
            //(new Guid("d3412d4a-c81d-4357-ab48-8303893a9bdc"), new { Title = "112" }),
            //(new Guid("c990a767-05bf-4d79-bc50-db09eb6c5aa4"), new { Title = "113" }),
            //(new Guid("95c2b2c7-84cb-4a47-9641-9daf30bca568"), new { Title = "114" }),
            //(new Guid("def1aed1-c6f3-447a-9eec-d8e62cada129"), new { Title = "115" }),
            //(new Guid("1c5d3196-d4e0-4bfa-a008-4e56bff9d6d8"), new { Title = "116" }),
            //(new Guid("b20cd121-f081-4c8a-89ac-2ae9c377c2d0"), new { Title = "117" }),
            //(new Guid("79f4903e-4c00-4cbf-bf0d-679e8075dc6d"), new { Title = "118" }),
            //(new Guid("f1428011-69d9-43e9-be0d-ab0b31ca9c56"), new { Title = "119" }),
            //(new Guid("c6f87878-a96a-4cad-8193-00d0f782b5d5"), new { Title = "120" }),
            //(new Guid("aa0cd08e-379c-49cc-a2b0-3f213ecfb807"), new { Title = "121" }),
            //(new Guid("3e5865e4-0854-4dbd-90e5-83abd08aebb0"), new { Title = "122" }),
            //(new Guid("8b036819-fd94-433f-b579-2eeb094aae71"), new { Title = "123" }),
            //(new Guid("d9c1cc04-d690-4d33-adbd-8c6aac54aad2"), new { Title = "124" }),
            //(new Guid("801cfdd4-41f7-4a35-9461-52aed98f31e3"), new { Title = "125" }),
            //(new Guid("69ad2583-2e52-478d-adb3-1474da332100"), new { Title = "126" }),
            //(new Guid("24b375e7-4b01-4b84-8050-9cccdb533e47"), new { Title = "127" }),
            //(new Guid("11d4c1dc-e621-4772-ad60-ac3a14366460"), new { Title = "128" }),
            //(new Guid("50180f9f-0393-4ae3-b8be-6a0b988d1115"), new { Title = "129" }),
            //(new Guid("2ae5fae9-61ae-4894-bd0d-3c2e50e0e289"), new { Title = "130" }),
            //(new Guid("f33d94fc-7e83-4999-8f8d-be87dc0f7fe3"), new { Title = "131" }),
            //(new Guid("0c5f0a6b-5379-492d-b912-dc2603d7a24e"), new { Title = "132" }),
            //(new Guid("0f010581-5a46-4a46-a455-d601f88f5421"), new { Title = "133" }),
            //(new Guid("2b2bda43-ec4b-43c0-b10c-953f9f768183"), new { Title = "134" }),
            //(new Guid("93bdd66c-8ea2-4444-8676-469bd4195054"), new { Title = "135" }),
            //(new Guid("0586aff3-947d-48f0-b841-8ef7cfbabc5c"), new { Title = "136" }),
            //(new Guid("19139cdc-41c4-4975-9fe9-34ff06d6fe15"), new { Title = "137" }),
            //(new Guid("0bd88b04-d154-4743-8f27-c3eb9fe14e01"), new { Title = "138" }),
            //(new Guid("e0f5279b-160f-49fd-be83-46b98df21ba9"), new { Title = "139" }),
            //(new Guid("f41d29f0-d9c1-4f2f-9917-fbd173d0ffa4"), new { Title = "140" }),
            //(new Guid("92c1df32-2929-4ce3-8f6d-0c625b513102"), new { Title = "141" }),
            //(new Guid("be844782-657e-4667-918b-57b1dd4b18f0"), new { Title = "142" }),
            //(new Guid("70c2932e-2f11-4ec3-a23c-29213e0f851b"), new { Title = "143" }),
            //(new Guid("a71b5d93-d7fe-4c3c-813e-b6a23c2b989e"), new { Title = "144" }),
            //(new Guid("5d782abb-bdf8-4da9-9cd1-782ad9a8151e"), new { Title = "145" }),
            //(new Guid("6464c378-84d7-4c95-a4d0-7dae6a5fa917"), new { Title = "146" }),
            //(new Guid("4147ade7-ed99-4101-b0a8-04aa70e2b07b"), new { Title = "147" }),
            //(new Guid("32a1e9ca-f4b7-425a-b204-c4a9675ea2da"), new { Title = "148" }),
            //(new Guid("5979c563-1933-4d21-b03c-1a28e4a2a6cb"), new { Title = "149" }),
            //(new Guid("94e2ee50-9004-4d91-9905-20412cf6ff32"), new { Title = "150" }),
            //(new Guid("f013e157-0969-41e2-824b-5896fd6a7133"), new { Title = "151" }),
            //(new Guid("f32ecabb-fe32-4791-9179-26d001430e8c"), new { Title = "152" }),
            //(new Guid("39962865-a374-4d6a-8c33-6e41ab01ed28"), new { Title = "153" }),
            //(new Guid("c143596a-5bb3-49f3-9db2-312ac1d53f37"), new { Title = "154" }),
            //(new Guid("fdc44073-50cb-4bce-acc1-0277448677e9"), new { Title = "155" }),
            //(new Guid("3992ad44-7af6-4c77-8d19-2ec57c2c19e8"), new { Title = "156" }),
            //(new Guid("71c4b4a7-c8aa-40d7-87e3-06300a9739c9"), new { Title = "157" }),
            //(new Guid("a5b23422-f0de-4bb9-841f-5fa814cdbc62"), new { Title = "158" }),
            //(new Guid("3e2ae445-3f18-453c-bc0a-eec71fd78c5a"), new { Title = "159" }),
            //(new Guid("859a8bda-d347-4b34-a8ef-22914554d8c1"), new { Title = "160" }),
            //(new Guid("52baa0a8-a400-47ba-b4da-0783fc11bdfd"), new { Title = "161" }),
            //(new Guid("94355529-d627-4010-ae00-42233c954770"), new { Title = "162" }),
            //(new Guid("e13807ea-ce2a-4cf2-b9d7-60567285169b"), new { Title = "163" }),
            //(new Guid("8b49a404-c17c-4f1a-a4f8-bb46191b2c8f"), new { Title = "164" }),
            //(new Guid("dd9da333-1ae4-436d-8376-89b0e1e8c500"), new { Title = "165" }),
            //(new Guid("43df3fa1-9763-49f7-b4c2-0f1bcec5edcb"), new { Title = "166" }),
            //(new Guid("5db32b98-b4ea-4b6a-b8e3-1d5598c912f4"), new { Title = "167" }),
            //(new Guid("542bee6d-2850-4389-bb7d-e1bc97ad363b"), new { Title = "168" }),
            //(new Guid("9a1d20c9-3870-4a02-bebe-abf8214a05cc"), new { Title = "169" }),
            //(new Guid("a52574b8-c8b3-4812-905a-8173b789e5e7"), new { Title = "170" }),
            //(new Guid("357d6241-7ec3-42ab-9581-424131503eb9"), new { Title = "171" }),
            //(new Guid("37102234-e547-4cde-9cd3-b3679c65be6e"), new { Title = "172" }),
            //(new Guid("082d7199-ee6a-411d-a216-06c02880a845"), new { Title = "173" }),
            //(new Guid("4af72fb6-7ff1-4d9f-9811-b4f05daa7d1f"), new { Title = "174" }),
            //(new Guid("51722e44-56a0-48e2-a124-103902c32320"), new { Title = "175" }),
            //(new Guid("f499df8c-2f2d-415f-8439-e4130e6e8928"), new { Title = "176" }),
            //(new Guid("8d55202e-3262-4956-8004-94d019a71d04"), new { Title = "177" }),
            //(new Guid("b009d767-f1fa-4ba2-b706-6158754f21c8"), new { Title = "178" }),
            //(new Guid("c7a9dcfe-ce65-49fe-a146-529ac522095d"), new { Title = "179" }),
            //(new Guid("00f30867-794c-4e44-8203-cf542a5d38f6"), new { Title = "180" }),
            //(new Guid("740ea3cd-84f0-4a39-a9f7-1b21f45e60d1"), new { Title = "181" }),
            //(new Guid("2ef8a60f-29b1-4509-a901-15b901787f06"), new { Title = "182" }),
            //(new Guid("c443ec5e-8107-4b67-bf84-dbebd9e061a4"), new { Title = "183" }),
            //(new Guid("166f952c-f2b5-4c32-b15d-cf2a73c7bfa8"), new { Title = "184" }),
            //(new Guid("cbc8889d-3e7a-4bb0-9ff9-814d4f203a59"), new { Title = "185" }),
            //(new Guid("277eb4db-ceef-4f2d-9a97-13c1aebcbaf0"), new { Title = "186" }),
            //(new Guid("4e90e43c-b35b-4ed3-bc0a-868963336874"), new { Title = "187" }),
            //(new Guid("73f7d9c6-3149-453d-8d6e-bfc557b51a17"), new { Title = "188" }),
            //(new Guid("230c578d-9d43-404c-845e-055b33218e4c"), new { Title = "189" }),
            //(new Guid("ae0ed1f2-597d-4439-be60-113dacab8914"), new { Title = "190" }),
            //(new Guid("3cc5467c-5ead-46ef-923a-dab9537e7722"), new { Title = "191" }),
            //(new Guid("cc132529-90a4-4ce1-9002-0e2ad38b374d"), new { Title = "192" }),
            //(new Guid("05f1173d-875a-47cf-949a-156368c1bc7e"), new { Title = "193" }),
            //(new Guid("cc72584a-7332-4cb7-a3f7-4a44c3993cca"), new { Title = "194" }),
            //(new Guid("77e08935-121f-45a0-b51e-0b7e85a3e721"), new { Title = "195" }),
            //(new Guid("22e5e653-b1fa-40b2-b101-b601926e643d"), new { Title = "196" }),
            //(new Guid("ad62d717-a98f-4f9a-bcdb-fa2f42abb588"), new { Title = "197" }),
            //(new Guid("c5f5248b-e4da-453e-a904-a6be8815bbab"), new { Title = "198" }),
            //(new Guid("7dbadd6f-6d88-42cc-9a58-c3a9caf89440"), new { Title = "199" }),
            //(new Guid("15a5964e-d4e0-4df2-b2e1-3a9e831ed08f"), new { Title = "200" }),
            //(new Guid("cd2e7859-f7ad-44a0-98dc-5ef39cc08dc9"), new { Title = "201" }),
            //(new Guid("3627d60a-94fb-4add-b238-ad2ac7a5a448"), new { Title = "202" }),
            //(new Guid("b76e6c71-96d4-46e2-8dbe-5c90ee3ce3b8"), new { Title = "203" }),
            //(new Guid("447c453d-7f77-443b-a877-f269708e2363"), new { Title = "204" }),
            //(new Guid("d44fa7e5-e140-49b2-a5ca-d71f185d9fb9"), new { Title = "205" }),
            //(new Guid("734f7225-6fc1-4175-8a74-f02ac6736f81"), new { Title = "206" }),
            //(new Guid("4256a439-a45d-4c5d-a2ad-5fed3ee26d56"), new { Title = "207" }),
            //(new Guid("1868acac-717b-448d-8f8e-eb0f0df0b7ab"), new { Title = "208" }),
            //(new Guid("77d47c31-eeb1-4f77-b7c6-94eb4f43d265"), new { Title = "209" }),
            //(new Guid("baa34024-87d7-40fa-a650-f8e31f9386bd"), new { Title = "210" }),
            //(new Guid("fb993b82-673c-487d-a784-8a121590b1e9"), new { Title = "211" }),
            //(new Guid("bda1a745-89a2-48c1-bf41-21e776513110"), new { Title = "212" }),
            //(new Guid("96f73d36-6d22-42e8-93f5-ef008c964434"), new { Title = "213" }),
            //(new Guid("16d6528a-8078-4df0-893a-dc5fa2e10078"), new { Title = "214" }),
            //(new Guid("7ec0b6b6-7d3e-44bb-a6bd-7858142702c2"), new { Title = "215" }),
            //(new Guid("e6757ded-b993-4522-8412-9b451728941e"), new { Title = "216" }),
            //(new Guid("62d98cb7-8305-4073-91ea-3cd50d2ceed7"), new { Title = "217" }),
            //(new Guid("7c90fa82-71e8-4b55-8591-5cecc5cba440"), new { Title = "218" }),
            //(new Guid("3028c5ed-7507-4d8e-84ab-7e51493154d3"), new { Title = "219" }),
            //(new Guid("8c903c19-a674-46bd-b896-13243fd40fa5"), new { Title = "220" }),
            //(new Guid("0da9b2ad-e59b-4330-a59b-336b36213c83"), new { Title = "221" }),
            //(new Guid("1d4f6001-d786-45f0-9c0c-7c405d80d804"), new { Title = "222" }),
            //(new Guid("a2ec4627-9955-4939-8bb0-a36558ea3857"), new { Title = "223" }),
            //(new Guid("18f3c76d-81ce-4f23-8e2b-84b26815b241"), new { Title = "224" }),
            //(new Guid("3b14ed49-50f5-402f-b225-09dd9db5f684"), new { Title = "225" }),
            //(new Guid("715ce886-ecf2-4fd6-b0d4-d24f996f34d5"), new { Title = "226" }),
            //(new Guid("c96a6742-2837-4c14-8dfa-42ab08e1c826"), new { Title = "227" }),
            //(new Guid("1d3b4506-9f7f-458a-8714-d74970333783"), new { Title = "228" }),
            //(new Guid("e3b885a4-a92d-4404-b35b-7cb2b27015a1"), new { Title = "229" }),
            //(new Guid("9052976c-28b3-459d-a973-1dd34d98ad0c"), new { Title = "230" }),
            //(new Guid("bf1838b9-09f3-4f75-a708-ded9295fcd13"), new { Title = "231" }),
            //(new Guid("7d1effd0-7cbf-4293-b849-89dcaae637a9"), new { Title = "232" }),
            //(new Guid("f1d51687-6177-4bfc-bf84-25061c964f80"), new { Title = "233" }),
            //(new Guid("95c7841f-21c5-4f36-acef-aae9910e65c8"), new { Title = "234" }),
            //(new Guid("99737c53-582c-4303-a310-24aa6ae3ca96"), new { Title = "235" }),
            //(new Guid("a562d4c3-826e-44d9-9265-8c0803172d5e"), new { Title = "236" }),
            //(new Guid("d6f5ac26-75a1-4263-a185-a3baa5fb368f"), new { Title = "237" }),
            //(new Guid("f203c1cd-e004-4257-ba81-71bca624a78e"), new { Title = "238" }),
            //(new Guid("1c5d1a0b-a519-4c80-aa54-29bd690a29da"), new { Title = "239" }),
            //(new Guid("47a39348-1941-4303-a13d-ea92610734b3"), new { Title = "240" }),
            //(new Guid("85ca6350-be75-4d7d-ad92-fa045f747ac3"), new { Title = "241" }),
            //(new Guid("1174b21b-8f67-4b35-bfc1-f75d03f8a314"), new { Title = "242" }),
            //(new Guid("a0e5723a-863e-4fc5-8845-33efd590b32e"), new { Title = "243" }),
            //(new Guid("e1ae3b63-12cb-4597-ad2c-8e830e06ddc5"), new { Title = "244" }),
            //(new Guid("615fc78b-25a2-4a0d-abca-9f39eb1c95ed"), new { Title = "245" }),
            //(new Guid("c018b7c5-9040-4f16-855a-12c74be88436"), new { Title = "246" }),
            //(new Guid("77c4266a-1735-4aa4-870d-3035f0883a74"), new { Title = "247" }),
            //(new Guid("edd254af-d708-4eb3-b2a0-e42f1beef079"), new { Title = "248" }),
            //(new Guid("d54975c1-f564-4a75-8e1f-f95ad26216d8"), new { Title = "249" }),
            //(new Guid("09e4fecd-104a-4db4-8fb8-ddc9417f714e"), new { Title = "250" }),
            //(new Guid("6ec241a7-a636-49f4-bba4-d2312f3c152a"), new { Title = "251" }),
            //(new Guid("d65ebc63-14d4-49ab-ac5f-32c9e0c9f0d9"), new { Title = "252" }),
            //(new Guid("b8d6ec11-5154-422b-86d7-8a671e89b83a"), new { Title = "253" }),
            //(new Guid("2c290224-3210-4514-8caf-46fb4f4862cf"), new { Title = "254" }),
            //(new Guid("de2773a6-988e-47a1-9e15-42b3dcf493c4"), new { Title = "255" }),
            //(new Guid("5ce3225b-ecfd-43c2-bd22-1de158e6f9ae"), new { Title = "256" }),
            //(new Guid("146fe2b9-bb02-494a-81ce-9f1102642b39"), new { Title = "257" }),
            //(new Guid("396993a5-acee-4c5e-b336-f8ce1f4cbcee"), new { Title = "258" }),
            //(new Guid("ba97c1f9-7d90-4015-89a6-af7da5fa6845"), new { Title = "259" }),
            //(new Guid("34cb1548-99d4-4c36-a792-491da2273c14"), new { Title = "260" }),
            //(new Guid("624a34b5-78e7-48db-8bc2-022cb9d342ad"), new { Title = "261" }),
            //(new Guid("edc1b1f6-d5ac-4ee8-aa5a-faad6e9a9b64"), new { Title = "262" }),
            //(new Guid("7c1810dd-ec92-45b6-87cf-5c5d6f8b8556"), new { Title = "263" }),
            //(new Guid("0043da78-9386-421a-80fd-b8d416d033e8"), new { Title = "264" }),
            //(new Guid("666a9a01-e65f-46e3-b825-9c8533afe349"), new { Title = "265" }),
            //(new Guid("b8be2f92-ef36-48bc-a1aa-05043cd58b38"), new { Title = "266" }),
            //(new Guid("5962ce8d-3d95-4496-8443-b2a5215dedf1"), new { Title = "267" }),
            //(new Guid("67a5f326-aec5-4c1e-9f84-438d02791e4e"), new { Title = "268" }),
            //(new Guid("97e0f7ca-d6f5-496a-88a2-029e25443567"), new { Title = "269" }),
            //(new Guid("f662bd94-9dac-452c-8de7-26d42b6a8c0e"), new { Title = "270" }),
            //(new Guid("c719eb4c-949f-4793-8bd2-a4c928f11c36"), new { Title = "271" }),
            //(new Guid("8b37c53c-f47c-4ffc-80f9-951e42c747bc"), new { Title = "272" }),


        };
    }
}