using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OKPluginNaemonConfig.Entity;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
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
        private readonly ILayerModel layerModel;
        private readonly IAttributeModel attributeModel;
        private readonly GenericTraitEntityModel<NaemonInstance, string> naemonInstanceModel;
        private readonly GenericTraitEntityModel<Host, string> hostModel;
        private readonly GenericTraitEntityModel<Service, string> serviceModel;
        private readonly GenericTraitEntityModel<Entity.Category, string> categoryModel;

        private readonly GenericTraitEntityModel<HostAction, string> hostActionModel;
        private readonly GenericTraitEntityModel<ServiceAction, string> serviceActionModel;
        private readonly GenericTraitEntityModel<NaemonInstanceTag, string> naemonInstancesTagModel;
        private readonly GenericTraitEntityModel<NaemonProfile, string> naemonProfileModel;
        private readonly GenericTraitEntityModel<TimePeriod, string> timePeriodModel;
        private readonly GenericTraitEntityModel<Variable, string> variableModel;
        private readonly GenericTraitEntityModel<ServiceLayer, string> serviceLayerModel;
        private readonly GenericTraitEntityModel<Command, string> commandModel;
        private readonly GenericTraitEntityModel<ServiceStatic, long> serviceStaticModel;
        private readonly GenericTraitEntityModel<Module, string> moduleModel;
        private readonly GenericTraitEntityModel<Interface, string> interfaceModel;
        public NaemonConfig(ICIModel ciModel, ILayerModel layerModel, IEffectiveTraitModel traitModel, IRelationModel relationModel, IAttributeModel attributeModel,
                           GenericTraitEntityModel<NaemonInstance, string> naemonInstanceModel,
                           GenericTraitEntityModel<Service, string> serviceModel,
                           GenericTraitEntityModel<Entity.Category, string> categoryModel,
                           GenericTraitEntityModel<HostAction, string> hostActionModel,
                           GenericTraitEntityModel<ServiceAction, string> serviceActionModel,
                           GenericTraitEntityModel<NaemonInstanceTag, string> naemonInstancesTagModel,
                           GenericTraitEntityModel<NaemonProfile, string> naemonProfileModel,
                           GenericTraitEntityModel<TimePeriod, string> timePeriodModel,
                           GenericTraitEntityModel<Variable, string> variableModel,
                           GenericTraitEntityModel<ServiceLayer, string> serviceLayerModel,
                           GenericTraitEntityModel<Command, string> commandModel,
                           GenericTraitEntityModel<ServiceStatic, long> serviceStaticModel,
                           GenericTraitEntityModel<Module, string> moduleModel,
                           GenericTraitEntityModel<Interface, string> interfaceModel,
                           GenericTraitEntityModel<Host, string> hostModel,
                           ILatestLayerChangeModel latestLayerChangeModel) : base(latestLayerChangeModel)
        {
            this.ciModel = ciModel;
            this.relationModel = relationModel;
            this.traitModel = traitModel;
            this.layerModel = layerModel;
            this.attributeModel = attributeModel;

            this.naemonInstanceModel = naemonInstanceModel;
            this.hostModel = hostModel;
            this.serviceModel = serviceModel;

            this.categoryModel = categoryModel;

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
            this.interfaceModel = interfaceModel;
        }

        public override async Task<bool> Run(Layer targetLayer, JObject config, IChangesetProxy changesetProxy, IModelContext trans, ILogger logger)
        {
            logger.LogDebug("Start naemonConfig");

            //return false;

            Configuration cfg = new();

            try
            {
                cfg = config.ToObject<Configuration>();
                logger.LogDebug("Parsed successfully configuration for naemon config compute layer.");
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
            //var naemonInstances = await naemonInstanceModel.GetAllByDataID(layersetMonman, trans, changesetProxy.TimeThreshold);

            var naemonInstances = await naemonInstanceModel.GetAllByCIID(layersetMonman, trans, changesetProxy.TimeThreshold);

            logger.LogInformation("Loaded all naemon instances.");


            logger.LogInformation("Started creating configuration items.");
            // a list with all CI from database
            var ciData = new List<ConfigurationItem>();


            // load all categories
            var allCategories = await categoryModel.GetAllByCIID(layersetCMDB, trans, changesetProxy.TimeThreshold);

            // load all interfaces
            var allInterfaces = await interfaceModel.GetAllByCIID(layersetCMDB, trans, changesetProxy.TimeThreshold);

            var hosts = await hostModel.GetAllByDataID(layersetCMDB, trans, changesetProxy.TimeThreshold);
            logger.LogInformation("Loaded all hosts.");

            foreach (var ciItem in hosts)
            {
                var ciCategories = allCategories.Where(c => ciItem.Value.CategoriesIds.ToList().Contains(c.Key)).ToList();

                var cat = new Dictionary<string, List<Category>>();
                foreach (var (_, ciCategory) in ciCategories)
                {

                    var obj = new Category
                    {
                        Id = ciCategory.Id,
                        Tree = ciCategory.CatTree,
                        Group = ciCategory.CatGroup,
                        Name = ciCategory.Cat,
                        Desc = ciCategory.CatDesc,
                    };


                    if (!cat.ContainsKey(obj.Group))
                    {
                        cat.Add(obj.Group, new List<Category> { obj });
                    }
                    else
                    {
                        cat[obj.Group].Add(obj);
                    }
                }

                var ciInterfaces = allInterfaces.Where(c => ciItem.Value.InterfacesIds.ToList().Contains(c.Key)).ToList();
                var interfaces = new List<InterfaceObj>();

                foreach (var (_,item) in ciInterfaces)
                {
                    var obj = new InterfaceObj
                    {
                        DNSName = item.DNSName,
                        Id = item.Id,
                        IP = item.IP,
                        LANType = item.LanType,
                        Type = item.Type,
                        Name = item.Name,
                    };

                    interfaces.Add(obj);
                }

                ciData.Add(new ConfigurationItem
                {
                    Type = "HOST",
                    Id = ciItem.Value.Id,
                    Name = ciItem.Value.Name,
                    Status = ciItem.Value.Status,
                    Address = ciItem.Value.Address,
                    Port = ciItem.Value.Port != "" ? int.Parse(ciItem.Value.Port) : null,
                    Cust = ciItem.Value.Cust, 
                    Criticality = ciItem.Value.Criticality,
                    SuppOS = "", // Add SuppOS for this ci,
                    SuppApp = "", // Add SuppApp for this ci,W
                    Categories = cat,
                    Interfaces = interfaces,

                });
            }

            // get services
            var services = await serviceModel.GetAllByDataID(layersetCMDB, trans, changesetProxy.TimeThreshold);
            logger.LogInformation("Loaded all services.");

            foreach (var ciItem in services)
            {
                var ciCategories = allCategories.Where(c => ciItem.Value.CategoriesIds.ToList().Contains(c.Key)).ToList();

                var cat = new Dictionary<string, List<Category>>();
                foreach (var (_, ciCategory) in ciCategories)
                {
                    var obj = new Category
                    {
                        Id = ciCategory.Id,
                        Tree = ciCategory.CatTree,
                        Group = ciCategory.CatGroup,
                        Name = ciCategory.Cat,
                        Desc = ciCategory.CatDesc,
                    };

                    if (!cat.ContainsKey(obj.Group))
                    {
                        cat.Add(obj.Group, new List<Category> { obj });
                    }
                    else
                    {
                        cat[obj.Group].Add(obj);
                    }
                }

                var ciInterfaces = allInterfaces.Where(c => ciItem.Value.InterfacesIds.ToList().Contains(c.Key)).ToList();
                var interfaces = new List<InterfaceObj>();

                foreach (var (_, item) in ciInterfaces)
                {
                    var obj = new InterfaceObj
                    {
                        DNSName = item.DNSName,
                        Id = item.Id,
                        IP = item.IP,
                        LANType = item.LanType,
                        Type = item.Type,
                        Name = item.Name,
                    };

                    interfaces.Add(obj);
                }

                ciData.Add(new ConfigurationItem
                {
                    Type = "SERVICE",
                    Id = ciItem.Value.Id,
                    Name = ciItem.Value.Name,
                    Status = ciItem.Value.Status,
                    Environment = ciItem.Value.Environment,
                    Address = ciItem.Value.Address,
                    Port = ciItem.Value.Port != "" ? int.Parse(ciItem.Value.Port) : null,
                    Cust = ciItem.Value.Cust,
                    Criticality = ciItem.Value.Criticality,
                    SuppOS = "", // Add SuppOS for this ci,
                    SuppApp = "", // Add SuppApp for this ci,
                    Categories = cat,
                    Interfaces = interfaces,

                });
            }

            #region add actions
            // add host actions to cidata
            var hostActions = await hostActionModel.GetAllByDataID(layersetCMDB, trans, changesetProxy.TimeThreshold);
            //NOTE mcsuk: the same as above for hosts + categories goes here
            //NOTE mk: in the cmdb import of the data there is no relation between actions and hosts

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
            //NOTE mk: in the cmdb import of the data there is no relation between actions and services
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


            #region process core data

            logger.LogInformation("Started processing core data.");

            // Update normalized CiData field Profile
            Helper.CIData.UpdateProfileField(ciData, cfg!.CMDBMonprofilePrefix);
            logger.LogInformation("Finished updating profile field => UpdateNormalizedCiDataFieldProfile.");

            // updateNormalizedCiDataFieldAddress
            // NOTE: this part is done directly when selecting hosts and serices

            // updateNormalizedCiData_addGenericCmdbCapTags
            Helper.CIData.AddGenericCmdbCapTags(ciData);
            logger.LogInformation("Finished adding cap tags => updateNormalizedCiData_addGenericCmdbCapTags.");

            // updateNormalizedCiData_addRelationData
            var allRunsOnRelations = await relationModel.GetMergedRelations(RelationSelectionWithPredicate.Build("runs_on"), layersetCMDB, trans, changesetProxy.TimeThreshold);
            var fromCIIDs = allRunsOnRelations.Select(relation => relation.Relation.FromCIID).ToHashSet();
            var fromCIs = (await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(fromCIIDs), layersetCMDB!, false, NamedAttributesSelection.Build("cmdb.id"), trans, changesetProxy.TimeThreshold)).ToDictionary(ci => ci.ID);

            var toCIIds = allRunsOnRelations.Select(relation => relation.Relation.ToCIID).ToHashSet();
            var toCIs = (await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(toCIIds), layersetCMDB!, false, NamedAttributesSelection.Build("cmdb.id"), trans, changesetProxy.TimeThreshold)).ToDictionary(ci => ci.ID);

            var cmdbRelationsBySrc = new Dictionary<string, (string, string)>();

            foreach (var item in allRunsOnRelations)
            {
                MergedCI fromCI;
                if (!fromCIs.ContainsKey(item!.Relation.FromCIID))
                {
                    continue;
                }
                fromCI = fromCIs[item!.Relation.FromCIID];

                var fromCIID = (fromCI.MergedAttributes["cmdb.service.id"].Attribute.Value as AttributeScalarValueText)?.Value;

                var cfgObj = ciData.Where(el => el.Type == "SERVICE" && el.Id == fromCIID).FirstOrDefault();

                if (cfgObj != null)
                {
                    //var targetCI = await ciModel.GetMergedCI(item.Relation.ToCIID, layersetCMDB!, AllAttributeSelection.Instance, trans, changesetProxy.TimeThreshold);
                    var targetCI = toCIs[item!.Relation.FromCIID];
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
                    if (cmdbRelationsBySrc.ContainsKey(ciItem.Id))
                    {
                       
                    }
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

            var naemonsCis = new Dictionary<Guid, List<ConfigurationItem>>();
            foreach (var item in naemonInstances) 
            {
                naemonsCis.Add(item.Key, new List<ConfigurationItem>());
                foreach (var ciItem in ciData)
                {
                    if (ciItem.NaemonsAvail.Contains(item.Value.Id))
                    {
                        naemonsCis[item.Key].Add(ciItem);
                    }
                }
            }
            #endregion

            #region generate configs foreach naemon instance
            // first create new objects
            // NOTE try to remove this 
            var naemonConfigObjs = new Dictionary<string, List<ConfigObj>>();


            var fragments = new List<BulkCIAttributeDataLayerScope.Fragment>();

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

                //naemonConfigObjs.Add(item.Key, naemonObjs.Concat(configObjs).ToList());

                fragments.Add(new BulkCIAttributeDataLayerScope.Fragment("config", AttributeScalarValueJSON.Build(JArray.FromObject(naemonObjs.Concat(configObjs).ToList())), item.Key));
                //naemonConfigObjs.Add(item.Key, configObjs);

                // TODO: we also need to process deployed cis for this naemon, check applib-confgen-ci.php#74

            }
            #endregion


            await attributeModel.BulkReplaceAttributes(
                new BulkCIAttributeDataLayerScope("", targetLayer.ID, fragments),
                changesetProxy,
                new DataOriginV1(DataOriginType.ComputeLayer),
                trans,
                MaskHandlingForRemovalApplyNoMask.Instance);

            // convert into jobjects

            //var jobjects = new Dictionary<string, JObject>();

            //foreach (var item in naemonConfigObjs)
            //{
            //    //var ci = await ciModel.CreateCI(trans);

            //    var s1 = JsonConvert.SerializeObject(item.Value);
            //    var ss = JArray.FromObject(item.Value);

            //    if (item.Key == "H12037680")
            //    {
            //        var a = 5;
            //    }

            //    //var (attribute, changed) = await attributeModel.InsertAttribute("config", AttributeScalarValueJSON.Build(ss), ci, "naemon_config", changesetProxy, new DataOriginV1(DataOriginType.Manual), trans);
            //}

            // save the created configurations to the target layer

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
            public string Type { get; set; }
            public string Id { get; set; }
            public string Name { get; set; }
            public string Status { get; set; }
            public string Environment { get; set; }
            public string Profile { get; set; }
            public string Address { get; set; }
            public int? Port { get; set; }
            public string Cust { get; set; }
            public string Criticality { get; set; }
            public string SuppApp { get; set; }
            public string SuppOS { get; set; }
            public List<string> ProfileOrg { get; set; }
            public List<string> NaemonsAvail { get; set; }
            public Dictionary<string, List<Category>> Categories { get; set; }
            public List<string> Tags { get; set; }
            public Actions Actions { get; set; }
            public List<InterfaceObj> Interfaces { get; set; }
            public Dictionary<string, KeyValuePair<string, string>> Relations { get; set; }
            public string EffectiveHostCI { get; set; }
            public Dictionary<string, string> Vars { get; set; }

            // NOTE fill this data with all columns from database
            public Dictionary<string, string> CmdbData { get; set; }
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
                Interfaces = new List<InterfaceObj>();
                Relations = new Dictionary<string, KeyValuePair<string, string>>();
                Categories = new Dictionary<string, List<Category>>();
                Actions = new Actions();
                Tags = new List<string>();
                ProfileOrg = new List<string>();
                NaemonsAvail = new List<string>();
                Vars = new Dictionary<string, string>();
                CmdbData = new Dictionary<string, string>();
            }
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

        public class InterfaceObj
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public string LANType { get; set; }
            public string Name { get; set; }
            public string IP { get; set; }
            public string DNSName { get; set; }
            public string Vlan { get; set; }

            public InterfaceObj()
            {
                Id = "";
                Type = "";
                LANType = "";
                Name = "";
                IP = "";
                DNSName = "";
                Vlan = "";
            }
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
