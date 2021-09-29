using Microsoft.Extensions.Logging;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OKPluginNaemonConfig
{
    public class NaemonConfig : CLBBase
    {
        private readonly IRelationModel relationModel;
        private readonly ICIModel ciModel;
        private readonly IEffectiveTraitModel traitModel;
        public NaemonConfig(ICIModel ciModel, IAttributeModel atributeModel, ILayerModel layerModel, IEffectiveTraitModel traitModel, IRelationModel relationModel,
                           IChangesetModel changesetModel, IUserInDatabaseModel userModel)
            : base(atributeModel, layerModel, changesetModel, userModel)
        {
            this.ciModel = ciModel;
            this.relationModel = relationModel;
            this.traitModel = traitModel;
        }

        private List<string> loadcmdbcustomer = new List<string>() { "ADISSEO", "AGRANA", "AMS", "AMSINT", "ANDRITZ", "ATS", "AVESTRA", "AWS" };

        // this is only temporary since this should be read from configuration
        private List<string> naemonsConfigGenerateprofiles = new List<string>() { "svphg200mon001", "svphg200mon002", "uansvclxnaemp01", "uansvclxnaemp02", "uansvclxnaemp03", "uansvclxnaemp04", "uansvclxnaemp05", "uansvclxnaemp06" };


        public override async Task<bool> Run(Layer targetLayer, IChangesetProxy changesetProxy, CLBErrorHandler errorHandler, IModelContext trans, ILogger logger)
        {
            logger.LogDebug("Start naemonConfig");

            // monman data -> layer "monman"
            // cmdb data -> layer "cmdb"
            // livestatus -> layer "livestatus"

            // naemon config CLB -> layer  "naemon_config"

            var layerset = await layerModel.BuildLayerSet(new[] { "testlayer01" }, trans);

            var layersetCMDB = await layerModel.BuildLayerSet(new[] { "cmdb" }, trans);
            var layersetMonman = await layerModel.BuildLayerSet(new[] { "monman" }, trans);
            var layersetLivestatus = await layerModel.BuildLayerSet(new[] { "livestatus" }, trans);
            var layersetNaemonConfig = await layerModel.BuildLayerSet(new[] { "naemon_config" }, trans);

            var allCIsCMDB = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layersetCMDB, false, AllAttributeSelection.Instance, trans, changesetProxy.TimeThreshold);
            var allCIsMonman = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layersetMonman, false, AllAttributeSelection.Instance, trans, changesetProxy.TimeThreshold);
            

            // load naemonInstances
            var naemonInstances = await traitModel.FilterCIsWithTrait(allCIsMonman, Traits.NaemonInstanceFlattened, layerset, trans, changesetProxy.TimeThreshold);
            var naemonIds = new List<string>();
            foreach (var ciItem in naemonInstances)
            {
                //(MergedCI ciItem, _) = naemon.Value;
                var success = ciItem.MergedAttributes.TryGetValue("monman-instance.id", out MergedCIAttribute? attributeNaemonId);

                if (!success)
                {
                    // log error here
                }

                naemonIds.Add(attributeNaemonId!.Attribute.Value.Value2String());
            }

            // select naemon instance with specific ID 'H12037680'
            // this is for intial version of creating naemon configuration
            // at the end we will have a loop that loops through naemoninstances list and selects th
            //var naemonH12037680 = await traitModel.GetEffectiveTraitsWithTraitAttributeValue(Traits.NaemonInstanceFlattened, "monman-instance.id", new AttributeScalarValueText("H12037680"), layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            //(_, (MergedCI ci, EffectiveTrait et)) = naemonH12037680.FirstOrDefault();

            //var success = ci.MergedAttributes.TryGetValue("monman-instance.id", out MergedCIAttribute? attributeNaemonId);

            //if (!success)
            //{
            //    // log error here
            //}

            //var naemonId = attributeNaemonId!.Attribute.Value.Value2String();

            // a list with all CI from datbase

            var ciData = new List<ConfigurationItem>();
            //var hosts = await traitModel.GetEffectiveTraitsForTrait(Traits.HCisFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            var hosts = await traitModel.FilterCIsWithTrait(allCIsCMDB, Traits.HCisFlattened, layersetCMDB, trans, changesetProxy.TimeThreshold);

            foreach (var ciItem in hosts)
            {
                //(MergedCI ciItem, _) = host.Value;

                var item = new ConfigurationItem
                {
                    Type = "HOST"
                };

                foreach (var attribute in ciItem.MergedAttributes)
                {
                    switch (attribute.Key)
                    {
                        case "cmdb.id":
                            item.Id = attribute.Value.Attribute.Value.Value2String();
                            break;
                        case "hostname":
                            item.Name = attribute.Value.Attribute.Value.Value2String();
                            break;
                        case "cmdb.status":
                            item.Status = attribute.Value.Attribute.Value.Value2String();
                            break;
                        default:
                            break;
                    }
                }

                ciData.Add(item);
            }

            // get services
            var services = await traitModel.FilterCIsWithTrait(allCIsCMDB, Traits.ACisFlattened, layersetCMDB, trans, changesetProxy.TimeThreshold);
            //var services = await traitModel.GetEffectiveTraitsForTrait(Traits.ACisFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);
            foreach (var ciItem in services)
            {
                //(MergedCI ciItem, _) = service.Value;
                var item = new ConfigurationItem
                {
                    Type = "SERVICE"
                };
                foreach (var attribute in ciItem.MergedAttributes)
                {
                    switch (attribute.Key)
                    {
                        case "cmdb.id":
                            item.Id = attribute.Value.Attribute.Value.Value2String();
                            break;
                        case "cmdb.name":
                            item.Name = attribute.Value.Attribute.Value.Value2String();
                            break;
                        case "cmdb.environment":
                            item.Environment = attribute.Value.Attribute.Value.Value2String();
                            break;
                        case "cmdb.status":
                            item.Status = attribute.Value.Attribute.Value.Value2String();
                            break;
                        default:
                            break;
                    }
                }

                ciData.Add(item);
            }

            // add categories for hosts 
            // HostsCategories
            var hostsCategories = await traitModel.FilterCIsWithTrait(allCIsCMDB, Traits.HostsCategoriesFlattened, layersetCMDB, trans, changesetProxy.TimeThreshold);
            //var hostsCategories = await traitModel.GetEffectiveTraitsForTrait(Traits.HostsCategoriesFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            foreach (var ciItem in hostsCategories)
            {
                //(MergedCI ciItem, _) = hostCategory.Value;
                var success = ciItem.MergedAttributes.TryGetValue("cmdb.host_category-HOSTID", out MergedCIAttribute? hostIdAttribute);

                if (!success)
                {
                    // log error here
                }

                var hostId = hostIdAttribute!.Attribute.Value.Value2String();

                foreach (var item in ciData)
                {
                    if (item.Id == hostId)
                    {
                        item.Categories = new Dictionary<string, Category>();

                        var obj = new Category();

                        foreach (var attribute in ciItem.MergedAttributes)
                        {
                            switch (attribute.Key)
                            {
                                case "cmdb.host_category-CATEGORYID":
                                    obj.Id = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.host_category-CATTREE":
                                    obj.Tree = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.host_category-CATGROUP":
                                    obj.Group = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.host_category-CATEGORY":
                                    obj.Name = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.host_category-CATDESC":
                                    obj.Desc = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                default:
                                    break;
                            }
                        }

                        item.Categories[obj.Group] = obj;
                    }
                }
            }

            // add categories for services
            var servicesCategories = await traitModel.FilterCIsWithTrait(allCIsCMDB, Traits.ServicesCategoriesFlattened, layersetCMDB, trans, changesetProxy.TimeThreshold);
            //var servicesCategories = await traitModel.GetEffectiveTraitsForTrait(Traits.ServicesCategoriesFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            foreach (var ciItem in servicesCategories)
            {
                //(MergedCI ciItem, _) = serviceCategory.Value;
                var success = ciItem.MergedAttributes.TryGetValue("cmdb.service_category-SVCID", out MergedCIAttribute? serviceIdAttribute);

                if (!success)
                {
                    // log error here
                }

                var serviceId = serviceIdAttribute!.Attribute.Value.Value2String();

                foreach (var item in ciData)
                {
                    if (item.Id == serviceId)
                    {
                        item.Categories = new Dictionary<string, Category>();

                        var obj = new Category();

                        foreach (var attribute in ciItem.MergedAttributes)
                        {
                            switch (attribute.Key)
                            {
                                case "cmdb.host_category-CATEGORYID":
                                    obj.Id = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.host_category-CATTREE":
                                    obj.Tree = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.host_category-CATGROUP":
                                    obj.Group = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.host_category-CATEGORY":
                                    obj.Name = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.host_category-CATDESC":
                                    obj.Desc = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                default:
                                    break;
                            }
                        }

                        item.Categories[obj.Group] = obj;
                    }
                }
            }

            // add host actions to cidata
            var hostActions = await traitModel.FilterCIsWithTrait(allCIsCMDB, Traits.HostActionsFlattened, layersetCMDB, trans, changesetProxy.TimeThreshold);

            //var hostActions = await traitModel.GetEffectiveTraitsForTrait(Traits.HostActionsFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            foreach (var ciItem in hostActions)
            {
                //(MergedCI ciItem, _) = hostAction.Value;
                var success = ciItem.MergedAttributes.TryGetValue("cmdb.host_action-HOSTID", out MergedCIAttribute? hostIdAttribute);

                var hostId = hostIdAttribute!.Attribute.Value.Value2String();

                foreach (var item in ciData)
                {
                    if (item.Id == hostId)
                    {
                        var obj = new Actions();

                        foreach (var attribute in ciItem.MergedAttributes)
                        {
                            switch (attribute.Key)
                            {
                                case "cmdb.host_action-HOSTACTID":
                                    obj.Id = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.host_action-HOSTACTTYPE":
                                    obj.Type = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.host_action-HOSTACTCMD":
                                    obj.Cmd = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.host_action-HOSTACTCMDUSER":
                                    obj.CmdUser = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                default:
                                    break;
                            }
                        }

                        item.Actions = obj;
                    }
                }
            }

            // add service actions to ci data 
            var serviceActions = await traitModel.FilterCIsWithTrait(allCIsCMDB, Traits.ServiceActionsFlattened, layersetCMDB, trans, changesetProxy.TimeThreshold);

            //var serviceActions = await traitModel.GetEffectiveTraitsForTrait(Traits.ServiceActionsFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            foreach (var ciItem in serviceActions)
            {
                //(MergedCI ciItem, _) = serviceAction.Value;
                var success = ciItem.MergedAttributes.TryGetValue("cmdb.service_action-SVCID", out MergedCIAttribute? serviceIdAttribute);

                var serviceId = serviceIdAttribute!.Attribute.Value.Value2String();

                foreach (var item in ciData)
                {
                    if (item.Id == serviceId)
                    {
                        var obj = new Actions();
                        foreach (var attribute in ciItem.MergedAttributes)
                        {
                            switch (attribute.Key)
                            {
                                case "cmdb.service_action-SVCACTID":
                                    obj.Id = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.service_action-SVCACTTYPE":
                                    obj.Type = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.service_action-SVCACTCMD":
                                    obj.Cmd = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.service_action-SVCACTCMDUSER":
                                    obj.CmdUser = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                default:
                                    break;
                            }
                        }

                        item.Actions = obj;
                    }
                }
            }

            // get capability map
            // then check profiles from db

            //getCapabilityMap - NaemonInstancesTagsFlattened
            var naemonInstancesTags = await traitModel.FilterCIsWithTrait(allCIsMonman, Traits.NaemonInstancesTagsFlattened, layersetCMDB, trans, changesetProxy.TimeThreshold);

            //var naemonInstancesTags = await traitModel.GetEffectiveTraitsForTrait(Traits.NaemonInstancesTagsFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            var capMap = new Dictionary<string, List<string>>();

            foreach (var ciItem in naemonInstancesTags)
            {
                //(MergedCI ciItem, _) = instanceTag.Value;

                var s = ciItem.MergedAttributes.TryGetValue("monman-instance_tag.tag", out MergedCIAttribute? instanceTagAttribute);

                if (!s)
                {
                    continue;
                }

                var tag = instanceTagAttribute!.Attribute.Value.Value2String();

                if (tag.StartsWith("cap_"))
                {
                    var ss = ciItem.MergedAttributes.TryGetValue("monman-instance_tag.id", out MergedCIAttribute? instanceIdAttribute);
                    if (!ss)
                    {
                        continue;
                    }

                    if (capMap.ContainsKey(tag))
                    {
                        capMap[tag].Add(instanceIdAttribute!.Attribute.Value.Value2String());
                    }
                    else
                    {
                        capMap.Add(tag, new List<string> { instanceIdAttribute!.Attribute.Value.Value2String() });
                    }
                }
            }

            var naemonProfiles = await traitModel.FilterCIsWithTrait(allCIsMonman, Traits.NaemonProfilesFlattened, layersetCMDB, trans, changesetProxy.TimeThreshold);

            //var naemonProfiles = await traitModel.GetEffectiveTraitsForTrait(Traits.NaemonProfilesFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            var profileFromDbNaemons = new List<string>();

            foreach (var ciItem in naemonInstances)
            {
                // we need to check here if isNaemonProfileFromDbEnabled 
                //(MergedCI ciItem, _) = naemon.Value;

                var s = ciItem.MergedAttributes.TryGetValue("monman-instance.name", out MergedCIAttribute? instanceNameAttribute);

                if (!s)
                {
                    continue;
                }

                var instanceName = instanceNameAttribute!.Attribute.Value.Value2String();

                if (naemonsConfigGenerateprofiles.Contains(instanceName))
                {
                    //profileFromDbNaemons.Add(ci);

                    // monman-instance.id
                    var ss = ciItem.MergedAttributes.TryGetValue("monman-instance.id", out MergedCIAttribute? instanceIdAttribute);

                    if (!ss)
                    {
                        continue;
                    }

                    profileFromDbNaemons.Add(instanceIdAttribute!.Attribute.Value.Value2String());
                }
            }

            /* extend capMap */

            if (profileFromDbNaemons.Count > 0)
            {
                foreach (var ciItem in naemonProfiles)
                {
                    //(MergedCI ciItem, _) = profile.Value;

                    // first get profile name 
                    var s = ciItem.MergedAttributes.TryGetValue("monman-profile.name", out MergedCIAttribute? profileNameAttribute);

                    if (!s)
                    {
                        continue;
                    }

                    //$cap = 'cap_lp_'.strtolower($profile['NAME']);

                    var cap = "cap_lp_" + profileNameAttribute!.Attribute.Value.Value2String();

                    if (!capMap.ContainsKey(cap))
                    {
                        capMap.Add(cap, new List<string>());
                    }
                    else
                    {
                        capMap[cap] = (List<string>)profileFromDbNaemons.Concat(capMap[cap]);
                    }
                }
            }

            //// create for each CI an json object so we can have more flexibility
            //foreach (var ciItem in cisData)
            //{
            //    // we need to add the naemonsVail to each configuration item
            //    var naemonsVail = naemonIds;

            //    // we need to check here ci tags which should be an attribute
            //    foreach (var instanceTag in naemonInstancesTags)
            //    {
            //        (MergedCI item, _) = instanceTag.Value;

            //        var s = item.MergedAttributes.TryGetValue("monman-instance.name", out MergedCIAttribute? instanceNameAttribute);

            //        if (!s)
            //        {
            //            continue;
            //        }

            //        var requirement = instanceNameAttribute!.Attribute.Value.Value2String();

            //        if (capMap.ContainsKey(requirement))
            //        {
            //            naemonsVail = naemonsVail.Intersect(capMap[requirement]).ToList();
            //        }
            //        else
            //        {
            //            naemonsVail = new List<string>();
            //        }
            //    }
            //}

            foreach (var item in ciData)
            {
                item.NaemonsAvail = naemonIds;

                if (capMap.ContainsKey(item.Name))
                {
                    item.NaemonsAvail = item.NaemonsAvail.Intersect(capMap[item.Name]).ToList();
                }
                else
                {
                    item.NaemonsAvail = new List<string>();
                }
            }

            foreach (var item in naemonInstances)
            {

                var s = item.MergedAttributes.TryGetValue("monman-instance.id", out MergedCIAttribute? instanceId);

                if (!s)
                {
                    // log error here
                }

                var id = instanceId!.Attribute.Value.Value2String();

                // get only configuration items for this naemon
                var thisNaemonCis = new List<dynamic>();

                foreach (var ciItem in ciData)
                {
                    if (ciItem.NaemonsAvail.Contains(id))
                    {
                        thisNaemonCis.Add(ciItem);
                    }
                }
            }

            foreach (var item in ciData)
            {
                if (item.Actions != null)
                {
                    break;
                }

            }

            //var commands = await traitModel.GetEffectiveTraitsWithTraitAttributeValue(Traits.CommandsFlattened, "monman-command.id", new AttributeScalarValueText("H12037680"), layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);
            // load services ci

            // foreach customer from loadcmdbcustomer we need to take data for hosts and services


            // now foreach naemonInstance we neeed to get the configuration items based on id
            // we do this check by comapring if nemonId is in naemonsVail list

            return true;
        }

        internal class ConfigurationItem
        {
            //public ConfigurationItem(string type, string id, string name, string status, Dictionary<string, Category> categories, Actions actions)
            //{
            //    Type = type;
            //    Id = id;
            //    Name = name;
            //    Status = status;
            //    Categories = categories;
            //    Actions = actions;
            //}
            public string Type { get; set; }
            public string Id { get; set; }
            public string Name { get; set; }
            public string Status { get; set; }
            public string Environment { get; set; }
            public List<string> NaemonsAvail { get; set; }
            public Dictionary<string, Category> Categories { get; set; }
            public Actions Actions { get; set; }
        }

        internal class Category
        {
            //public Category(string id, string tree, string group, string name, string desc)
            //{
            //    Id = id;
            //    Tree = tree;
            //    Group = group;
            //    Name = name;
            //    Desc = desc;
            //}

            public string Id { get; set; }
            public string Tree { get; set; }
            public string Group { get; set; }
            public string Name { get; set; }
            public string Desc { get; set; }
        }

        internal class Actions
        {
            //public Actions(string id, string type, string cmd, string cmdUser)
            //{
            //    Id = id;
            //    Type = type;
            //    Cmd = cmd;
            //    CmdUser = cmdUser;
            //}
            public string Id { get; set; }
            public string Type { get; set; }
            public string Cmd { get; set; }
            public string CmdUser { get; set; }
        }
    }
}
