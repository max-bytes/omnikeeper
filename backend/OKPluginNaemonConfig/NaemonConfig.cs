using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        #region configuration
        // TODO: move code in this region into configuration
        private List<string> loadcmdbcustomer = new List<string>() { "ADISSEO", "AGRANA", "AMS", "AMSINT", "ANDRITZ", "ATS", "AVESTRA", "AWS" };

        // this is only temporary since this should be read from configuration
        private List<string> naemonsConfigGenerateprofiles = new List<string>() { "svphg200mon001", "svphg200mon002", "uansvclxnaemp01", "uansvclxnaemp02", "uansvclxnaemp03", "uansvclxnaemp04", "uansvclxnaemp05", "uansvclxnaemp06" };

        //    cmdb-monprofile-prefix:
        //- 'profile-'
        //- 'profiletsc-'

        private List<string> cmdbMonprofilePrefix = new List<string> { "profile-", "profiletsc-" };
        #endregion
        public override async Task<bool> Run(Layer targetLayer, JObject config, IChangesetProxy changesetProxy, CLBErrorHandler errorHandler, IModelContext trans, ILogger logger)
        {
            logger.LogDebug("Start naemonConfig");

            var layersetCMDB = await layerModel.BuildLayerSet(new[] { "cmdb" }, trans);
            var layersetMonman = await layerModel.BuildLayerSet(new[] { "monman" }, trans);
            var layersetLivestatus = await layerModel.BuildLayerSet(new[] { "livestatus" }, trans);
            var layersetNaemonConfig = await layerModel.BuildLayerSet(new[] { "naemon_config" }, trans);

            var allCIsCMDB = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layersetCMDB, false, AllAttributeSelection.Instance, trans, changesetProxy.TimeThreshold);
            var allCIsMonman = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layersetMonman, false, AllAttributeSelection.Instance, trans, changesetProxy.TimeThreshold);
            

            // load naemonInstances
            var naemonInstances = await traitModel.FilterCIsWithTrait(allCIsMonman, Traits.NaemonInstanceFlattened, layersetMonman, trans, changesetProxy.TimeThreshold);
            var naemonIds = new List<string>();
            foreach (var ciItem in naemonInstances)
            {
                //(MergedCI ciItem, _) = naemon.Value;
                var success = ciItem.MergedAttributes.TryGetValue("naemon_instance.id", out MergedCIAttribute? attributeNaemonId);

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

            // NOTE mcsuk: this part is cumbersome because of the way the data is set up; it would by much cleaner if there was a proper relation between the host and its categories
            // in the original CMDB, there is a relation like that, so I believe we should also add a proper relation in omnikeeper
            // if we have that, we can make use of the relations and find links between categories and hosts through that instead of having to read the cmdb.host_category_hostid 
            // and doing a search in the hosts
            foreach (var ciItem in hostsCategories)
            {
                var success = ciItem.MergedAttributes.TryGetValue("cmdb.host_category_hostid", out MergedCIAttribute? hostIdAttribute);

                if (!success)
                {
                    // log error here
                }

                var hostId = hostIdAttribute!.Attribute.Value.Value2String();

                foreach (var item in ciData)
                {
                    if (item.Id == hostId)
                    {
                        var obj = new Category();

                        foreach (var attribute in ciItem.MergedAttributes)
                        {
                            switch (attribute.Key)
                            {
                                case "cmdb.host_category_categoryid":
                                    obj.Id = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.host_category_cattree":
                                    obj.Tree = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.host_category_catgroup":
                                    obj.Group = attribute.Value.Attribute.Value.Value2String();
                                    if (obj.Group == "MONITORING")
                                    {
                                        var a = 5;
                                    }
                                    break;
                                case "cmdb.host_category_category":
                                    obj.Name = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.host_category_catdesc":
                                    obj.Desc = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                default:
                                    break;
                            }
                        }

                        if (!item.Categories.ContainsKey(obj.Group))
                        {
                            item.Categories.Add(obj.Group, new List<Category> { obj });
                        } else
                        {
                            item.Categories[obj.Group].Add(obj);
                        }

                        break;
                    }
                }
            }

            var hh = ciData.Where(el => el.Id == "H02047416");


            // add categories for services
            var servicesCategories = await traitModel.FilterCIsWithTrait(allCIsCMDB, Traits.ServicesCategoriesFlattened, layersetCMDB, trans, changesetProxy.TimeThreshold);

            // NOTE mcsuk: the same as above for hosts+categories goes here for services+categories
            foreach (var ciItem in servicesCategories)
            {
                var success = ciItem.MergedAttributes.TryGetValue("cmdb.service_category_svcid", out MergedCIAttribute? serviceIdAttribute);

                if (!success)
                {
                    // log error here
                }

                var serviceId = serviceIdAttribute!.Attribute.Value.Value2String();

                foreach (var item in ciData)
                {
                    if (item.Id == serviceId)
                    {
                        var obj = new Category();

                        foreach (var attribute in ciItem.MergedAttributes)
                        {
                            switch (attribute.Key)
                            {
                                case "cmdb.service_category_categoryid":
                                    obj.Id = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.service_category_cattree":
                                    obj.Tree = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.service_category_catgroup":
                                    obj.Group = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.service_category_category":
                                    obj.Name = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.service_category_catdesc":
                                    obj.Desc = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                default:
                                    break;
                            }
                        }

                        if (!item.Categories.ContainsKey(obj.Group))
                        {
                            item.Categories.Add(obj.Group, new List<Category> { obj });
                        }
                        else
                        {
                            item.Categories[obj.Group].Add(obj);
                        }

                        break;
                    }
                }
            }

            // add host actions to cidata
            var hostActions = await traitModel.FilterCIsWithTrait(allCIsCMDB, Traits.HostActionsFlattened, layersetCMDB, trans, changesetProxy.TimeThreshold);

            // NOTE mcsuk: the same as above for hosts+categories goes here
            foreach (var ciItem in hostActions)
            {
                var success = ciItem.MergedAttributes.TryGetValue("cmdb.host_action_hostid", out MergedCIAttribute? hostIdAttribute);

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
                                case "cmdb.host_action_id":
                                    obj.Id = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.host_action_type":
                                    obj.Type = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.host_action_cmd":
                                    obj.Cmd = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.host_action_cmduser":
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

            // NOTE mcsuk: the same as above for hosts+categories goes here
            foreach (var ciItem in serviceActions)
            {
                var success = ciItem.MergedAttributes.TryGetValue("cmdb.service_action_svcid", out MergedCIAttribute? serviceIdAttribute);

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
                                case "cmdb.service_action_id":
                                    obj.Id = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.service_action_type":
                                    obj.Type = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.service_action_cmd":
                                    obj.Cmd = attribute.Value.Attribute.Value.Value2String();
                                    break;
                                case "cmdb.service_action_cmduser":
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

            // add normalized ci data from interfaces

            var interfaces = await traitModel.FilterCIsWithTrait(allCIsCMDB, Traits.InterfacesFlattened, layersetCMDB, trans, changesetProxy.TimeThreshold);

            foreach (var ciItem in interfaces)
            {
                /*
                         'ID' => $interface[$fieldPrefix.'IFID'],
            'TYPE' => $interface[$fieldPrefix.'IFTYPE'],
            'LANTYPE' => $interface[$fieldPrefix.'IFLANTYPE'],
            'NAME' => $interface[$fieldPrefix.'IFNAME'],
            'IP' => $interface[$fieldPrefix.'IFIP'],
//                        'IPVERSION' => $interface['IFIPVERSION'],
//                        'GATEWAY' => $interface['IFGATEWAY'],
            'DNSNAME' => $interface[$fieldPrefix.'IFDNS'],
            'VLAN' => $interface[$fieldPrefix.'IFVLAN'],
 */
                //var obj = new Interfaces();
                //foreach (var attribute in collection)
                //{

                //}
            }
            
            //getCapabilityMap - NaemonInstancesTagsFlattened
            var naemonInstancesTags = await traitModel.FilterCIsWithTrait(allCIsMonman, Traits.NaemonInstancesTagsFlattened, layersetCMDB, trans, changesetProxy.TimeThreshold);

            var capMap = new Dictionary<string, List<string>>();

            foreach (var ciItem in naemonInstancesTags)
            {
                var s = ciItem.MergedAttributes.TryGetValue("naemon_instance_tag.tag", out MergedCIAttribute? instanceTagAttribute);

                if (!s)
                {
                    continue;
                }

                var tag = instanceTagAttribute!.Attribute.Value.Value2String();

                if (tag.StartsWith("cap_"))
                {
                    var ss = ciItem.MergedAttributes.TryGetValue("naemon_instance_tag.id", out MergedCIAttribute? instanceIdAttribute);
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

            var profileFromDbNaemons = new List<string>();

            foreach (var ciItem in naemonInstances)
            {
                // we need to check here if isNaemonProfileFromDbEnabled 
                var s = ciItem.MergedAttributes.TryGetValue("naemon_instance.name", out MergedCIAttribute? instanceNameAttribute);

                if (!s)
                {
                    continue;
                }

                var instanceName = instanceNameAttribute!.Attribute.Value.Value2String();

                if (naemonsConfigGenerateprofiles.Contains(instanceName))
                {
                    // monman-instance.id
                    var ss = ciItem.MergedAttributes.TryGetValue("naemon_instance.id", out MergedCIAttribute? instanceIdAttribute);

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
                    var s = ciItem.MergedAttributes.TryGetValue("naemon_profile.name", out MergedCIAttribute? profileNameAttribute);

                    if (!s)
                    {
                        continue;
                    }

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

                var s = item.MergedAttributes.TryGetValue("naemon_instance.id", out MergedCIAttribute? instanceId);

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

            //foreach (var item in ciData)
            //{
            //    if (item.Actions != null)
            //    {
            //        break;
            //    }

            //}

            #region process core data
            // updateNormalizedCiDataFieldProfile
            foreach (var ciItem in ciData)
            {
                var profileCount = 0;
                ciItem.Profile = "NONE";
                ciItem.ProfileOrg = new List<string>();

                if (ciItem.Categories.ContainsKey("MONITORING"))
                {
                    foreach (var category in ciItem.Categories["MONITORING"])
                    {
                        // check profile against configured scoping pattern
                        var isMyProfileScope = false;
                        foreach (var pattern in cmdbMonprofilePrefix)
                        {
                            if (Regex.IsMatch(category.Name, $"/^${pattern}/i"))
                            {
                                isMyProfileScope = true;
                                break;
                            }
                        }

                        if (isMyProfileScope)
                        {
                            profileCount += 1;
                            ciItem.Profile = category.Name.ToLower();
                            ciItem.ProfileOrg.Add(category.Name.ToLower());
                        }
                    }
                }

                if (profileCount > 1)
                {
                    ciItem.Profile = "MULTIPLE";
                }

                if (profileCount == 1 && Regex.IsMatch(ciItem.Profile, "/^profile/i"))
                {
                    // add legacy profile capability
                    ciItem.Tags.Add("cap_lp_" + ciItem.Profile);
                }

            }

            // updateNormalizedCiDataFieldAddress
            foreach (var ciItem in ciData)
            {
                if (ciItem.Type == "HOST")
                {

                }
            }

            // updateNormalizedCiData_addGenericCmdbCapTags

            foreach (var ciItem in ciData)
            {
                if (ciItem.Categories.ContainsKey("MONITORING_CAP"))
                {
                    foreach (var category in ciItem.Categories["MONITORING_CAP"])
                    {
                        ciItem.Tags.Add($"cap_{category.Tree}_{category.Name}".ToLower());
                    }
                } else
                {
                    // TODO check if we should add this
                    //ciItem.Tags.Add("cap_default");
                }
            }

            #endregion

            var configObjs = new List<ConfigObj>();

            // apply global filter
            // include only cis that have STATUS = to "ACTIVE" || "BASE_INSTALLED" || "READY_FOR_SERVICE"
            // TODO: this should be configurable
            ciData = ciData.Where(el => el.Status == "ACTIVE" || el.Status == "BASE_INSTALLED" || el.Status == "READY_FOR_SERVICE").ToList();

            // getNaemonConfigObjectsFromStaticTemplates - global-commands
            configObjs.Add(new ConfigObj
            {
                Type = "command",
                Attributes = new Dictionary<string, string>
                {
                    ["command_name"] = "check-nrpe",
                    ["command_line"] = "$USER1$/check_nrpe -2 -t 50 -H $HOSTADDRESS$ -K /opt2/nrpe-ssl/auth.key -C /opt2/nrpe-ssl/auth.crt $ARG1$",
                }
            });

            #region getNaemonConfigObjectsFromTimeperiods
            var timeperiods = await traitModel.FilterCIsWithTrait(allCIsMonman, Traits.TimePeriodsFlattened, layersetMonman, trans, changesetProxy.TimeThreshold);
            foreach (var ciItem in timeperiods)
            {
                var timeperiodId = ciItem.MergedAttributes["naemon_timeperiod.id"]!.Attribute.Value.Value2String();

                var obj = new ConfigObj
                {
                    Type = "timeperiod",
                    Attributes = new Dictionary<string, string>(),
                };

                foreach (var attribute in ciItem.MergedAttributes)
                {
                    switch (attribute.Key)
                    {
                        case "naemon_timeperiod.name":
                            obj.Attributes["timeperiod_name"] = attribute.Value.Attribute.Value.Value2String();
                            break;
                        case "naemon_timeperiod.alias":
                            obj.Attributes["alias"] = attribute.Value.Attribute.Value.Value2String();
                            break;
                        case "naemon_timeperiod.span_mon":
                            var monday = attribute.Value.Attribute.Value.Value2String();
                            if (monday != null && monday.Length > 0)
                            {
                                obj.Attributes["monday"] = monday;
                            }
                            break;
                        case "naemon_timeperiod.span_tue":
                            var tuesday = attribute.Value.Attribute.Value.Value2String();
                            if (tuesday != null && tuesday.Length > 0)
                            {
                                obj.Attributes["tuesday"] = tuesday;
                            }
                            break;
                        case "naemon_timeperiod.span_wed":
                            var wednesday = attribute.Value.Attribute.Value.Value2String();
                            if (wednesday != null && wednesday.Length > 0)
                            {
                                obj.Attributes["wednesday"] = wednesday;
                            }
                            break;
                        case "naemon_timeperiod.span_thu":
                            obj.Attributes["thursday"] = attribute.Value.Attribute.Value.Value2String();
                            var thursday = attribute.Value.Attribute.Value.Value2String();
                            if (thursday != null && thursday.Length > 0)
                            {
                                obj.Attributes["thursday"] = thursday;
                            }
                            break;
                        case "naemon_timeperiod.span_fri":
                            var friday = attribute.Value.Attribute.Value.Value2String();
                            if (friday != null && friday.Length > 0)
                            {
                                obj.Attributes["friday"] = friday;
                            }
                            break;
                        case "naemon_timeperiod.span_sat":
                            var saturday = attribute.Value.Attribute.Value.Value2String();
                            if (saturday != null && saturday.Length > 0)
                            {
                                obj.Attributes["saturday"] = saturday;
                            }
                            break;
                        case "naemon_timeperiod.span_sun":
                            var sunday = attribute.Value.Attribute.Value.Value2String();
                            if (sunday != null && sunday.Length > 0)
                            {
                                obj.Attributes["sunday"] = sunday;
                            }
                            break;
                        default:
                            break;
                    }
                }

                configObjs.Add(obj);
            }
            #endregion


            #region getNaemonConfigObjectsFromNormalizedCiData
            var deployedCis = new List<ConfigurationItem>();

            foreach (var ciItem in ciData)
            {
                //if (Regex.IsMatch("", ciItem.))
                //{

                //}
            }

            #endregion




            // get configuration from legacy objects

            // getNaemonConfigObjectsFromLegacyProfiles_globalVars

            var variables = await traitModel.FilterCIsWithTrait(allCIsMonman, Traits.VariablesFlattened, layersetMonman, trans, changesetProxy.TimeThreshold);

            foreach (var ciItem in variables)
            {
                ciItem.MergedAttributes.TryGetValue("naemon_variable.reftype", out MergedCIAttribute? refTypeAttribute);

                var refType = refTypeAttribute!.Attribute.Value.Value2String();

                // select only variables that have reftype GLOBAL
                if (refType != "GLOBAL")
                {
                    continue;
                }

                var attributes = new Dictionary<string, string>
                {
                    ["name"] = "global-variables",
                    ["_ALERTS"] = "OFF",
                    ["_NRPEPORT"] = "5666"
                };

                foreach (var attribute in ciItem.MergedAttributes)
                {
                    switch (attribute.Key)
                    {
                        case "naemon_variable.id":
                            attributes["_ID"] = attribute.Value.Attribute.Value.Value2String();
                            break;
                        case "naemon_variable.type":
                            attributes["_TYPE"] = attribute.Value.Attribute.Value.Value2String();
                            break;
                        case "naemon_variable.name":
                            attributes["_NAME"] = attribute.Value.Attribute.Value.Value2String();
                            break;
                        case "naemon_variable.value":
                            attributes["_VALUE"] = attribute.Value.Attribute.Value.Value2String();
                            break;
                        case "naemon_variable.issecret":
                            attributes["_ISSECRET"] = attribute.Value.Attribute.Value.Value2String();
                            break;
                        case "naemon_variable.reftype":
                            attributes["_REFTYPE"] = attribute.Value.Attribute.Value.Value2String();
                            break;
                        case "naemon_variable.refid":
                            attributes["_REFID"] = attribute.Value.Attribute.Value.Value2String();
                            break;
                        default:
                            break;
                    }
                }

                configObjs.Add(new ConfigObj
                {
                    Type = "host",
                    Attributes = attributes,
                });

            }

            // getNaemonConfigObjectsFromLegacyProfiles_profiles

            var appendBasetemplates = new List<string> { "global-variables", "tsa-generic-host" };





            // getNaemonConfigObjectsFromLegacyProfiles_modules

            var serviceLayers = await traitModel.FilterCIsWithTrait(allCIsMonman, Traits.ServiceLayersFlattened, layersetMonman, trans, changesetProxy.TimeThreshold);
            var layersById = new Dictionary<string, MergedCI>();
            foreach (var ciItem in serviceLayers)
            {
                var layerId = ciItem.MergedAttributes["naemon_service_layer.id"]!.Attribute.Value.Value2String();

                layersById.Add(layerId, ciItem);
            }


            // getCommands -> We need only comand ids here

            var commands = await traitModel.FilterCIsWithTrait(allCIsMonman, Traits.CommandsFlattened, layersetMonman, trans, changesetProxy.TimeThreshold);
            var commandsById = new Dictionary<string, MergedCI>();
            foreach (var ciItem in commands)
            {
                var commandId = ciItem.MergedAttributes["naemon_command.id"]!.Attribute.Value.Value2String();

                commandsById.Add(commandId, ciItem);
            }

            // getTimeperiods -> We need a list with timeperiod ids

            var timeperiodsById = new Dictionary<string, MergedCI>();
            foreach (var ciItem in timeperiods)
            {
                var timeperiodId = ciItem.MergedAttributes["naemon_timeperiod.id"]!.Attribute.Value.Value2String();

                timeperiodsById.Add(timeperiodId, ciItem);
            }

            // prepare services
            // get SERVICES_STATIC

            var servicesStatic = await traitModel.FilterCIsWithTrait(allCIsMonman, Traits.ServicesStaticFlattened, layersetMonman, trans, changesetProxy.TimeThreshold);
            var servicesStaticById = new Dictionary<string, MergedCI>();
            var servicesByModulesId = new Dictionary<string, List<MergedCI>>();
            foreach (var ciItem in servicesStatic)
            {
                var id = ciItem.MergedAttributes["naemon_services_static.id"]!.Attribute.Value.Value2String();

                servicesStaticById.Add(id, ciItem);

                var module = ciItem.MergedAttributes["naemon_services_static.module"]!.Attribute.Value.Value2String();

                if (servicesByModulesId.ContainsKey(module))
                {
                    servicesByModulesId[module].Add(ciItem);
                }
                else
                {
                    servicesByModulesId[module] = new List<MergedCI> { ciItem };
                }
            }

            var modules = await traitModel.FilterCIsWithTrait(allCIsMonman, Traits.NaemonModulesFlattened, layersetMonman, trans, changesetProxy.TimeThreshold);

            foreach (var ciItem in modules)
            {
                var moduleName = ciItem.MergedAttributes["naemon_module.name"]!.Attribute.Value.Value2String().ToLower();
                var hostObj = new ConfigObj
                {
                    Type = "host",
                    Attributes = new Dictionary<string, string>
                    {
                        ["name"] = "mod-" + moduleName,
                        ["hostgroups"] = "+" + moduleName,
                    },
                };

                var hostGroupObj = new ConfigObj
                {
                    Type = "hostgroup",
                    Attributes = new Dictionary<string, string>
                    {
                        ["hostgroup_name"] = moduleName,
                        ["alias"] = moduleName,
                    },
                };

                var moduleId = ciItem.MergedAttributes["naemon_module.id"]!.Attribute.Value.Value2String().ToLower();

                if (servicesByModulesId.ContainsKey(moduleId))
                {
                    foreach (var service in servicesByModulesId[moduleId])
                    {
                        // goto next if service is disabled
                        var disabled = (service.MergedAttributes["naemon_services_static.disabled"].Attribute.Value as AttributeScalarValueInteger)?.Value;
                        if (disabled > 0)
                        {
                            continue;
                        }

                        var attributes = new Dictionary<string, string>();
                        var vars = new Dictionary<string, string>();

                        var serviceTemplates = new List<string> { "tsa-generic-service" };

                        var perf = (service.MergedAttributes["naemon_services_static.perf"].Attribute.Value as AttributeScalarValueInteger)?.Value;

                        if (perf == 1)
                        {
                            serviceTemplates.Add("service-pnp");
                        }
  
                        attributes["use"] = string.Join(",", serviceTemplates);
                        attributes["hostgroup_name"] = moduleName;

                        // $layersById[$service['LAYER']]['NUM'] . ' ' . $service['SERVICENAME'];

                        var svcLayer = (service.MergedAttributes["naemon_services_static.layer"].Attribute.Value as AttributeScalarValueInteger)?.Value;
                        var layerNum = (layersById[svcLayer.ToString()].MergedAttributes["naemon_service_layer.num"].Attribute.Value as AttributeScalarValueInteger)?.Value;
                        var svcName = (service.MergedAttributes["naemon_services_static.servicename"].Attribute.Value as AttributeScalarValueText)?.Value;
                        attributes["service_description"] = layerNum + " " + svcName;

                        var svcCheckCommand = (service.MergedAttributes["naemon_services_static.checkcommand"].Attribute.Value as AttributeScalarValueInteger)?.Value;
                        var commandName = (commandsById[svcCheckCommand.ToString()].MergedAttributes["naemon_command.name"].Attribute.Value as AttributeScalarValueText)?.Value;
                        var commandParts = new List<string>{ commandName };

                        for (int i = 1; i <= 8; i++)
                        {
                            var k = $"naemon_services_static.arg{i}";

                            if (service.MergedAttributes.TryGetValue(k, out MergedCIAttribute? attr))
                            {
                                var arg = (attr.Attribute.Value as AttributeScalarValueText)?.Value;

                                if (arg.Length > 0)
                                {
                                    if (arg.Substring(0,1) == "$")
                                    {
                                        arg = "$HOST" + arg.Substring(1, arg.Length - 1) + "$";
                                    }

                                    commandParts.Add(arg);
                                }
                            }
                        }

                        attributes["check_command"] = string.Join("!", commandParts);

                        /* add other attributes */

                        var freshnesThreshold = GetAttributeValueInt("naemon_services_static.freshness_threshold", service.MergedAttributes);

                        if (freshnesThreshold != null)
                        {
                            attributes["check_freshness"] = "1";
                            attributes["freshness_threshold"] = freshnesThreshold.ToString();
                        }
                        

                        var passive = GetAttributeValueInt("naemon_services_static.passive", service.MergedAttributes);
                        if (passive != null && passive == 1)
                        {
                            attributes["passive_checks_enabled"] = "1";
                            attributes["active_checks_enabled"] = "0";
                        }

                        var checkInterval = GetAttributeValueInt("naemon_services_static.check_interval", service.MergedAttributes);
                        if (checkInterval != null && checkInterval != 0)
                        {
                            attributes["active_checks_enabled"] = checkInterval.ToString();
                        }

                        var maxCheckAttempts = GetAttributeValueInt("naemon_services_static.max_check_attempts", service.MergedAttributes);
                        if (maxCheckAttempts != null && maxCheckAttempts != 0)
                        {
                            attributes["max_check_attempts"] = maxCheckAttempts.ToString();
                        }

                        var retryInterval = GetAttributeValueInt("naemon_services_static.retry_interval", service.MergedAttributes);
                        if (retryInterval != null && retryInterval != 0)
                        {
                            attributes["retry_interval"] = retryInterval.ToString();
                        }

                        var timeperiodCheck = GetAttributeValueInt("naemon_services_static.timeperiod_check", service.MergedAttributes);
                        if (timeperiodCheck != null && timeperiodsById.ContainsKey(timeperiodCheck.ToString()))
                        {
                            var tName = (timeperiodsById[timeperiodCheck.ToString()].MergedAttributes["naemon_timeperiod.name"].Attribute.Value as AttributeScalarValueText)?.Value;

                            attributes["check_period"] = tName;
                        }

                        var timeperiodNotify = GetAttributeValueInt("naemon_services_static.timeperiod_notify", service.MergedAttributes);
                        if (timeperiodNotify != null && timeperiodsById.ContainsKey(timeperiodNotify.ToString()))
                        {
                            var tName = (timeperiodsById[timeperiodNotify.ToString()].MergedAttributes["naemon_timeperiod.name"].Attribute.Value as AttributeScalarValueText)?.Value;

                            attributes["notification_period"] = tName;
                        }

                        /* fill variables */
                        var target = (service.MergedAttributes["naemon_services_static.target"].Attribute.Value as AttributeScalarValueText)?.Value;
                        vars["Tickettarget"] = target;

                        var checkPrio = (service.MergedAttributes["naemon_services_static.checkprio"].Attribute.Value as AttributeScalarValueText)?.Value;
                        vars["Fehlerart"] = checkPrio;

                        /* customer cockpit vars */
                        var visibility = GetAttributeValueInt("naemon_services_static.monview_visibility", service.MergedAttributes);
                        if (visibility != null)
                        {
                            vars["Visibility"] = visibility.ToString();
                        }

                        var kpiType = GetAttributeValueInt("naemon_services_static.kpi_avail", service.MergedAttributes);
                        if (kpiType != null)
                        {
                            vars["kpi_type"] = kpiType.ToString();
                        }
                        
                        var metricName = (service.MergedAttributes["naemon_services_static.metric_name"].Attribute.Value as AttributeScalarValueText)?.Value;
                        vars["metric_name"] = metricName;

                        var metricType = (service.MergedAttributes["naemon_services_static.metric_type"].Attribute.Value as AttributeScalarValueText)?.Value;
                        vars["metric_type"] = metricType;

                        var metricMin = GetAttributeValueInt("naemon_services_static.metric_min", service.MergedAttributes);
                        if (metricMin != null)
                        {
                            vars["metric_min"] = metricMin.ToString();
                        }

                        var metricMax = GetAttributeValueInt("naemon_services_static.metric_max", service.MergedAttributes);
                        if (metricMax != null)
                        {
                            vars["metric_max"] = metricMax.ToString();

                        }

                        var metricPerfkey = (service.MergedAttributes["naemon_services_static.metric_perfkey"].Attribute.Value as AttributeScalarValueText)?.Value;
                        vars["metric_perfkey"] = metricPerfkey;

                        var metricPerfunit = (service.MergedAttributes["naemon_services_static.metric_perfunit"].Attribute.Value as AttributeScalarValueText)?.Value;
                        vars["metric_perfunit"] = metricPerfunit;

                        var monviewWarn = (service.MergedAttributes["naemon_services_static.monview_warn"].Attribute.Value as AttributeScalarValueText)?.Value;
                        vars["monview_warn"] = monviewWarn;

                        var monviewCrit = (service.MergedAttributes["naemon_services_static.monview_crit"].Attribute.Value as AttributeScalarValueText)?.Value;
                        vars["monview_crit"] = monviewCrit;

                        /* add variables to attributes */
                        foreach (var var in vars)
                        {
                            var (key, value) = var;
                            var k = "_" + key.ToUpper();
                            attributes[k] = value ?? "";
                        }

                        /* push service to config */
                        configObjs.Add(new ConfigObj { Type = "service", Attributes = attributes });

                        /* add servicedependency objects */
                        var mastersvcCheck = GetAttributeValueInt("naemon_services_static.mastersvc_check", service.MergedAttributes);
                        if (mastersvcCheck != null && servicesStaticById.ContainsKey(mastersvcCheck.ToString()))
                        {
                            var masterService = servicesStaticById[mastersvcCheck.ToString()];
                            var masterSvcName = (masterService.MergedAttributes["naemon_services_static.servicename"].Attribute.Value as AttributeScalarValueText)?.Value;

                            configObjs.Add(new ConfigObj
                            {
                                Type = "servicedependency",
                                Attributes = new Dictionary<string, string>
                                {
                                    ["hostgroup_name"] = moduleName,
                                    ["service_description"] = layerNum + " " + masterSvcName,
                                    ["dependent_service_description"] = layerNum + " " + svcName,
                                    ["inherits_parent"] = "1",
                                    ["execution_failure_criteria"] = "w,u,c",
                                }
                            });
                        }

                        var mastersvcNotify = GetAttributeValueInt("naemon_services_static.mastersvc_notify", service.MergedAttributes);
                        if (mastersvcNotify != null && servicesStaticById.ContainsKey(mastersvcNotify.ToString()))
                        {
                            var masterService = servicesStaticById[mastersvcNotify.ToString()];
                            var masterSvcName = (masterService.MergedAttributes["naemon_services_static.servicename"].Attribute.Value as AttributeScalarValueText)?.Value;

                            configObjs.Add(new ConfigObj
                            {
                                Type = "servicedependency",
                                Attributes = new Dictionary<string, string>
                                {
                                    ["hostgroup_name"] = moduleName,
                                    ["service_description"] = layerNum + " " + masterSvcName,
                                    ["dependent_service_description"] = layerNum + " " + svcName,
                                    ["inherits_parent"] = "1",
                                    ["execution_failure_criteria"] = "w,u,c",
                                }
                            });
                        }
                    }
                }



            }


            // getNaemonConfigObjectsFromLegacyProfiles_commands
            foreach (var command in commands)
            {
                var commandName = (command.MergedAttributes["naemon_command.name"].Attribute.Value as AttributeScalarValueText)?.Value;
                var commandLine = (command.MergedAttributes["naemon_command.exec"].Attribute.Value as AttributeScalarValueText)?.Value;

                configObjs.Add(new ConfigObj
                {
                    Type = "command",
                    Attributes = new Dictionary<string, string>
                    {
                        ["command_name"] = commandName,
                        ["command_line"] = commandLine,
                    },
                });
            }




            //TODO getNaemonConfigObjectsFromLegacyProfiles_hostcommands
            //

            

            
            return true;
        }

        private string? GetAttributeValueString(string key, IDictionary<string, MergedCIAttribute> attributes)
        {
            var success = attributes.TryGetValue(key, out MergedCIAttribute? attributeValue);

            if (!success)
            {
                return null;
            }

            return (attributeValue.Attribute.Value as AttributeScalarValueText).Value;
        }

        private long? GetAttributeValueInt(string key, IDictionary<string, MergedCIAttribute> attributes)
        {
            var success = attributes.TryGetValue(key, out MergedCIAttribute? attributeValue);

            if (!success)
            {
                return null;
            }

            return (attributeValue.Attribute.Value as AttributeScalarValueInteger).Value;
        }
        // TODO: temporary class, maybe we need to remove this in later iteration

        internal class ConfigObj
        {
            public string Type { get; set; }
            public Dictionary<string, string> Attributes { get; set; }
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

            public ConfigurationItem()
            {
                Categories = new Dictionary<string, List<Category>>();
                Tags = new List<string>();
                ProfileOrg = new List<string>();
                NaemonsAvail = new List<string>();
            }
            public string Type { get; set; }
            public string Id { get; set; }
            public string Name { get; set; }
            public string Status { get; set; }
            public string Environment { get; set; }
            public string Profile { get; set; }
            public string Address { get; set; }
            public List<string> ProfileOrg { get; set; }
            public List<string> NaemonsAvail { get; set; }
            public Dictionary<string, List<Category>> Categories { get; set; }
            public List<string> Tags { get; set; }
            public Actions Actions { get; set; }
            public Interfaces Interfaces { get; set; }
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

        internal class Interfaces
        {
            public int Id { get; set; }
            public int Type { get; set; }
            public int LANType { get; set; }
            public int Name { get; set; }
            public int IP { get; set; }
            public int DSNName { get; set; }
            public int Vlan { get; set; }
        }
    }
}
