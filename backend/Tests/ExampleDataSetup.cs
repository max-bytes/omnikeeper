using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tests.Integration;
using Omnikeeper.Base.Model.TraitBased;

namespace Tests
{
    public class ExampleDataSetup
    {
        public static async Task<IEnumerable<string>> SetupCMDBExampleData(int numCIs, int numLayers, int numAttributeInserts, int numDataTransactions, bool filterDuplicates, IServiceProvider serviceProvider, IModelContextBuilder modelContextBuilder)
        {
            var changesetModel = serviceProvider.GetRequiredService<IChangesetModel>();
            var layerModel = serviceProvider.GetRequiredService<ILayerModel>();
            var attributeModel = serviceProvider.GetRequiredService<IAttributeModel>();
            var ciModel = serviceProvider.GetRequiredService<ICIModel>();
            var userModel = serviceProvider.GetRequiredService<IUserInDatabaseModel>();
            var traitModel = serviceProvider.GetRequiredService<GenericTraitEntityModel<RecursiveTrait, string>>();
            var user = await DBSetup.SetupUser(userModel, modelContextBuilder.BuildImmediate());
            var metaConfigurationModel = serviceProvider.GetRequiredService<IMetaConfigurationModel>();

            var random = new Random(3);

            Func<IAttributeValue> randomAttributeValue = () => new AttributeScalarValueText("V" + RandomUtility.GenerateRandomString(8, random));
            var possibleAttributes = new ((string name, Func<IAttributeValue> value), int chance)[]
            {
                (("hostname", randomAttributeValue), 5),
                ((ICIModel.NameAttribute, randomAttributeValue), 5),
                (("ipAddress", randomAttributeValue), 1),
                (("application_name", randomAttributeValue), 1),
                (("os_family", () => new AttributeScalarValueText(RandomUtility.GetRandom(random, ("Windows", 10), ("Redhat", 3), ("Gentoo", 1)))), 5),
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

            var ciids = Enumerable.Range(0, numCIs).Select(i =>
            {
                return Guid.NewGuid();
            }).ToList();

            var layerNames = Enumerable.Range(0, numLayers).Select(i =>
            {
                var identity = "l" + i;// RandomUtility.GenerateRandomString(8, random);
                return identity;
            }).ToList();


            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(modelContextBuilder.BuildImmediate());

            List<Layer> layers;
            using (var mc = modelContextBuilder.BuildDeferred())
            {
                // okconfig layer
                layerModel.UpsertLayer("__okconfig", mc).GetAwaiter().GetResult();

                await ciModel.BulkCreateCIs(ciids, mc);

                layers = layerNames.Select(identity =>
                {
                    return layerModel.UpsertLayer(identity, mc).GetAwaiter().GetResult();
                }).ToList();

                var rts = Traits.Get();
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                foreach (var rt in rts)
                    await traitModel.InsertOrUpdate(rt,
                        metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                        new DataOriginV1(DataOriginType.Manual), changeset, mc);
                mc.Commit();
            }

            var fragments = Enumerable.Range(0, numAttributeInserts).Select(i =>
            {
                var nameValue = RandomUtility.GetRandom(random, possibleAttributes);
                var name = nameValue.name;
                var value = nameValue.value();
                var ciid = ciids.GetRandom(random);
                var layer = layers.GetRandom(random)!;
                var dataTransaction = random.Next(0, numDataTransactions);
                return (layer, dataTransaction, new BulkCIAttributeDataLayerScope.Fragment(name, value, ciid));
            }).GroupBy(t => t.dataTransaction, t => (t.layer, t.Item3));

            foreach (var fg in fragments)
            {
                var dataTransaction = fg.Key;
                using var mc = modelContextBuilder.BuildDeferred();
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);

                var fragmentsInLayers = fg.GroupBy(t => t.layer.ID, t => t.Item2);

                foreach (var fl in fragmentsInLayers)
                {
                    IEnumerable<BulkCIAttributeDataLayerScope.Fragment> finalFragments = fl;
                    // filter out duplicates
                    if (filterDuplicates)
                    {
                        var filtered = new Dictionary<string, BulkCIAttributeDataLayerScope.Fragment>();
                        foreach (var f in fl)
                        {
                            var hash = CIAttribute.CreateInformationHash(f.Name, f.CIID);
                            filtered[hash] = f;
                        }
                        finalFragments = filtered.Values;
                    }

                    await attributeModel.BulkReplaceAttributes(new BulkCIAttributeDataLayerScope("", fl.Key, finalFragments), changeset, new DataOriginV1(DataOriginType.Manual), mc, MaskHandlingForRemovalApplyNoMask.Instance);
                }
                mc.Commit();
            }

            return layerNames;
        }

        public static class Traits
        {
            public static IEnumerable<RecursiveTrait> Get()
            {
                var traits = new RecursiveTrait[]
                    {
                    // hosts
                    new RecursiveTrait("host", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                        new TraitAttribute("hostname",
                            CIAttributeTemplate.BuildFromParams("hostname", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),
                    new RecursiveTrait("host_windows", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                        new TraitAttribute("os_family",
                            CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                                new CIAttributeValueConstraintTextRegex(new Regex(@"Windows", RegexOptions.IgnoreCase)))
                        )
                    }, requiredTraits: new string[] { "host" }),

                    new RecursiveTrait("host_linux", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                        new TraitAttribute("os_family",
                            CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                                new CIAttributeValueConstraintTextRegex(new Regex(@"(RedHat|CentOS|Debian|Suse|Gentoo|Archlinux|Mandrake)", RegexOptions.IgnoreCase)))
                        )
                    }, requiredTraits: new string[] { "host" }),

                    // linux disk devices
                    new RecursiveTrait("linux_block_device", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                        new TraitAttribute("device",
                            CIAttributeTemplate.BuildFromParams("device", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        new TraitAttribute("mount",
                            CIAttributeTemplate.BuildFromParams("mount", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // linux network_interface
                    new RecursiveTrait("linux_network_interface", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
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
                    new RecursiveTrait("application", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                        new TraitAttribute("name",
                            CIAttributeTemplate.BuildFromParams("application_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // automation / ansible
                    new RecursiveTrait("ansible_can_deploy_to_it", new TraitOriginV1(TraitOriginType.Data),
                        new List<TraitAttribute>() {
                            new TraitAttribute("hostname", // TODO: make this an anyOf[CIAttributeTemplate], or use dependent trait host
                                CIAttributeTemplate.BuildFromParams("ipAddress",    AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                            )
                        },
                        new List<TraitAttribute>() {
                            new TraitAttribute("variables",
                                CIAttributeTemplate.BuildFromParams("automation.ansible_variables", AttributeValueType.JSON, false)
                            )
                        }
                        //new List<TraitRelation>() {
                        //    new TraitRelation("ansible_groups",
                        //        new RelationTemplate("has_ansible_group", 1, null)
                        //    )
                        //}
                    ),
                    };

                return traits;
            }
        }
    }
}
