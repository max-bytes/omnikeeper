using Microsoft.Extensions.Logging;
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
        private readonly GenericTraitEntityModel<HostCategory, string> hostsCategoryModel;
        private readonly GenericTraitEntityModel<ServiceCategory, string> servicesCategoryModel;
        private readonly GenericTraitEntityModel<HostAction, string> hostActionModel;
        private readonly GenericTraitEntityModel<ServiceAction, string> serviceActionModel;
        private readonly GenericTraitEntityModel<NaemonInstanceTag, string> naemonInstancesTagModel;
        private readonly GenericTraitEntityModel<NaemonProfile, string> naemonProfileModel;
        private readonly GenericTraitEntityModel<TimePeriod, string> timePeriodModel;
        private readonly GenericTraitEntityModel<Variable, string> variableModel;
        private readonly GenericTraitEntityModel<ServiceLayer, string> serviceLayerModel;
        private readonly GenericTraitEntityModel<Command, string> commandModel;
        private readonly GenericTraitEntityModel<ServiceStatic, string> serviceStaticModel;
        private readonly GenericTraitEntityModel<Module, string> moduleModel;
        public NaemonConfig(ICIModel ciModel, IAttributeModel atributeModel, ILayerModel layerModel, IEffectiveTraitModel traitModel, IRelationModel relationModel,
                           IChangesetModel changesetModel, IUserInDatabaseModel userModel,
                           GenericTraitEntityModel<NaemonInstance, string> naemonInstanceModel,
                           GenericTraitEntityModel<Service, string> serviceModel,
                           GenericTraitEntityModel<HostCategory, string> hostsCategoryModel,
                           GenericTraitEntityModel<ServiceCategory, string> servicesCategoryModel,
                           GenericTraitEntityModel<HostAction, string> hostActionModel,
                           GenericTraitEntityModel<ServiceAction, string> serviceActionModel,
                           GenericTraitEntityModel<NaemonInstanceTag, string> naemonInstancesTagModel,
                           GenericTraitEntityModel<NaemonProfile, string> naemonProfileModel,
                           GenericTraitEntityModel<TimePeriod, string> timePeriodModel,
                           GenericTraitEntityModel<Variable, string> variableModel,
                           GenericTraitEntityModel<ServiceLayer, string> serviceLayerModel,
                           GenericTraitEntityModel<Command, string> commandModel,
                           GenericTraitEntityModel<ServiceStatic, string> serviceStaticModel,
                           GenericTraitEntityModel<Module, string> moduleModel,
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
            this.timePeriodModel = timePeriodModel;
            this.variableModel = variableModel;
            this.serviceLayerModel = serviceLayerModel;
            this.commandModel = commandModel;
            this.serviceStaticModel = serviceStaticModel;
            this.moduleModel = moduleModel;
        }

        public override async Task<bool> Run(Layer targetLayer, JObject config, IChangesetProxy changesetProxy, CLBErrorHandler errorHandler, IModelContext trans, ILogger logger)
        {
            logger.LogDebug("Start naemonConfig");

            var cfg = new Configuration();

            try
            {
                cfg = config.ToObject<Configuration>();
            }
            catch (Exception ex)
            {
                logger.LogError("An error ocurred while creating configuration instance.", ex);
                return false;
            }

            var layersetCMDB = await layerModel.BuildLayerSet(new[] { cfg!.CMDBLayerId }, trans);
            var layersetMonman = await layerModel.BuildLayerSet(new[] { cfg!.MonmanLayerId }, trans);
            var layersetNaemonConfig = await layerModel.BuildLayerSet(new[] { cfg!.NaemonConfigLayerId }, trans);

            // load all naemons
            var naemonInstances = await naemonInstanceModel.GetAllByDataID(layersetMonman, trans, changesetProxy.TimeThreshold);
            logger.LogInformation("Loaded all naemon instances.");


            logger.LogInformation("Started creating configuration items.");
            // a list with all CI from database
            var ciData = new List<ConfigurationItem>();

            var hosts = await hostModel.GetAllByDataID(layersetCMDB, trans, changesetProxy.TimeThreshold);
            logger.LogInformation("Loaded all hosts.");
            foreach (var ciItem in hosts)
            {
                ciData.Add(new ConfigurationItem
                {
                    Type = "HOST",
                    Id = ciItem.Value.Id,
                    Name = ciItem.Value.Name,
                    Status = ciItem.Value.Status,
                    Address = "", // This should be taken from HMONIPADDRESS field
                    Port = 0, // This should be taken from HMONIPPORT field
                    Cust = "", // Add customer for this ci,
                    Criticality = "", // Add Criticality for this ci,
                    SuppOS = "", // Add SuppOS for this ci,
                    SuppApp = "", // Add SuppApp for this ci,

                });
            }

            // get services
            var services = await serviceModel.GetAllByDataID(layersetCMDB, trans, changesetProxy.TimeThreshold);
            logger.LogInformation("Loaded all services.");

            foreach (var ciItem in services)
            {
                ciData.Add(new ConfigurationItem
                {
                    Type = "SERVICE",
                    Id = ciItem.Value.Id,
                    Name = ciItem.Value.Name,
                    Status = ciItem.Value.Status,
                    Environment = ciItem.Value.Environment,
                    Address = "", // This should be taken from SVCMONIPADDRESS field
                    Port = 0, // This should be taken from SVCMONIPPORT field
                    Cust = "", // Add customer for this ci
                    Criticality = "", // Add Criticality for this ci,
                    SuppOS = "", // Add SuppOS for this ci,
                    SuppApp = "", // Add SuppApp for this ci,

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

                        /* override address from hostactions */
                        if (ciItem.Value.Type.ToUpper() == "MONITORING")
                        {
                            el.Address = ciItem.Value.Cmd;
                        }
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
            // we should load interfaces, currently only one column is loaded from this table into omnikeeper

            //var interfaces = await traitModel.FilterCIsWithTrait(allCIsCMDB, Traits.InterfacesFlattened, layersetCMDB, trans, changesetProxy.TimeThreshold);

            //foreach (var ciItem in interfaces)
            //{
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
            //}



            #region process core data

            logger.LogInformation("Started processing core data.");

            // UpdateNormalizedCiDataFieldProfile
            Helper.CIData.UpdateProfileField(ciData, cfg!.CMDBMonprofilePrefix);
            logger.LogInformation("Finished updating profile field => UpdateNormalizedCiDataFieldProfile.");


            // updateNormalizedCiDataFieldAddress
            // NOTE: this part is done directly when selecting hosts and serices

            // updateNormalizedCiData_addGenericCmdbCapTags
            Helper.CIData.AddGenericCmdbCapTags(ciData);
            logger.LogInformation("Finished adding cap tags => updateNormalizedCiData_addGenericCmdbCapTags.");

            // updateNormalizedCiData_addRelationData
            var allRunsOnRelations = await relationModel.GetMergedRelations(RelationSelectionWithPredicate.Build("runs_on"), layersetCMDB, trans, changesetProxy.TimeThreshold);

            var cmdbRelationsBySrc = new Dictionary<string, (string, string)>();

            foreach (var item in allRunsOnRelations)
            {

                var fromCI = await ciModel.GetMergedCI(item!.Relation.FromCIID, layersetCMDB!, AllAttributeSelection.Instance, trans, changesetProxy.TimeThreshold);

                var fromCIID = (fromCI.MergedAttributes["cmdb.id"].Attribute.Value as AttributeScalarValueText)?.Value;

                var cfgObj = ciData.Where(el => el.Type == "SERVICE" && el.Id == fromCIID).FirstOrDefault();

                if (cfgObj != null)
                {
                    var targetCI = await ciModel.GetMergedCI(item.Relation.ToCIID, layersetCMDB!, AllAttributeSelection.Instance, trans, changesetProxy.TimeThreshold);

                    var targetCIID = (fromCI.MergedAttributes["cmdb.id"].Attribute.Value as AttributeScalarValueText)?.Value;

                    if (!cfgObj.Relations.ContainsKey("OUT"))
                    {
                        cfgObj.Relations.Add("OUT", new KeyValuePair<string, string>(item.Relation.PredicateID, targetCIID!));
                    }
                    else
                    {
                        cfgObj.Relations["OUT"] = new KeyValuePair<string, string>(item.Relation.PredicateID, targetCIID!);
                    }

                    // 
                    if (!cmdbRelationsBySrc.ContainsKey(fromCIID!))
                    {
                        cmdbRelationsBySrc.Add(fromCIID!, (item.Relation.PredicateID, targetCIID!));
                    }
                    else
                    {
                        cmdbRelationsBySrc[fromCIID!] = (item.Relation.PredicateID, targetCIID!);
                    }

                    // write incoming relations if CI exists

                    var targetCfgObj = ciData.Where(el => el.Id == targetCIID).FirstOrDefault();

                    if (targetCfgObj != null)
                    {

                        if (!targetCfgObj.Relations.ContainsKey("IN"))
                        {
                            targetCfgObj.Relations.Add("IN", new KeyValuePair<string, string>(item.Relation.PredicateID, fromCIID!));
                        }
                        else
                        {
                            targetCfgObj.Relations["IN"] = new KeyValuePair<string, string>(item.Relation.PredicateID, fromCIID!);
                        }
                    }
                }
            }

            foreach (var ciItem in ciData)
            {
                // add effective host for ci

                //$ciDataRef[$id]['RELATIONS']['EFFECTIVEHOSTCI'] = '';

                ciItem.EffectiveHostCI = "";

                if (ciItem.Id.StartsWith("H"))
                {
                    ciItem.EffectiveHostCI = ciItem.Id;
                }
                else
                {

                }

            }

            logger.LogInformation("Finished adding relation data => updateNormalizedCiData_addRelationData.");


            /* update data, mainly vars stuff */
            // updateNormalizedCiData_preProcessVars

            foreach (var ciItem in ciData)
            {
                // NOTE should we update resultRef[$id]['VARS']['ALERTS'] = 'OFF' ?
                if (!ciItem.Vars.ContainsKey("HASNRPE"))
                {
                    ciItem.Vars.Add("HASNRPE", "YES");
                }

                if (!ciItem.Vars.ContainsKey("DYNAMICADD"))
                {
                    ciItem.Vars.Add("DYNAMICADD", "NO");
                }

                if (!ciItem.Vars.ContainsKey("DYNAMICMODULES"))
                {
                    ciItem.Vars.Add("DYNAMICMODULES", "YES");
                }
            }

            logger.LogInformation("Finished updating pre process vars => updateNormalizedCiData_preProcessVars.");

            // updateNormalizedCiData_varsFromDatabase


            // updateNormalizedCiData_varsByExpression

            // run at last or at least after vars engine to ensure overwriting of internal vars
            // updateNormalizedCiData_postProcessVars

            Helper.CIData.UpdateNormalizedCiDataPostProcessVars(ciData);
            logger.LogInformation("Finished updating post process cars => updateNormalizedCiData_postProcessVars.");

            // updateNormalizedCiData_updateLocationField

            #endregion


            #region build CapabilityMap
            //getCapabilityMap - NaemonInstancesTagsFlattened

            var nInstancesTag = await naemonInstancesTagModel.GetAllByDataID(layersetMonman, trans, changesetProxy.TimeThreshold);
            var nProfiles = await naemonProfileModel.GetAllByDataID(layersetMonman, trans, changesetProxy.TimeThreshold);
            // NOTE we will move to use this when the nInstanceTag are fetched correctlly 
            var capMap = Helper.CIData.BuildCapMap(nInstancesTag, nProfiles, naemonInstances, cfg!.NaemonsConfigGenerateprofiles);

            #endregion

            /* test compatibility of naemons and add NAEMONSAVAIL */
            var naemonIds = naemonInstances.Select(el => el.Value.Id).ToList();
            Helper.CIData.AddNaemonsAvailField(ciData, naemonIds, capMap);

            var configObjs = new List<ConfigObj>();

            // apply global filter
            // include only cis that have STATUS = to "ACTIVE" || "BASE_INSTALLED" || "READY_FOR_SERVICE"
            // TODO: this should be configurable
            ciData = ciData.Where(el => el.Status == "ACTIVE" || el.Status == "BASE_INSTALLED" || el.Status == "READY_FOR_SERVICE").ToList();

            // getNaemonConfigObjectsFromStaticTemplates - global-commands
            Helper.ConfigObjects.GetFromStaticTemplates(configObjs);

            #region getNaemonConfigObjectsFromTimeperiods

            var timeperiods = await timePeriodModel.GetAllByDataID(layersetMonman, trans, changesetProxy.TimeThreshold);
            Helper.ConfigObjects.GetFromTimeperiods(configObjs, timeperiods);

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

            var variables = await variableModel.GetAllByDataID(layersetMonman, trans, changesetProxy.TimeThreshold);
            Helper.ConfigObjects.GetFromLegacyProfilesGlobalVars(configObjs, variables);


            // getNaemonConfigObjectsFromLegacyProfiles_profiles

            var appendBasetemplates = new List<string> { "global-variables", "tsa-generic-host" };

            // getNaemonConfigObjectsFromLegacyProfiles_modules

            //var serviceLayers = await traitModel.FilterCIsWithTrait(allCIsMonman, Traits.ServiceLayersFlattened, layersetMonman, trans, changesetProxy.TimeThreshold);

            //var layersById = new Dictionary<string, MergedCI>();
            //foreach (var ciItem in serviceLayers)
            //{
            //    var layerId = ciItem.MergedAttributes["naemon_service_layer.id"]!.Attribute.Value.Value2String();

            //    layersById.Add(layerId, ciItem);
            //}


            // getCommands -> We need only command ids here

            //var commands = await traitModel.FilterCIsWithTrait(allCIsMonman, Traits.CommandsFlattened, layersetMonman, trans, changesetProxy.TimeThreshold);

            //var commandsById = new Dictionary<string, MergedCI>();
            //foreach (var ciItem in commands)
            //{
            //    var commandId = ciItem.MergedAttributes["naemon_command.id"]!.Attribute.Value.Value2String();

            //    commandsById.Add(commandId, ciItem);
            //}

            // getTimeperiods -> We need a list with timeperiod ids

            //var timeperiodsById = new Dictionary<string, MergedCI>();
            //var timeperiodsById = timeperiods;


            //foreach (var ciItem in timeperiods)
            //{
            //    var timeperiodId = ciItem.MergedAttributes["naemon_timeperiod.id"]!.Attribute.Value.Value2String();

            //    timeperiodsById.Add(timeperiodId, ciItem);
            //}

            // prepare services
            // get SERVICES_STATIC

            //var servicesStatic = await traitModel.FilterCIsWithTrait(allCIsMonman, Traits.ServicesStaticFlattened, layersetMonman, trans, changesetProxy.TimeThreshold);
            //var servicesStaticById = new Dictionary<string, MergedCI>();
            //var servicesByModulesId = new Dictionary<string, List<MergedCI>>();
            //foreach (var ciItem in servicesStatic)
            //{
            //    var id = ciItem.MergedAttributes["naemon_services_static.id"]!.Attribute.Value.Value2String();

            //    servicesStaticById.Add(id, ciItem);

            //    var module = ciItem.MergedAttributes["naemon_services_static.module"]!.Attribute.Value.Value2String();

            //    if (servicesByModulesId.ContainsKey(module))
            //    {
            //        servicesByModulesId[module].Add(ciItem);
            //    }
            //    else
            //    {
            //        servicesByModulesId[module] = new List<MergedCI> { ciItem };
            //    }
            //}

            //var modules = await traitModel.FilterCIsWithTrait(allCIsMonman, Traits.NaemonModulesFlattened, layersetMonman, trans, changesetProxy.TimeThreshold);

            //foreach (var ciItem in modules)
            //{
            //    var moduleName = ciItem.MergedAttributes["naemon_module.name"]!.Attribute.Value.Value2String().ToLower();

            //    configObjs.Add(new ConfigObj
            //    {
            //        Type = "host",
            //        Attributes = new Dictionary<string, string>
            //        {
            //            ["name"] = "mod-" + moduleName,
            //            ["hostgroups"] = "+" + moduleName,
            //        },
            //    });

            //    configObjs.Add(new ConfigObj
            //    {
            //        Type = "hostgroup",
            //        Attributes = new Dictionary<string, string>
            //        {
            //            ["hostgroup_name"] = moduleName,
            //            ["alias"] = moduleName,
            //        },
            //    });

            //    var moduleId = ciItem.MergedAttributes["naemon_module.id"]!.Attribute.Value.Value2String().ToLower();

            //    if (servicesByModulesId.ContainsKey(moduleId))
            //    {
            //        foreach (var service in servicesByModulesId[moduleId])
            //        {
            //            // goto next if service is disabled
            //            var disabled = (service.MergedAttributes["naemon_services_static.disabled"].Attribute.Value as AttributeScalarValueInteger)?.Value;
            //            if (disabled > 0)
            //            {
            //                continue;
            //            }

            //            var attributes = new Dictionary<string, string>();
            //            var vars = new Dictionary<string, string>();

            //            var serviceTemplates = new List<string> { "tsa-generic-service" };

            //            var perf = (service.MergedAttributes["naemon_services_static.perf"].Attribute.Value as AttributeScalarValueInteger)?.Value;

            //            if (perf == 1)
            //            {
            //                serviceTemplates.Add("service-pnp");
            //            }

            //            attributes["use"] = string.Join(",", serviceTemplates);
            //            attributes["hostgroup_name"] = moduleName;

            //            // $layersById[$service['LAYER']]['NUM'] . ' ' . $service['SERVICENAME'];

            //            var svcLayer = (service.MergedAttributes["naemon_services_static.layer"].Attribute.Value as AttributeScalarValueInteger)?.Value;
            //            var layerNum = (layersById[svcLayer.ToString()].MergedAttributes["naemon_service_layer.num"].Attribute.Value as AttributeScalarValueInteger)?.Value;
            //            var svcName = (service.MergedAttributes["naemon_services_static.servicename"].Attribute.Value as AttributeScalarValueText)?.Value;
            //            attributes["service_description"] = layerNum + " " + svcName;

            //            var svcCheckCommand = (service.MergedAttributes["naemon_services_static.checkcommand"].Attribute.Value as AttributeScalarValueInteger)?.Value;
            //            var commandName = (commandsById[svcCheckCommand.ToString()].MergedAttributes["naemon_command.name"].Attribute.Value as AttributeScalarValueText)?.Value;
            //            var commandParts = new List<string> { commandName };

            //            for (int i = 1; i <= 8; i++)
            //            {
            //                var k = $"naemon_services_static.arg{i}";

            //                if (service.MergedAttributes.TryGetValue(k, out MergedCIAttribute? attr))
            //                {
            //                    var arg = (attr.Attribute.Value as AttributeScalarValueText)?.Value;

            //                    if (arg.Length > 0)
            //                    {
            //                        if (arg.Substring(0, 1) == "$")
            //                        {
            //                            arg = "$HOST" + arg.Substring(1, arg.Length - 1) + "$";
            //                        }

            //                        commandParts.Add(arg);
            //                    }
            //                }
            //            }

            //            attributes["check_command"] = string.Join("!", commandParts);

            //            /* add other attributes */

            //            var freshnesThreshold = GetAttributeValueInt("naemon_services_static.freshness_threshold", service.MergedAttributes);

            //            if (freshnesThreshold != null)
            //            {
            //                attributes["check_freshness"] = "1";
            //                attributes["freshness_threshold"] = freshnesThreshold.ToString();
            //            }


            //            var passive = GetAttributeValueInt("naemon_services_static.passive", service.MergedAttributes);
            //            if (passive != null && passive == 1)
            //            {
            //                attributes["passive_checks_enabled"] = "1";
            //                attributes["active_checks_enabled"] = "0";
            //            }

            //            var checkInterval = GetAttributeValueInt("naemon_services_static.check_interval", service.MergedAttributes);
            //            if (checkInterval != null && checkInterval != 0)
            //            {
            //                attributes["active_checks_enabled"] = checkInterval.ToString();
            //            }

            //            var maxCheckAttempts = GetAttributeValueInt("naemon_services_static.max_check_attempts", service.MergedAttributes);
            //            if (maxCheckAttempts != null && maxCheckAttempts != 0)
            //            {
            //                attributes["max_check_attempts"] = maxCheckAttempts.ToString();
            //            }

            //            var retryInterval = GetAttributeValueInt("naemon_services_static.retry_interval", service.MergedAttributes);
            //            if (retryInterval != null && retryInterval != 0)
            //            {
            //                attributes["retry_interval"] = retryInterval.ToString();
            //            }

            //            //var timeperiodCheck = GetAttributeValueInt("naemon_services_static.timeperiod_check", service.MergedAttributes);
            //            //if (timeperiodCheck != null && timeperiodsById.ContainsKey(timeperiodCheck.ToString()))
            //            //{
            //            //    var tName = (timeperiodsById[timeperiodCheck.ToString()].MergedAttributes["naemon_timeperiod.name"].Attribute.Value as AttributeScalarValueText)?.Value;

            //            //    attributes["check_period"] = tName;
            //            //}

            //            //var timeperiodNotify = GetAttributeValueInt("naemon_services_static.timeperiod_notify", service.MergedAttributes);
            //            //if (timeperiodNotify != null && timeperiodsById.ContainsKey(timeperiodNotify.ToString()))
            //            //{
            //            //    var tName = (timeperiodsById[timeperiodNotify.ToString()].MergedAttributes["naemon_timeperiod.name"].Attribute.Value as AttributeScalarValueText)?.Value;

            //            //    attributes["notification_period"] = tName;
            //            //}

            //            /* fill variables */
            //            var target = (service.MergedAttributes["naemon_services_static.target"].Attribute.Value as AttributeScalarValueText)?.Value;
            //            vars["Tickettarget"] = target;

            //            var checkPrio = (service.MergedAttributes["naemon_services_static.checkprio"].Attribute.Value as AttributeScalarValueText)?.Value;
            //            vars["Fehlerart"] = checkPrio;

            //            /* customer cockpit vars */
            //            var visibility = GetAttributeValueInt("naemon_services_static.monview_visibility", service.MergedAttributes);
            //            if (visibility != null)
            //            {
            //                vars["Visibility"] = visibility.ToString();
            //            }

            //            var kpiType = GetAttributeValueInt("naemon_services_static.kpi_avail", service.MergedAttributes);
            //            if (kpiType != null)
            //            {
            //                vars["kpi_type"] = kpiType.ToString();
            //            }

            //            var metricName = (service.MergedAttributes["naemon_services_static.metric_name"].Attribute.Value as AttributeScalarValueText)?.Value;
            //            vars["metric_name"] = metricName;

            //            var metricType = (service.MergedAttributes["naemon_services_static.metric_type"].Attribute.Value as AttributeScalarValueText)?.Value;
            //            vars["metric_type"] = metricType;

            //            var metricMin = GetAttributeValueInt("naemon_services_static.metric_min", service.MergedAttributes);
            //            if (metricMin != null)
            //            {
            //                vars["metric_min"] = metricMin.ToString();
            //            }

            //            var metricMax = GetAttributeValueInt("naemon_services_static.metric_max", service.MergedAttributes);
            //            if (metricMax != null)
            //            {
            //                vars["metric_max"] = metricMax.ToString();

            //            }

            //            var metricPerfkey = (service.MergedAttributes["naemon_services_static.metric_perfkey"].Attribute.Value as AttributeScalarValueText)?.Value;
            //            vars["metric_perfkey"] = metricPerfkey;

            //            var metricPerfunit = (service.MergedAttributes["naemon_services_static.metric_perfunit"].Attribute.Value as AttributeScalarValueText)?.Value;
            //            vars["metric_perfunit"] = metricPerfunit;

            //            var monviewWarn = (service.MergedAttributes["naemon_services_static.monview_warn"].Attribute.Value as AttributeScalarValueText)?.Value;
            //            vars["monview_warn"] = monviewWarn;

            //            var monviewCrit = (service.MergedAttributes["naemon_services_static.monview_crit"].Attribute.Value as AttributeScalarValueText)?.Value;
            //            vars["monview_crit"] = monviewCrit;

            //            /* add variables to attributes */
            //            foreach (var var in vars)
            //            {
            //                var (key, value) = var;
            //                var k = "_" + key.ToUpper();
            //                attributes[k] = value ?? "";
            //            }

            //            /* push service to config */
            //            configObjs.Add(new ConfigObj { Type = "service", Attributes = attributes });

            //            /* add servicedependency objects */
            //            var mastersvcCheck = GetAttributeValueInt("naemon_services_static.mastersvc_check", service.MergedAttributes);
            //            if (mastersvcCheck != null && servicesStaticById.ContainsKey(mastersvcCheck.ToString()))
            //            {
            //                var masterService = servicesStaticById[mastersvcCheck.ToString()];
            //                var masterSvcName = (masterService.MergedAttributes["naemon_services_static.servicename"].Attribute.Value as AttributeScalarValueText)?.Value;

            //                configObjs.Add(new ConfigObj
            //                {
            //                    Type = "servicedependency",
            //                    Attributes = new Dictionary<string, string>
            //                    {
            //                        ["hostgroup_name"] = moduleName,
            //                        ["service_description"] = layerNum + " " + masterSvcName,
            //                        ["dependent_service_description"] = layerNum + " " + svcName,
            //                        ["inherits_parent"] = "1",
            //                        ["execution_failure_criteria"] = "w,u,c",
            //                    }
            //                });
            //            }

            //            var mastersvcNotify = GetAttributeValueInt("naemon_services_static.mastersvc_notify", service.MergedAttributes);
            //            if (mastersvcNotify != null && servicesStaticById.ContainsKey(mastersvcNotify.ToString()))
            //            {
            //                var masterService = servicesStaticById[mastersvcNotify.ToString()];
            //                var masterSvcName = (masterService.MergedAttributes["naemon_services_static.servicename"].Attribute.Value as AttributeScalarValueText)?.Value;

            //                configObjs.Add(new ConfigObj
            //                {
            //                    Type = "servicedependency",
            //                    Attributes = new Dictionary<string, string>
            //                    {
            //                        ["hostgroup_name"] = moduleName,
            //                        ["service_description"] = layerNum + " " + masterSvcName,
            //                        ["dependent_service_description"] = layerNum + " " + svcName,
            //                        ["inherits_parent"] = "1",
            //                        ["execution_failure_criteria"] = "w,u,c",
            //                    }
            //                });
            //            }
            //        }
            //    }



            //}


            // We can use this part only when optional attributes are selected correctly
            var serviceLayers = await serviceLayerModel.GetAllByDataID(layersetMonman, trans, changesetProxy.TimeThreshold);
            var commands = await commandModel.GetAllByDataID(layersetMonman, trans, changesetProxy.TimeThreshold);
            var servicesStatic = await serviceStaticModel.GetAllByDataID(layersetMonman, trans, changesetProxy.TimeThreshold);
            var modules = await moduleModel.GetAllByDataID(layersetMonman, trans, changesetProxy.TimeThreshold);

            Helper.ConfigObjects.GetNaemonConfigObjectsFromLegacyProfilesModules(configObjs, serviceLayers, commands, timeperiods, servicesStatic, modules);




            // getNaemonConfigObjectsFromLegacyProfiles_commands
            Helper.ConfigObjects.GetNaemonConfigObjectsFromLegacyProfilesCommands(configObjs, commands);

            #endregion

            //TODO getNaemonConfigObjectsFromLegacyProfiles_hostcommands

            #region get cis for naemons

            // NOTE remove the commented part below
            //foreach (var item in naemonInstances)
            //{

            //    var s = item.MergedAttributes.TryGetValue("naemon_instance.id", out MergedCIAttribute? instanceId);

            //    if (!s)
            //    {
            //        // log error here
            //    }

            //    var id = instanceId!.Attribute.Value.Value2String();

            //    // get only configuration items for this naemon
            //    //var thisNaemonCis = new List<dynamic>();

            //    naemonsCis.Add(id, new List<ConfigurationItem>());
            //    foreach (var ciItem in ciData)
            //    {
            //        if (ciItem.NaemonsAvail.Contains(id))
            //        {
            //            //thisNaemonCis.Add(ciItem);
            //            naemonsCis[id].Add(ciItem);
            //        }
            //    }
            //}

            // NEW 
            var naemonsCis = new Dictionary<string, List<ConfigurationItem>>();
            foreach (var item in naemonInstances)
            {
                naemonsCis.Add(item.Value.Id, new List<ConfigurationItem>());
                foreach (var ciItem in ciData)
                {
                    if (ciItem.NaemonsAvail.Contains(item.Value.Id))
                    {
                        naemonsCis[item.Value.Id].Add(ciItem);
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

        public class ConfigObj
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

        public class ConfigurationItem
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
                EffectiveHostCI = "";
                Cust = "";
                Criticality = "";
                SuppApp = "";
                SuppOS = "";
                Interfaces = new Interfaces();
                Relations = new Dictionary<string, KeyValuePair<string, string>>();
                Categories = new Dictionary<string, List<Category>>();
                Actions = new Actions();
                Categories = new Dictionary<string, List<Category>>();
                Tags = new List<string>();
                ProfileOrg = new List<string>();
                NaemonsAvail = new List<string>();
                Vars = new Dictionary<string, string>();
                CmdbData = new Dictionary<string, string>();
            }
            public string Type { get; set; }
            public string Id { get; set; }
            public string Name { get; set; }
            public string Status { get; set; }
            public string Environment { get; set; }
            public string Profile { get; set; }
            public string Address { get; set; }
            public int Port { get; set; }
            public string Cust { get; set; }
            public string Criticality { get; set; }
            public string SuppApp { get; set; }
            public string SuppOS { get; set; }
            public List<string> ProfileOrg { get; set; }
            public List<string> NaemonsAvail { get; set; }
            public Dictionary<string, List<Category>> Categories { get; set; }
            public List<string> Tags { get; set; }
            public Actions Actions { get; set; }
            public Interfaces Interfaces { get; set; }
            public Dictionary<string, KeyValuePair<string, string>> Relations { get; set; }
            public string EffectiveHostCI { get; set; }
            public Dictionary<string, string> Vars { get; set; }

            // NOTE fill this data with all columns from database
            public Dictionary<string, string> CmdbData { get; set; }
        }

        public class Category
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

        public class Actions
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

        public class Interfaces
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
