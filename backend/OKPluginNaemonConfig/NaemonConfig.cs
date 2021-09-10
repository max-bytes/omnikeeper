using Microsoft.Extensions.Logging;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
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


            var naemonIds = new List<string>();

            var naemonModules = await traitModel.GetEffectiveTraitsForTrait(Traits.NaemonModulesFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            //$profileFromDbNaemons = [];
            // get db enabled naemons
            //var profileFromDbNaemons = new List<MergedCI>();

            var profileFromDbNaemons = new List<string>();

            foreach (var naemon in naemonInstances)
            {
                // we need to check here if isNaemonProfileFromDbEnabled 
                (MergedCI ci, EffectiveTrait et) = naemon.Value;

                var success = ci.MergedAttributes.TryGetValue("monman-instance.name", out MergedCIAttribute? instanceNameAttribute);

                if (!success)
                {
                    continue;
                }

                var instanceName = instanceNameAttribute!.Attribute.Value.Value2String();

                if (naemonsConfigGenerateprofiles.Contains(instanceName))
                {
                    //profileFromDbNaemons.Add(ci);

                    // monman-instance.id
                    var s = ci.MergedAttributes.TryGetValue("monman-instance.id", out MergedCIAttribute? instanceIdAttribute);

                    if (!s)
                    {
                        continue;
                    }

                    profileFromDbNaemons.Add(instanceIdAttribute!.Attribute.Value.Value2String());

                }
            }

            // getCapabilityMap - NaemonInstancesTagsFlattened

            var naemonInstancesTags = await traitModel.GetEffectiveTraitsForTrait(Traits.NaemonInstancesTagsFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            var capMap = new Dictionary<string, List<string>>();

            foreach (var instanceTag in naemonInstancesTags)
            {
                (MergedCI ci, _) = instanceTag.Value;

                var success = ci.MergedAttributes.TryGetValue("monman-instance_tag.tag", out MergedCIAttribute? instanceTagAttribute);

                if (!success)
                {
                    continue;
                }

                var tag = instanceTagAttribute!.Attribute.Value.Value2String();

                if (tag.StartsWith("cap_"))
                {
                    var s = ci.MergedAttributes.TryGetValue("monman-instance_tag.tag", out MergedCIAttribute? instanceIdAttribute);
                    if (!s)
                    {
                        continue;
                    }

                    capMap.Add(tag, new List<string> { instanceIdAttribute!.Attribute.Value.Value2String() });
                }
            }

            var naemonProfiles = await traitModel.GetEffectiveTraitsForTrait(Traits.NaemonProfilesFlattened, layerset, new AllCIIDsSelection(), trans, changesetProxy.TimeThreshold);

            /* extend capMap */

            if (profileFromDbNaemons.Count > 0)
            {
                foreach (var profile in naemonProfiles)
                {
                    (MergedCI ci, _) = profile.Value;
                    // first get profile name 
                    var success = ci.MergedAttributes.TryGetValue("monman-profile.name", out MergedCIAttribute? profileNameAttribute);

                    if (!success)
                    {
                        continue;
                    }

                    //$cap = 'cap_lp_'.strtolower($profile['NAME']);

                    var cap = "cap_lp_" + profileNameAttribute!.Attribute.Value.Value2String();

                    if (!capMap.ContainsKey(cap))
                    {
                        capMap.Add(cap, new List<string>());
                    }

                    capMap[cap] = (List<string>)profileFromDbNaemons.Concat(new List<string> { cap });
                }
            }

            /* test compatibility of naemons and add NAEMONSAVAIL */



            return true;
        }
    }
}
