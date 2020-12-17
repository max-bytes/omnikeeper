using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using Omnikeeper.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;
using Omnikeeper.Base.Utils.ModelContext;
using Microsoft.Extensions.Logging.Abstractions;
using Omnikeeper.Base.Entity.DataOrigin;
using Tests.Integration;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Controllers;
using Omnikeeper.Base.Entity;
using System.Text.RegularExpressions;

namespace PerfTests
{
    class GetTraitsTest : DIServicedTestBase
    {
        private List<Guid> ciNames = new List<Guid>();
        private List<string> layerNames = new List<string>();

        public GetTraitsTest() : base(true)
        {

        }


        private V GetRandom<V>(Random r, params (V item, int chance)[] possibilities)
        {
            var indices = possibilities.SelectMany((p, i) => Enumerable.Repeat(i, p.chance)).ToArray();
            var index = r.Next(0, indices.Length - 1);
            return possibilities[indices[index]].item;
        }

        public async Task SetupData()
        {
            var timer = new Stopwatch();
            timer.Start();

            var changesetModel = ServiceProvider.GetRequiredService<IChangesetModel>();
            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var userModel = ServiceProvider.GetRequiredService<IUserInDatabaseModel>();
            var traitModel = ServiceProvider.GetRequiredService<IRecursiveTraitModel>();
            var user = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate());
            
            var numCIs = 500;
            var numLayers = 4;
            var numAttributeInserts = 5000;

            var random = new Random(3);

            Func<IAttributeValue> randomAttributeValue = () => new AttributeScalarValueText("V" + RandomString.Generate(8, random));
            var possibleAttributes = new ((string name, Func<IAttributeValue> value), int chance)[]
            {
                (("hostname", randomAttributeValue), 5),
                (("ipAddress", randomAttributeValue), 1),
                (("application_name", randomAttributeValue), 1),
                (("os_family", () => new AttributeScalarValueText(GetRandom(random, ("Windows", 10), ("Redhat", 3), ("Gentoo", 1)))), 5),
                (("device", randomAttributeValue), 1),
                (("mount", randomAttributeValue), 1),
                (("type", randomAttributeValue), 1),
                (("active", randomAttributeValue), 1),
                (("generic-attribute 1", randomAttributeValue), 1),
                (("generic-attribute 2", randomAttributeValue), 1),
                (("generic-attribute 3", randomAttributeValue), 1),
                (("generic-attribute 4", randomAttributeValue), 1),
                (("generic-attribute 5", randomAttributeValue), 1),
            };


            ciNames = Enumerable.Range(0, numCIs).Select(i =>
            {
                return Guid.NewGuid();
            }).ToList();

            layerNames = Enumerable.Range(0, numLayers).Select(i =>
            {
                var identity = "L" + RandomString.Generate(8, random);
                return identity;
            }).ToList();

            var changeset = new ChangesetProxy(user, DateTimeOffset.Now, changesetModel);

            //Console.WriteLine(ciNames.Count());
            using var mc = ModelContextBuilder.BuildDeferred();
            var cis = ciNames.Select(identity =>
            {
                return (ciModel.CreateCI(identity, mc).GetAwaiter().GetResult(), identity);
            }).ToList();

            var layers = layerNames.Select(identity =>
            {
                return layerModel.CreateLayer(identity, mc).GetAwaiter().GetResult();
            }).ToList();

            var attributes = Enumerable.Range(0, numAttributeInserts).Select(i =>
            {
                var nameValue = GetRandom(random, possibleAttributes);
                var name = nameValue.name;
                var value = nameValue.value();
                var layer = layers.GetRandom(random);
                var ciid = cis.GetRandom(random).Item1;
                return attributeModel.InsertAttribute(name!, value, ciid, layer!.ID, changeset, new DataOriginV1(DataOriginType.Manual), mc).GetAwaiter().GetResult();
            }).ToList();

            await traitModel.SetRecursiveTraitSet(Traits.Get(), mc);

            mc.Commit();

            timer.Stop();
            Console.WriteLine($"Setup - Elapsed time: {timer.ElapsedMilliseconds / 1000f}");
        }

        [Test]
        public async Task TestGetMergedCIsWithTrait()
        {
            await SetupData();

            var random = new Random(3);

            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var effectiveTraitModel = ServiceProvider.GetRequiredService<IEffectiveTraitModel>();
            var traitsProvider = ServiceProvider.GetRequiredService<ITraitsProvider>();

            using var mc = ModelContextBuilder.BuildDeferred();

            var layerset = layerModel.BuildLayerSet(layerNames.ToArray(), mc).GetAwaiter().GetResult();
            var time = TimeThreshold.BuildLatest();
            var traitHost = await traitsProvider.GetActiveTrait("host", mc, time);
            var traitLinuxHost = await traitsProvider.GetActiveTrait("linux_host", mc, time);

            var cis = Time("First fetch of CIs with trait host", async () =>
            {
                return await effectiveTraitModel.GetMergedCIsWithTrait(traitHost, layerset, mc, time);
            });
            Console.WriteLine($"Count: {cis.Count()}");
            Console.WriteLine("Attributes of random CI");
            foreach (var aa in cis.GetRandom(random).MergedAttributes)
            {
                Console.WriteLine($"{aa.Value.Attribute.State} {aa.Value.LayerStackIDs[^1]} {aa.Value.Attribute.Name}: {aa.Value.Attribute.Value.Value2String()} ");
            }

            var cis2 = Time("Second fetch of CIs with trait host", async () =>
            {
                return await effectiveTraitModel.GetMergedCIsWithTrait(traitHost, layerset, mc, time);
            });
            var cis3 = Time("Third fetch of CIs with trait host", async () =>
            {
                return await effectiveTraitModel.GetMergedCIsWithTrait(traitHost, layerset, mc, time);
            });
            Console.WriteLine("Attributes of random CI 3");
            foreach (var aa in cis3.GetRandom(random).MergedAttributes)
            {
                Console.WriteLine($"{aa.Value.Attribute.State} {aa.Value.LayerStackIDs[^1]} {aa.Value.Attribute.Name}: {aa.Value.Attribute.Value.Value2String()} ");
            }


            var cis4 = Time("First fetch of CIs with trait linux-host", async () =>
            {
                return await effectiveTraitModel.GetMergedCIsWithTrait(traitLinuxHost, layerset, mc, time);
            });
            Console.WriteLine($"Count: {cis4.Count()}");
            Console.WriteLine("Attributes of random CI");
            foreach (var aa in cis4.GetRandom(random).MergedAttributes)
            {
                Console.WriteLine($"{aa.Value.Attribute.State} {aa.Value.LayerStackIDs[^1]} {aa.Value.Attribute.Name}: {aa.Value.Attribute.Value.Value2String()} ");
            }

            var cis5 = Time("Second fetch of CIs with trait linux-host", async () =>
            {
                return await effectiveTraitModel.GetMergedCIsWithTrait(traitLinuxHost, layerset, mc, time);
            });
        }

        private R Time<R>(string name, Func<Task<R>> f)
        {
            Console.WriteLine($"---");
            var timer = new Stopwatch();
            timer.Start();
            var result = f().GetAwaiter().GetResult();
            timer.Stop();
            Console.WriteLine($"{name} - Elapsed Time: {timer.ElapsedMilliseconds / 1000f}");
            return result;
        }
    }


    public static class Traits
    {
        public static RecursiveTraitSet Get()
        {
            var traits = new RecursiveTrait[]
                {
                    // hosts
                    new RecursiveTrait("host", new List<TraitAttribute>() {
                        new TraitAttribute("hostname",
                            CIAttributeTemplate.BuildFromParams("hostname", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),
                    new RecursiveTrait("windows_host", new List<TraitAttribute>() {
                        new TraitAttribute("os_family",
                            CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                                new CIAttributeValueConstraintTextRegex(new Regex(@"Windows", RegexOptions.IgnoreCase)))
                        )
                    }, requiredTraits: new string[] { "host" }),

                    new RecursiveTrait("linux_host", new List<TraitAttribute>() {
                        new TraitAttribute("os_family",
                            CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                                new CIAttributeValueConstraintTextRegex(new Regex(@"(RedHat|CentOS|Debian|Suse|Gentoo|Archlinux|Mandrake)", RegexOptions.IgnoreCase)))
                        )
                    }, requiredTraits: new string[] { "host" }),

                    // linux disk devices
                    new RecursiveTrait("linux_block_device", new List<TraitAttribute>() {
                        new TraitAttribute("device",
                            CIAttributeTemplate.BuildFromParams("device", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        new TraitAttribute("mount",
                            CIAttributeTemplate.BuildFromParams("mount", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // linux network_interface
                    new RecursiveTrait("linux_network_interface", new List<TraitAttribute>() {
                        new TraitAttribute("device",
                            CIAttributeTemplate.BuildFromParams("device", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        new TraitAttribute("type",
                            CIAttributeTemplate.BuildFromParams("type", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        new TraitAttribute("active",
                            CIAttributeTemplate.BuildFromParams("active", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // applications
                    new RecursiveTrait("application", new List<TraitAttribute>() {
                        new TraitAttribute("name",
                            CIAttributeTemplate.BuildFromParams("application_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // automation / ansible
                    new RecursiveTrait("ansible_can_deploy_to_it",
                        new List<TraitAttribute>() {
                            new TraitAttribute("hostname", // TODO: make this an anyOf[CIAttributeTemplate], or use dependent trait host
                                CIAttributeTemplate.BuildFromParams("ipAddress",    AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                            )
                        },
                        new List<TraitAttribute>() {
                            new TraitAttribute("variables",
                                CIAttributeTemplate.BuildFromParams("automation.ansible_variables", AttributeValueType.JSON, false)
                            )
                        },
                        new List<TraitRelation>() {
                            new TraitRelation("ansible_groups",
                                new RelationTemplate("has_ansible_group", 1, null)
                            )
                        }
                    ),
                };

            return RecursiveTraitSet.Build(traits);
        }
    }
}
