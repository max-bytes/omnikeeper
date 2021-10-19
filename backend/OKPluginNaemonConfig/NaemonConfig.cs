﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OKPluginNaemonConfig.Entity;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
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
        private readonly GenericTraitEntityModel<NaemonInstance, string> naemonInstanceModel;
        private readonly GenericTraitEntityModel<Host, string> hostModel;
        private readonly GenericTraitEntityModel<Service, string> serviceModel;
        private readonly GenericTraitEntityModel<HostsCategory, string> hostsCategoryModel;
        private readonly GenericTraitEntityModel<ServicesCategory, string> servicesCategoryModel;
        private readonly GenericTraitEntityModel<HostAction, string> hostActionModel;
        private readonly GenericTraitEntityModel<ServiceAction, string> serviceActionModel;
        private readonly GenericTraitEntityModel<NaemonInstancesTag, string> naemonInstancesTagModel;
        private readonly GenericTraitEntityModel<NaemonProfile, string> naemonProfileModel;
        public NaemonConfig(ICIModel ciModel, IAttributeModel atributeModel, ILayerModel layerModel, IEffectiveTraitModel traitModel, IRelationModel relationModel,
                           IChangesetModel changesetModel, IUserInDatabaseModel userModel, 
                           GenericTraitEntityModel<NaemonInstance, string> naemonInstanceModel,
                           GenericTraitEntityModel<Service, string> serviceModel,
                           GenericTraitEntityModel<HostsCategory, string> hostsCategoryModel,
                           GenericTraitEntityModel<ServicesCategory, string> servicesCategoryModel,
                           GenericTraitEntityModel<HostAction, string> hostActionModel,
                           GenericTraitEntityModel<ServiceAction, string> serviceActionModel,
                           GenericTraitEntityModel<NaemonInstancesTag, string> naemonInstancesTagModel,
                           GenericTraitEntityModel<NaemonProfile, string> naemonProfileModel,
                           GenericTraitEntityModel<Host, string> hostModel)
            : base(atributeModel, layerModel, changesetModel, userModel)
        {
            this.ciModel = ciModel;
            this.relationModel = relationModel;
            this.traitModel = traitModel;
            this.naemonInstanceModel = naemonInstanceModel;
            this.hostModel = hostModel;
            this.serviceModel = serviceModel;
            this.hostsCategoryModel = hostsCategoryModel;
            this.servicesCategoryModel = servicesCategoryModel;
            this.hostActionModel = hostActionModel;
            this.serviceActionModel = serviceActionModel;
            this.naemonInstancesTagModel = naemonInstancesTagModel;
            this.naemonProfileModel = naemonProfileModel;
        }

        public override async Task<bool> Run(Layer targetLayer, JObject config, IChangesetProxy changesetProxy, CLBErrorHandler errorHandler, IModelContext trans, ILogger logger)
        {
            logger.LogDebug("Start naemonConfig");

            var cfg = new Configuration();

            try
            {
                cfg = config.ToObject<Configuration>();
            }
            catch (System.Exception)
            {
                //TODO throw an error here
                throw;
            }

            var layersetCMDB = await layerModel.BuildLayerSet(new[] { cfg!.CMDBLayerId }, trans);
            var layersetMonman = await layerModel.BuildLayerSet(new[] { cfg!.MonmanLayerId }, trans);
            var layersetNaemonConfig = await layerModel.BuildLayerSet(new[] { cfg!.NaemonConfigLayerId }, trans);

            var allCIsCMDB = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layersetCMDB, false, AllAttributeSelection.Instance, trans, changesetProxy.TimeThreshold);
            var allCIsMonman = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layersetMonman, false, AllAttributeSelection.Instance, trans, changesetProxy.TimeThreshold);


            // load all naemons



            // load naemonInstances
            var naemonInstances = await traitModel.FilterCIsWithTrait(allCIsMonman, Traits.NaemonInstanceFlattened, layersetMonman, trans, changesetProxy.TimeThreshold);
            
            var nInstances = await naemonInstanceModel.GetAllByDataID(layersetMonman, trans, changesetProxy.TimeThreshold);
            var naemonIds = nInstances.Select(el => el.Value.Id).ToList();


            // a list with all CI from database
            var ciData = new List<ConfigurationItem>();

            var hosts = await hostModel.GetAllByDataID(layersetCMDB, trans, changesetProxy.TimeThreshold);

            foreach (var ciItem in hosts)
            {
                ciData.Add(new ConfigurationItem
                {
                    Type = "HOST",
                    Id = ciItem.Value.Id,
                    Name = ciItem.Value.Name,
                    Status = ciItem.Value.Status,
                });
            }

            // get services
            var services = await serviceModel.GetAllByDataID(layersetCMDB, trans, changesetProxy.TimeThreshold);

            foreach (var ciItem in services)
            {
                ciData.Add(new ConfigurationItem
                {
                    Type = "SERVICE",
                    Id = ciItem.Value.Id,
                    Name = ciItem.Value.Name,
                    Status = ciItem.Value.Status,
                    Environment = ciItem.Value.Environment,
                });
            }

            #region add categories
            // add categories for hosts 
            var hostsCategories = await hostsCategoryModel.GetAllByDataID(layersetCMDB, trans, changesetProxy.TimeThreshold);
            //NOTE mcsuk: this part is cumbersome because of the way the data is set up; it would by much cleaner if there was a proper relation between the host and its categories
            // in the original CMDB, there is a relation like that, so I believe we should also add a proper relation in omnikeeper
            // if we have that, we can make use of the relations and find links between categories and hosts through that instead of having to read the cmdb.host_category_hostid
            // and doing a search in the hosts
            foreach (var ciItem in hostsCategories)
            {
                ciData.ForEach(el =>
                {
                    if (el.Id == ciItem.Value.HostId)
                    {
                        var obj = new Category
                        {
                            Id = ciItem.Value.Id,
                            Tree = ciItem.Value.CatTree,
                            Group = ciItem.Value.CatGroup,
                            Name = ciItem.Value.Category,
                            Desc = ciItem.Value.CatDesc,
                        };

                        if (!el.Categories.ContainsKey(obj.Group))
                        {
                            el.Categories.Add(obj.Group, new List<Category> { obj });
                        }
                        else
                        {
                            el.Categories[obj.Group].Add(obj);
                        }
                    }
                });
            }

            // add categories for services
            var servicesCategories = await servicesCategoryModel.GetAllByDataID(layersetCMDB, trans, changesetProxy.TimeThreshold);
            // NOTE mcsuk: the same as above for hosts+categories goes here for services+categories

            foreach (var ciItem in servicesCategories)
            {
                ciData.ForEach(el =>
                {
                    if (el.Id == ciItem.Value.ServiceId)
                    {
                        var obj = new Category
                        {
                            Id = ciItem.Value.Id,
                            Tree = ciItem.Value.CatTree,
                            Group = ciItem.Value.CatGroup,
                            Name = ciItem.Value.Category,
                            Desc = ciItem.Value.CatDesc,
                        };

                        if (!el.Categories.ContainsKey(obj.Group))
                        {
                            el.Categories.Add(obj.Group, new List<Category> { obj });
                        }
                        else
                        {
                            el.Categories[obj.Group].Add(obj);
                        }
                    }
                });
            }
            #endregion

            #region add actions
            // add host actions to cidata
            var hostActions = await hostActionModel.GetAllByDataID(layersetCMDB, trans, changesetProxy.TimeThreshold);
            //NOTE mcsuk: the same as above for hosts + categories goes here
            foreach (var ciItem in hostActions)
            {
                ciData.ForEach(el =>
                {
                    if (el.Id == ciItem.Value.HostId)
                    {
                        el.Actions = new Actions
                        {
                            Id = ciItem.Value.Id,
                            Type = ciItem.Value.Type,
                            Cmd = ciItem.Value.Cmd,
                            CmdUser = ciItem.Value.CmdUser,
                        };
                    }
                });
            }

            // add service actions to ci data 
            var serviceActions = await serviceActionModel.GetAllByDataID(layersetCMDB, trans, changesetProxy.TimeThreshold);
            //NOTE mcsuk: the same as above for hosts + categories goes here
            foreach (var ciItem in serviceActions)
            {
                ciData.ForEach(el =>
                {
                    if (el.Id == ciItem.Value.ServiceId)
                    {
                        el.Actions = new Actions
                        {
                            Id = ciItem.Value.Id,
                            Type = ciItem.Value.Type,
                            Cmd = ciItem.Value.Cmd,
                            CmdUser = ciItem.Value.CmdUser,
                        };
                    }
                });
            }

            #endregion

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

                if (cfg!.NaemonsConfigGenerateprofiles.Contains(instanceName))
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
                        capMap.Add(cap, profileFromDbNaemons);
                    }
                    else
                    {
                        capMap[cap] = (List<string>)profileFromDbNaemons.Concat(capMap[cap]);
                    }
                }
            }

            // new capmap
            var nInstancesTag = await naemonInstancesTagModel.GetAllByDataID(layersetMonman, trans, changesetProxy.TimeThreshold);
            var nProfiles = await naemonProfileModel.GetAllByDataID(layersetMonman, trans, changesetProxy.TimeThreshold);
            // NOTE we will move to use this when the nInstanceTag are fetched correctlly 
            var capMapNew = Helper.CIData.BuildCapMap(nInstancesTag, nProfiles, nInstances, cfg!.NaemonsConfigGenerateprofiles);



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
                        foreach (var pattern in cfg!.CMDBMonprofilePrefix)
                        {
                            if (Regex.IsMatch(category.Name, $"^{pattern}", RegexOptions.IgnoreCase))
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

                if (profileCount == 1 && Regex.IsMatch(ciItem.Profile, "^profile", RegexOptions.IgnoreCase))
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
                }
                else
                {
                    // TODO check if we should add this
                    //ciItem.Tags.Add("cap_default");
                }
            }

            // updateNormalizedCiData_addRelationData
            var allRunsOnRelations = await relationModel.GetMergedRelations(RelationSelectionWithPredicate.Build("runsOn"), layersetCMDB, trans, changesetProxy.TimeThreshold);

            foreach (var ciItem in ciData)
            {
                // Relations
                // 
            }
            #endregion

            /* test compatibility of naemons and add NAEMONSAVAIL */
            foreach (var item in ciData)
            {
                // TODO how to handle cases that dont have tags should NaemonsAvail include all naemon ids
                item.NaemonsAvail = naemonIds;

                foreach (var requirement in item.Tags)
                {
                    if (capMap.ContainsKey(requirement))
                    {
                        item.NaemonsAvail = item.NaemonsAvail.Intersect(capMap[requirement]).ToList();
                    }
                    else
                    {
                        item.NaemonsAvail = new List<string>();
                    }
                }
            }

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




            #region get configuration from legacy objects

            // TODO: we should generate legacy objects only if isNaemonProfileFromDbEnabled condition is fulfilled

            // getNaemonConfigObjectsFromLegacyProfiles_globalVars

            var variables = await traitModel.FilterCIsWithTrait(allCIsMonman, Traits.VariablesFlattened, layersetMonman, trans, changesetProxy.TimeThreshold);

            var variablesAttributes = new Dictionary<string, string>
            {
                ["name"] = "global-variables",
                ["_ALERTS"] = "OFF",
                ["_NRPEPORT"] = "5666"
            };

            foreach (var ciItem in variables)
            {
                ciItem.MergedAttributes.TryGetValue("naemon_variable.reftype", out MergedCIAttribute? refTypeAttribute);

                var refType = refTypeAttribute!.Attribute.Value.Value2String();


                ciItem.MergedAttributes.TryGetValue("naemon_variable.type", out MergedCIAttribute? typeAttribute);

                var type = typeAttribute!.Attribute.Value.Value2String();

                // select only variables that have reftype GLOBAL
                if (refType != "GLOBAL" || type != "value")
                {
                    continue;
                }

                string id = "", name = "", value = "";
                bool isSecret = false;

                foreach (var attribute in ciItem.MergedAttributes)
                {
                    switch (attribute.Key)
                    {
                        case "naemon_variable.id":
                            id = attribute.Value.Attribute.Value.Value2String();
                            break;
                        case "naemon_variable.name":
                            name = attribute.Value.Attribute.Value.Value2String();
                            break;
                        case "naemon_variable.value":
                            value = attribute.Value.Attribute.Value.Value2String();
                            break;
                        case "naemon_variable.issecret":
                            isSecret = Convert.ToBoolean(Convert.ToInt16(attribute.Value.Attribute.Value.Value2String()));
                            break;
                        default:
                            break;
                    }
                }

                if (isSecret)
                {
                    value = $"$(python /opt2/nm-agent/bin/getSecret.py {id})";
                }

                var key = $"_{name.ToUpper()}";
                if (!variablesAttributes.ContainsKey(key))
                {
                    variablesAttributes.Add(key, value);
                }
                else if (value != "")
                {
                    variablesAttributes[key] = value;
                }
                
            }

            configObjs.Add(new ConfigObj
            {
                Type = "host",
                Attributes = variablesAttributes,
            });

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


            // getCommands -> We need only command ids here

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

                configObjs.Add(new ConfigObj
                {
                    Type = "host",
                    Attributes = new Dictionary<string, string>
                    {
                        ["name"] = "mod-" + moduleName,
                        ["hostgroups"] = "+" + moduleName,
                    },
                });

                configObjs.Add(new ConfigObj
                {
                    Type = "hostgroup",
                    Attributes = new Dictionary<string, string>
                    {
                        ["hostgroup_name"] = moduleName,
                        ["alias"] = moduleName,
                    },
                });

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
                        var commandParts = new List<string> { commandName };

                        for (int i = 1; i <= 8; i++)
                        {
                            var k = $"naemon_services_static.arg{i}";

                            if (service.MergedAttributes.TryGetValue(k, out MergedCIAttribute? attr))
                            {
                                var arg = (attr.Attribute.Value as AttributeScalarValueText)?.Value;

                                if (arg.Length > 0)
                                {
                                    if (arg.Substring(0, 1) == "$")
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

            #endregion



            //TODO getNaemonConfigObjectsFromLegacyProfiles_hostcommands

            #region get cis for naemons
            var naemonsCis = new Dictionary<string, List<ConfigurationItem>>();
            foreach (var item in naemonInstances)
            {

                var s = item.MergedAttributes.TryGetValue("naemon_instance.id", out MergedCIAttribute? instanceId);

                if (!s)
                {
                    // log error here
                }

                var id = instanceId!.Attribute.Value.Value2String();

                // get only configuration items for this naemon
                //var thisNaemonCis = new List<dynamic>();

                naemonsCis.Add(id, new List<ConfigurationItem>());
                foreach (var ciItem in ciData)
                {
                    if (ciItem.NaemonsAvail.Contains(id))
                    {
                        //thisNaemonCis.Add(ciItem);
                        naemonsCis[id].Add(ciItem);
                    }
                }
            }
            #endregion

            #region generate configs foreach naemon instance
            // first create new objects
            var naemonConfigObjs = new Dictionary<string, List<ConfigObj>>();
            foreach (var item in naemonsCis)
            {
                // deployed cis for this profile
                var naemonDeployedCIs = new List<ConfigurationItem>();
                var naemonObjs = new List<ConfigObj>();

                foreach (var ciItem in item.Value)
                {
                    if (Regex.IsMatch(ciItem.Profile, "^profiledynamic-tsi-silverpeak-"))
                    {
                        naemonDeployedCIs.Add(ciItem);
                    }
                    else if (ciItem.Profile != "NONE")
                    {
                        var obj = new ConfigObj
                        {
                            Type = "host",
                            Attributes = new Dictionary<string, string>
                            {
                                ["host_name"] = ciItem.Name,
                                ["alias"] = ciItem.Id,
                                ["address"] = ciItem.Address,
                            }
                        };

                        var uses = new List<string>();

                        if (ciItem.Profile == "dynamic-nrpe" || ciItem.Profile == "MULTIPLE")
                        {
                            uses.Add("global-variables");
                            uses.Add("tsa-generic-host");
                        }
                        else
                        {
                            uses.Add(ciItem.Profile);
                        }

                        obj.Attributes.Add("use", string.Join(",", uses));

                        // TODO: add vars here

                        naemonObjs.Add(obj);
                    }
                }

                naemonConfigObjs.Add(item.Key, naemonObjs.Concat(configObjs).ToList());
                //naemonConfigObjs.Add(item.Key, configObjs);

                // TODO: we also need to process deployed cis for this naemon, check applib-confgen-ci.php#74

            }
            #endregion

            // convert into jobjects

            var jobjects = new Dictionary<string, JObject>();

            foreach (var item in naemonConfigObjs)
            {
                //var ci = await ciModel.CreateCI(trans);

                var s1 = JsonConvert.SerializeObject(item.Value);
                var ss = JArray.FromObject(item.Value);

                if (item.Key == "H12037680")
                {
                    var a = 5;
                }

                //var (attribute, changed) = await attributeModel.InsertAttribute("config", AttributeScalarValueJSON.Build(ss), ci, "naemon_config", changesetProxy, new DataOriginV1(DataOriginType.Manual), trans);
            }

            return true;
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

        internal class ConfigObj
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("attributes")]
            public Dictionary<string, string> Attributes { get; set; }

            public ConfigObj()
            {
                Type = "";
                Attributes = new Dictionary<string, string>();
            }
        }

        internal class ConfigurationItem
        {
            public ConfigurationItem()
            {
                Type = "";
                Id = "";
                Name = "";
                Status = "";
                Environment = "";
                Profile = "";
                Address = "";
                Interfaces = new Interfaces();
                Relations = new Dictionary<string, Dictionary<string, string>>();
                Categories = new Dictionary<string, List<Category>>();
                Actions = new Actions();
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
            public Dictionary<string, Dictionary<string, string>> Relations { get; set; }
        }

        internal class Category
        {
            public string Id { get; set; }
            public string Tree { get; set; }
            public string Group { get; set; }
            public string Name { get; set; }
            public string Desc { get; set; }

            public Category()
            {
                Id = "";
                Tree = "";
                Group = "";
                Name = "";
                Desc = "";
            }
        }

        internal class Actions
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public string Cmd { get; set; }
            public string CmdUser { get; set; }

            public Actions()
            {
                Id = "";
                Type = "";
                Cmd = "";
                CmdUser = "";
            }
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

        internal class Configuration
        {
            [JsonProperty("monman_layer_id", Required = Required.Always)]
            public string MonmanLayerId { get; set; }

            [JsonProperty("cmdb_layer_id", Required = Required.Always)]
            public string CMDBLayerId { get; set; }

            [JsonProperty("naemon_config_layer_id", Required = Required.Always)]
            public string NaemonConfigLayerId { get; set; }

            [JsonProperty("load-cmdb-customer", Required = Required.Always)]
            public List<string> LoadCMDBCustomer { get; set; }

            [JsonProperty("cmdb-monprofile-prefix", Required = Required.Always)]
            public List<string> CMDBMonprofilePrefix { get; set; }

            [JsonProperty("naemons-config-generateprofiles", Required = Required.Always)]
            public List<string> NaemonsConfigGenerateprofiles { get; set; }

            public Configuration()
            {
                MonmanLayerId = "";
                CMDBLayerId = "";
                NaemonConfigLayerId = "";
                LoadCMDBCustomer = new List<string>();
                CMDBMonprofilePrefix = new List<string>();
                NaemonsConfigGenerateprofiles = new List<string>();
            }
        }
    }
}
