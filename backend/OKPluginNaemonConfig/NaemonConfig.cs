using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
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
        private List<string> naemonsConfigGenerateprofiles = new List<string>(){ "svphg200mon001", "svphg200mon002", "uansvclxnaemp01", "uansvclxnaemp02", "uansvclxnaemp03", "uansvclxnaemp04", "uansvclxnaemp05", "uansvclxnaemp06" };


        public override async Task<bool> Run(Layer targetLayer, IChangesetProxy changesetProxy, CLBErrorHandler errorHandler, IModelContext trans, ILogger logger) 
        {
            logger.LogDebug("Start naemonConfig");

            var layerset = await layerModel.BuildLayerSet(new[] { "testlayer01" }, trans);

            var layerset02 = await layerModel.BuildLayerSet(new[] { "testlayer02" }, trans);

            // load naemonInstances
            var naemonInstances = await traitModel.GetEffectiveTraitsForTrait(Traits.NaemonInstanceFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);
            var naemonIds = new List<string>();
            foreach (var naemon in naemonInstances)
            {
                (MergedCI ciItem, _) = naemon.Value;
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

            var cisData = new List<MergedCI>();

            // get hosts from db
            //traitModel.get
            var hosts = await traitModel.GetEffectiveTraitsForTrait(Traits.HCisFlattened, layerset02, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            var a = await traitModel.GetMergedCIsWithTrait(Traits.HCisFlattened, layerset02, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            foreach (var host in hosts)
            {
                (MergedCI ciItem, _) = host.Value;
    //            attribute('hostname', HOSTNAME || ''),
				//attribute('__name', join(' - ', [HOSTID || '', HOSTNAME || ''])),
				//attribute('cmdb.id', HOSTID || ''),
				//attribute('cmdb.platform', HPLATFORM || ''),
				//attribute('cmdb.status', HSTATUS || ''),
				//attribute('cmdb.more', HMORE || ''),
				//attribute('cmdb.customer', HCUST || ''),
				//attribute('cmdb.cpu', HCPU || ''),
				//attribute('cmdb.os', HOS || ''),
				//attribute('cmdb.fsource', HFSOURCE || ''),
				//attribute('cmdb.fkey', HFKEY || '')
                //                $ci['TYPE'] = 'HOST';
                //$ci['ID'] = $v['HOSTID'];
                //$ci['NAME'] = $v['HOSTNAME'];

                //$ci['CUST'] = $v['HCUST'];
                //$ci['ENVIRONMENT'] = $v['HENVIRONMENT'];
                //$ci['STATUS'] = $v['HSTATUS'];
                //$ci['CRITICALITY'] = $v['HCRITICALITY'];

                //$ci['SUPP_OS'] = $v['HSUP'];
                //$ci['SUPP_APP'] = $v['HSUPAPP'];
                var attributes = new Dictionary<string, MergedCIAttribute>();
                //attributes.Add("TYPE", new MergedCIAttribute())
                foreach (var attribute in ciItem.MergedAttributes)
                {
                    switch (attribute.Key)
                    {
                        case "cmdb.id":
                            attributes["ID"] = attribute.Value;
                            break;
                        case "hostname":
                            attributes["NAME"] = attribute.Value;
                            break;
                        case "cmdb.status":
                            attributes["STATUS"] = attribute.Value;
                            break;
                        default:
                            break;
                    }
                }
                ciItem.MergedAttributes.Clear();
                foreach (var attribute in attributes)
                {
                    ciItem.MergedAttributes.AddOrUpdate(attribute.Key, attribute.Value);
                }
                // before inserting here we should define that type is host
                cisData.Append(ciItem); // we need to define an object for CI that has type and other properties
            }

            // select only  one host
            var t = await traitModel.GetEffectiveTraitsWithTraitAttributeValue(Traits.HCisFlattened, "hostname", new AttributeScalarValueText("nb11644015"), layerset02, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            // get services
            var services = await traitModel.GetEffectiveTraitsForTrait(Traits.ACisFlattened, layerset02, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);
            foreach (var service in services)
            {
                (MergedCI ciItem, _) = service.Value;

                // before inserting here we should define that type is service
                //$ci['TYPE'] = 'SERVICE';
                //$ci['ID'] = $v['SVCID'];
                //$ci['NAME'] = $v['SVCNAME'];

                //$ci['CUST'] = $v['SVCCUSTOMER'];
                //$ci['ENVIRONMENT'] = $v['SVCENVIRONMENT'];
                //$ci['STATUS'] = $v['SVCSTATUS'];
                //$ci['CRITICALITY'] = $v['SVCCRITICALITY'];

                //$ci['SUPP_OS'] = $v['SVCSUP'];
                //$ci['SUPP_APP'] = $v['SVCSUPAPP'];

    //            attribute('cmdb.name', SVCNAME || ''),
				//attribute('__name', join(' - ', [SVCID || '', SVCNAME || ''])),
				//attribute('cmdb.id', SVCID || ''),
				//attribute('cmdb.environment', SVCENVIRONMENT || ''),
				//attribute('cmdb.status', SVCSTATUS || ''),
				//attribute('cmdb.class', SVCCLASS || ''),
				//attribute('cmdb.type', SVCTYPE || ''),
				//attribute('cmdb.detail', SVCDETAIL || ''),
				//attribute('cmdb.fsource', SVCFSOURCE || ''),
				//attribute('cmdb.fkey', SVCFKEY || '')

                var attributes = new Dictionary<string, MergedCIAttribute>();

                foreach (var attribute in ciItem.MergedAttributes)
                {
                    switch (attribute.Key)
                    {
                        case "cmdb.id":
                            attributes["ID"] = attribute.Value;
                            break;
                        case "cmdb.name":
                            attributes["NAME"] = attribute.Value;
                            break;
                        case "cmdb.environment":
                            attributes["ENVIRONMENT"] = attribute.Value;
                            break;
                        case "cmdb.status":
                            attributes["STATUS"] = attribute.Value;
                            break;
                        default:
                            break;
                    }
                }
                ciItem.MergedAttributes.Clear();
                foreach (var attribute in attributes)
                {
                    ciItem.MergedAttributes.AddOrUpdate(attribute.Key, attribute.Value);
                }
                
                //ciItem.MergedAttributes.Concat(attributes);
                cisData.Add(ciItem); // we need to define an object for CI that has type and other properties, maybe add a new attribute here
            }

            // get capability map
            // then check profiles from db

            //getCapabilityMap - NaemonInstancesTagsFlattened

            var naemonInstancesTags = await traitModel.GetEffectiveTraitsForTrait(Traits.NaemonInstancesTagsFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            var capMap = new Dictionary<string, List<string>>();

            foreach (var instanceTag in naemonInstancesTags)
            {
                (MergedCI ciItem, _) = instanceTag.Value;

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
                    } else
                    {
                        capMap.Add(tag, new List<string> { instanceIdAttribute!.Attribute.Value.Value2String() });
                    }
                }
            }

            var naemonProfiles = await traitModel.GetEffectiveTraitsForTrait(Traits.NaemonProfilesFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            var profileFromDbNaemons = new List<string>();

            foreach (var naemon in naemonInstances)
            {
                // we need to check here if isNaemonProfileFromDbEnabled 
                (MergedCI ciItem, _) = naemon.Value;

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
                foreach (var profile in naemonProfiles)
                {
                    (MergedCI ciItem, _) = profile.Value;
                    
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
                    } else
                    {
                        capMap[cap] = (List<string>)profileFromDbNaemons.Concat(capMap[cap]);
                    }
                }
            }

            // create for each CI an json object so we can have more flexibility
            foreach (var ciItem in cisData)
            {
                // we need to add the naemonsVail to each configuration item
                var naemonsVail = naemonIds;

                // we need to check here ci tags which should be an attribute
                foreach (var instanceTag in naemonInstancesTags)
                {
                    (MergedCI item, _) = instanceTag.Value;

                    var s = item.MergedAttributes.TryGetValue("monman-instance.name", out MergedCIAttribute? instanceNameAttribute);

                    if (!s)
                    {
                        continue;
                    }

                    var requirement = instanceNameAttribute!.Attribute.Value.Value2String();

                    if (capMap.ContainsKey(requirement))
                    {
                        naemonsVail = naemonsVail.Intersect(capMap[requirement]).ToList();
                    }
                    else
                    {
                        naemonsVail = new List<string>();
                    }
                }
            }


            var commands = await traitModel.GetEffectiveTraitsWithTraitAttributeValue(Traits.CommandsFlattened, "monman-command.id", new AttributeScalarValueText("H12037680"), layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);
            // load services ci

            // foreach customer from loadcmdbcustomer we need to take data for hosts and services


            // now foreach naemonInstance we neeed to get the configuration items based on id
            // we do this check by comapring if nemonId is in naemonsVail list

            return true;
        }

        // Do we need this object
        class CIItem
        {
            public List<string>? NAEMONSAVAIL { get; set; }

        }

        public async Task<bool> RunV1(Layer targetLayer, IChangesetProxy changesetProxy, CLBErrorHandler errorHandler, IModelContext trans, ILogger logger)
        {
            logger.LogDebug("Start naemonConfig");

            var layerset = await layerModel.BuildLayerSet(new[] { "testlayer01" }, trans);

            var layerset02 = await layerModel.BuildLayerSet(new[] { "testlayer02" }, trans);

            var naemonInstances = await traitModel.GetEffectiveTraitsForTrait(Traits.NaemonInstanceFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            //foreach (var instance in naemonInstances)
            //{
            //    //$id = $agent['ID'];
            //    var ci = instance.Value.ci;
            //    var instanceIdattrName = "monman-instance.id";

            //    var agent = ci.MergedAttributes;

            //    MergedCIAttribute id;

            //    var success = ci.MergedAttributes.TryGetValue(instanceIdattrName, out id!);

            //    if (!success)
            //    {
            //        // log an error here
            //        break;
            //    }

            //    var naemonInstanceId = id.Attribute.Value.Value2String();

            //    var ciData = new List<TraitAttribute>();
            //    // $generateConfig and $isSpecialNode

            //    // get hosts and service traits

            //    var hCis = await traitModel.GetEffectiveTraitsForTrait(Traits.HCisFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);
            //    var aCis = await traitModel.GetEffectiveTraitsForTrait(Traits.ACisFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            //    // we need to filter hCis and aCis based on loadcmdbcustomer list 
            //    // convert these two lists to one ciData list
            //    var hMergedCis = new List<MergedCI>();
            //    var aMergedCis = new List<MergedCI>();

            //    foreach (var el in hCis)
            //    {
            //        (MergedCI elCi, _) = el.Value;

            //        var s = elCi.MergedAttributes.TryGetValue("cmdb.customer", out MergedCIAttribute? cmdbCustomer);

            //        // check if cmd.customer is in the loadcmdbcustomer list
            //        var v = cmdbCustomer!.Attribute.Value.Value2String();
            //        if (s && loadcmdbcustomer.Contains(v!))
            //        {
            //            hMergedCis.Add(elCi); // now this list contins filtered mergedCis  based on loadcmdbcustomer list
            //        }
            //    }

            //    foreach (var el in aCis)
            //    {
            //        (MergedCI elCi, _) = el.Value;
            //        var s = elCi.MergedAttributes.TryGetValue("SVCCUSTOMER", out MergedCIAttribute? svcCustomer); // this attribute looks like is missing after the data is transformed to omnikeeper?
            //        var v = svcCustomer!.Attribute.Value.Value2String();
            //        if (s && loadcmdbcustomer.Contains(v!))
            //        {
            //            aMergedCis.Add(elCi); // now this list contins filtered mergedCis  based on loadcmdbcustomer list
            //        }
            //    }

            //    // now we need to check what addToNormalizedCiDataFromBaseData() function returns

            //    // now we have ci data that is returned by getNormalizedCiBaseData() function on php project

            //}

            // select naemon instance with specific ID 'H12037680'
            // this is for intial version of creating naemon configuration
            // at the end we will have a loop that loops through naemoninstances list and selects th
            var naemonH12037680 = await traitModel.GetEffectiveTraitsWithTraitAttributeValue(Traits.NaemonInstanceFlattened, "monman-instance.id", new AttributeScalarValueText("H12037680"), layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            // get all host data for this naemon id 'H12037680' 
            var hCiH12037680 = await traitModel.GetEffectiveTraitsWithTraitAttributeValue(Traits.HCisFlattened, "hostname", new AttributeScalarValueText("H12037680"), layerset02, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);
            
            
            var hCis = await traitModel.GetEffectiveTraitsForTrait(Traits.HCisFlattened, layerset02, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);


            var naemonIds = new List<string>();

            var naemonModules = await traitModel.GetEffectiveTraitsForTrait(Traits.NaemonModulesFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            //traitModel.GetEffectiveTraitsWithTraitAttributeValue


            //$profileFromDbNaemons = [];
            // get db enabled naemons
            //var profileFromDbNaemons = new List<MergedCI>();

            var profileFromDbNaemons = new List<string>();

            //foreach (var naemon in naemonInstances)
            //{
            //    // we need to check here if isNaemonProfileFromDbEnabled 
            //    (MergedCI ci, EffectiveTrait et) = naemon.Value;

            //    var success = ci.MergedAttributes.TryGetValue("monman-instance.name", out MergedCIAttribute? instanceNameAttribute);

            //    if (!success)
            //    {
            //        continue;
            //    }

            //    var instanceName = instanceNameAttribute!.Attribute.Value.Value2String();

            //    if (naemonsConfigGenerateprofiles.Contains(instanceName))
            //    {
            //        //profileFromDbNaemons.Add(ci);

            //        // monman-instance.id
            //        var s = ci.MergedAttributes.TryGetValue("monman-instance.id", out MergedCIAttribute? instanceIdAttribute);

            //        if (!s)
            //        {
            //            continue;
            //        }

            //        profileFromDbNaemons.Add(instanceIdAttribute!.Attribute.Value.Value2String());

            //    }
            //}

            // getCapabilityMap - NaemonInstancesTagsFlattened

            //var naemonInstancesTags = await traitModel.GetEffectiveTraitsForTrait(Traits.NaemonInstancesTagsFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            //var capMap = new Dictionary<string, List<string>>();

            //foreach (var instanceTag in naemonInstancesTags)
            //{
            //    (MergedCI ci, _) = instanceTag.Value;

            //    var success = ci.MergedAttributes.TryGetValue("monman-instance_tag.tag", out MergedCIAttribute? instanceTagAttribute);

            //    if (!success)
            //    {
            //        continue;
            //    }

            //    var tag = instanceTagAttribute!.Attribute.Value.Value2String();

            //    if (tag.StartsWith("cap_"))
            //    {
            //        var s = ci.MergedAttributes.TryGetValue("monman-instance_tag.tag", out MergedCIAttribute? instanceIdAttribute);
            //        if (!s)
            //        {
            //            continue;
            //        }

            //        capMap.Add(tag, new List<string> { instanceIdAttribute!.Attribute.Value.Value2String() });
            //    }
            //}

            //var naemonProfiles = await traitModel.GetEffectiveTraitsForTrait(Traits.NaemonProfilesFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            /* extend capMap */

            //if (profileFromDbNaemons.Count > 0)
            //{
            //    foreach (var profile in naemonProfiles)
            //    {
            //        (MergedCI ci, _) = profile.Value;
            //        // first get profile name 
            //        var success = ci.MergedAttributes.TryGetValue("monman-profile.name", out MergedCIAttribute? profileNameAttribute);

            //        if (!success)
            //        {
            //            continue;
            //        }

            //        //$cap = 'cap_lp_'.strtolower($profile['NAME']);

            //        var cap = "cap_lp_" + profileNameAttribute!.Attribute.Value.Value2String();

            //        if (!capMap.ContainsKey(cap))
            //        {
            //            capMap.Add(cap, new List<string>());
            //        }

            //        capMap[cap] = (List<string>)profileFromDbNaemons.Concat(new List<string> { cap });
            //    }
            //}

            /* test compatibility of naemons and add NAEMONSAVAIL */



            return true;
        }
    }
}
