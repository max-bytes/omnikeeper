using OKPluginNaemonConfig.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using static OKPluginNaemonConfig.NaemonConfig;

namespace OKPluginNaemonConfig.Helper
{
    public static partial class ConfigObjects
    {
        public static void GetNaemonConfigObjectsFromLegacyProfilesModules(
            List<ConfigObj> configObjs,
            IDictionary<string, ServiceLayer> serviceLayers,
            IDictionary<string, Command> commands,
            IDictionary<string, TimePeriod> timeperiods, // ?? timeperiodsById
            IDictionary<long, ServiceStatic> servicesStatic,
            IDictionary<string, Module> modules

            )
        {
            // NOTE - we need to refactor this, in cases when ids are selected correctly we dont need to create this dictionary

            var layersById = new Dictionary<string, ServiceLayer>();
            foreach (var ciItem in serviceLayers)
            {
                //var layerId = ciItem.MergedAttributes["naemon_service_layer.id"]!.Attribute.Value.Value2String();

                layersById.Add(ciItem.Value.Id.ToString(), ciItem.Value);
            }


            // getCommands -> We need only command ids here
            // NOTE - same as above
            var commandsById = new Dictionary<string, Command>();
            foreach (var ciItem in commands)
            {
                //var commandId = ciItem.MergedAttributes["naemon_command.id"]!.Attribute.Value.Value2String();

                //commandsById.Add(commandId, ciItem);
                commandsById.Add(ciItem.Value.Id.ToString(), ciItem.Value);
            }

            // getTimeperiods -> We need a list with timeperiod ids

            // NOTE - same here
            var timeperiodsById = new Dictionary<string, TimePeriod>();
            foreach (var ciItem in timeperiods)
            {
                //var timeperiodId = ciItem.MergedAttributes["naemon_timeperiod.id"]!.Attribute.Value.Value2String();

                //timeperiodsById.Add(timeperiodId, ciItem);
                timeperiodsById.Add(ciItem.Value.Id.ToString(), ciItem.Value);
            }

            // prepare services
            // get SERVICES_STATIC
            var servicesStaticById = new Dictionary<string, ServiceStatic>();
            var servicesByModulesId = new Dictionary<string, List<ServiceStatic>>();
            foreach (var ciItem in servicesStatic)
            {
                //var id = ciItem.MergedAttributes["naemon_services_static.id"]!.Attribute.Value.Value2String();

                servicesStaticById.Add(ciItem.Value.Id.ToString(), ciItem.Value);

                //var module = ciItem.MergedAttributes["naemon_services_static.module"]!.Attribute.Value.Value2String();

                if (servicesByModulesId.ContainsKey(ciItem.Value.Module.ToString()))
                {
                    servicesByModulesId[ciItem.Value.Module.ToString()].Add(ciItem.Value);
                }
                else
                {
                    servicesByModulesId[ciItem.Value.Module.ToString()] = new List<ServiceStatic> { ciItem.Value };
                }
            }



            foreach (var ciItem in modules)
            {
                //var moduleName = ciItem.MergedAttributes["naemon_module.name"]!.Attribute.Value.Value2String().ToLower();

                configObjs.Add(new ConfigObj
                {
                    Type = "host",
                    Attributes = new Dictionary<string, string>
                    {
                        ["name"] = "mod-" + ciItem.Value.Name,
                        ["hostgroups"] = "+" + ciItem.Value.Name,
                    },
                });

                configObjs.Add(new ConfigObj
                {
                    Type = "hostgroup",
                    Attributes = new Dictionary<string, string>
                    {
                        ["hostgroup_name"] = ciItem.Value.Name,
                        ["alias"] = ciItem.Value.Name,
                    },
                });

                //var moduleId = ciItem.MergedAttributes["naemon_module.id"]!.Attribute.Value.Value2String().ToLower();

                if (servicesByModulesId.ContainsKey(ciItem.Value.Id.ToString()))
                {
                    foreach (var service in servicesByModulesId[ciItem.Value.Id.ToString()])
                    {
                        // goto next if service is disabled
                        //var disabled = (service.MergedAttributes["naemon_services_static.disabled"].Attribute.Value as AttributeScalarValueInteger)?.Value;
                        if (service.Disabled > 0)
                        {
                            continue;
                        }

                        var attributes = new Dictionary<string, string>();
                        var vars = new Dictionary<string, string>();

                        var serviceTemplates = new List<string> { "tsa-generic-service" };

                        //var perf = (service.MergedAttributes["naemon_services_static.perf"].Attribute.Value as AttributeScalarValueInteger)?.Value;

                        if (service.Perf == 1)
                        {
                            serviceTemplates.Add("service-pnp");
                        }

                        attributes["use"] = string.Join(",", serviceTemplates);
                        attributes["hostgroup_name"] = ciItem.Value.Name;

                        // $layersById[$service['LAYER']]['NUM'] . ' ' . $service['SERVICENAME'];

                        //var svcLayer = (service.MergedAttributes["naemon_services_static.layer"].Attribute.Value as AttributeScalarValueInteger)?.Value;
                        //var layerNum = (layersById[svcLayer.ToString()].MergedAttributes["naemon_service_layer.num"].Attribute.Value as AttributeScalarValueInteger)?.Value;

                        var layerNum = layersById[service.Layer.ToString()]!.Num;

                        //var svcName = (service.MergedAttributes["naemon_services_static.servicename"].Attribute.Value as AttributeScalarValueText)?.Value;

                        attributes["service_description"] = layerNum + " " + service.ServiceName;


                        //var svcCheckCommand = (service.MergedAttributes["naemon_services_static.checkcommand"].Attribute.Value as AttributeScalarValueInteger)?.Value;
                        //var commandName = (commandsById[svcCheckCommand.ToString()].MergedAttributes["naemon_command.name"].Attribute.Value as AttributeScalarValueText)?.Value;

                        var commandName = commandsById[service.CheckCommand.ToString()].Name;
                        var commandParts = new List<string> { commandName };

                        //for (int i = 1; i <= 8; i++)
                        //{
                        //    var k = $"naemon_services_static.arg{i}";

                        //    if (service.MergedAttributes.TryGetValue(k, out MergedCIAttribute? attr))
                        //    {
                        //        var arg = (attr.Attribute.Value as AttributeScalarValueText)?.Value;

                        //        if (arg.Length > 0)
                        //        {
                        //            if (arg.Substring(0, 1) == "$")
                        //            {
                        //                arg = "$HOST" + arg.Substring(1, arg.Length - 1) + "$";
                        //            }

                        //            commandParts.Add(arg);
                        //        }
                        //    }
                        //}

                        if (service.Arg1.Length > 0)
                        {
                            if (service.Arg1.Substring(0, 1) == "$")
                            {
                                commandParts.Add("$HOST" + service.Arg1[1..] + "$");
                            }

                            commandParts.Add(service.Arg1);
                        }

                        if (service.Arg2.Length > 0)
                        {
                            if (service.Arg2.Substring(0, 1) == "$")
                            {
                                commandParts.Add("$HOST" + service.Arg2[1..] + "$");
                            }

                            commandParts.Add(service.Arg2);
                        }

                        if (service.Arg3.Length > 0)
                        {
                            if (service.Arg3.Substring(0, 1) == "$")
                            {
                                commandParts.Add("$HOST" + service.Arg3[1..] + "$");
                            }

                            commandParts.Add(service.Arg3);
                        }

                        if (service.Arg4.Length > 0)
                        {
                            if (service.Arg4.Substring(0, 1) == "$")
                            {
                                commandParts.Add("$HOST" + service.Arg4[1..] + "$");
                            }

                            commandParts.Add(service.Arg4);
                        }

                        if (service.Arg5.Length > 0)
                        {
                            if (service.Arg5.Substring(0, 1) == "$")
                            {
                                commandParts.Add("$HOST" + service.Arg5[1..] + "$");
                            }

                            commandParts.Add(service.Arg5);
                        }

                        if (service.Arg6.Length > 0)
                        {
                            if (service.Arg6.Substring(0, 1) == "$")
                            {
                                commandParts.Add("$HOST" + service.Arg6[1..] + "$");
                            }

                            commandParts.Add(service.Arg6);
                        }

                        if (service.Arg7.Length > 0)
                        {
                            if (service.Arg7.Substring(0, 1) == "$")
                            {
                                commandParts.Add("$HOST" + service.Arg7[1..] + "$");
                            }

                            commandParts.Add(service.Arg7);
                        }

                        if (service.Arg8.Length > 0)
                        {
                            if (service.Arg8.Substring(0, 1) == "$")
                            {
                                commandParts.Add("$HOST" + service.Arg8[1..] + "$");
                            }

                            commandParts.Add(service.Arg8);
                        }

                        attributes["check_command"] = string.Join("!", commandParts);

                        /* add other attributes */

                        //var freshnesThreshold = GetAttributeValueInt("naemon_services_static.freshness_threshold", service.MergedAttributes);

                        if (service.FreshnessThreshold != null)
                        {
                            attributes["check_freshness"] = "1";
                            attributes["freshness_threshold"] = service.FreshnessThreshold.ToString();
                        }


                        //var passive = GetAttributeValueInt("naemon_services_static.passive", service.MergedAttributes);
                        if (service.Passive != null && service.Passive == 1)
                        {
                            attributes["passive_checks_enabled"] = "1";
                            attributes["active_checks_enabled"] = "0";
                        }

                        //var checkInterval = GetAttributeValueInt("naemon_services_static.check_interval", service.MergedAttributes);
                        if (service.CheckInterval != 0)
                        {
                            attributes["active_checks_enabled"] = service.CheckInterval.ToString();
                        }

                        //var maxCheckAttempts = GetAttributeValueInt("naemon_services_static.max_check_attempts", service.MergedAttributes);
                        if (service.MaxCheckAttempts != 0)
                        {
                            attributes["max_check_attempts"] = service.MaxCheckAttempts.ToString();
                        }

                        //var retryInterval = GetAttributeValueInt("naemon_services_static.retry_interval", service.MergedAttributes);
                        if (service.RetryInterval != 0)
                        {
                            attributes["retry_interval"] = service.RetryInterval.ToString();
                        }

                        //var timeperiodCheck = GetAttributeValueInt("naemon_services_static.timeperiod_check", service.MergedAttributes);
                        if (timeperiodsById.ContainsKey(service.TimeperiodCheck.ToString()))
                        {
                            //var tName = (timeperiodsById[timeperiodCheck.ToString()].MergedAttributes["naemon_timeperiod.name"].Attribute.Value as AttributeScalarValueText)?.Value;

                            attributes["check_period"] = timeperiodsById[service.TimeperiodCheck.ToString()].Name;
                        }

                        //var timeperiodNotify = GetAttributeValueInt("naemon_services_static.timeperiod_notify", service.MergedAttributes);
                        if (timeperiodsById.ContainsKey(service.TimeperiodNotify.ToString()))
                        {
                            //var tName = (timeperiodsById[timeperiodNotify.ToString()].MergedAttributes["naemon_timeperiod.name"].Attribute.Value as AttributeScalarValueText)?.Value;

                            attributes["notification_period"] = timeperiodsById[service.TimeperiodNotify.ToString()].Name;
                        }

                        /* fill variables */
                        //var target = (service.MergedAttributes["naemon_services_static.target"].Attribute.Value as AttributeScalarValueText)?.Value;
                        vars["Tickettarget"] = service.Target;

                        //var checkPrio = (service.MergedAttributes["naemon_services_static.checkprio"].Attribute.Value as AttributeScalarValueText)?.Value;
                        vars["Fehlerart"] = service.Checkprio;

                        /* customer cockpit vars */
                        //var visibility = GetAttributeValueInt("naemon_services_static.monview_visibility", service.MergedAttributes);
                        //if (visibility != null)
                        //{
                        vars["Visibility"] = service.MonviewVisibility.ToString();
                        //}

                        //var kpiType = GetAttributeValueInt("naemon_services_static.kpi_avail", service.MergedAttributes);
                        //if (kpiType != null)
                        //{
                        //    vars["kpi_type"] = kpiType.ToString();
                        //}

                        vars["kpi_type"] = service.KPIAvail.ToString();

                        //var metricName = (service.MergedAttributes["naemon_services_static.metric_name"].Attribute.Value as AttributeScalarValueText)?.Value;
                        vars["metric_name"] = service.MetricName;

                        //var metricType = (service.MergedAttributes["naemon_services_static.metric_type"].Attribute.Value as AttributeScalarValueText)?.Value;
                        vars["metric_type"] = service.MetricType;

                        //var metricMin = GetAttributeValueInt("naemon_services_static.metric_min", service.MergedAttributes);
                        //if (metricMin != null)
                        //{
                        //    vars["metric_min"] = metricMin.ToString();
                        //}

                        vars["metric_min"] = service.MetricMin.ToString();

                        //var metricMax = GetAttributeValueInt("naemon_services_static.metric_max", service.MergedAttributes);
                        //if (metricMax != null)
                        //{
                        //    vars["metric_max"] = metricMax.ToString();
                        //}

                        vars["metric_max"] = service.MetricMax.ToString();

                        //var metricPerfkey = (service.MergedAttributes["naemon_services_static.metric_perfkey"].Attribute.Value as AttributeScalarValueText)?.Value;
                        vars["metric_perfkey"] = service.MetricPerfkey;

                        //var metricPerfunit = (service.MergedAttributes["naemon_services_static.metric_perfunit"].Attribute.Value as AttributeScalarValueText)?.Value;
                        vars["metric_perfunit"] = service.MetricPerfunit;

                        //var monviewWarn = (service.MergedAttributes["naemon_services_static.monview_warn"].Attribute.Value as AttributeScalarValueText)?.Value;
                        vars["monview_warn"] = service.MonviewWarn;

                        //var monviewCrit = (service.MergedAttributes["naemon_services_static.monview_crit"].Attribute.Value as AttributeScalarValueText)?.Value;
                        vars["monview_crit"] = service.MonviewCrit;

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

                        //var mastersvcCheck = GetAttributeValueInt("naemon_services_static.mastersvc_check", service.MergedAttributes);
                        if (service.MastersvcCheck != null && servicesStaticById.ContainsKey(service.MastersvcCheck.ToString()))
                        {
                            var masterService = servicesStaticById[service.MastersvcCheck.ToString()];
                            //var masterSvcName = (masterService.MergedAttributes["naemon_services_static.servicename"].Attribute.Value as AttributeScalarValueText)?.Value;

                            configObjs.Add(new ConfigObj
                            {
                                Type = "servicedependency",
                                Attributes = new Dictionary<string, string>
                                {
                                    ["hostgroup_name"] = ciItem.Value.Name,
                                    ["service_description"] = layerNum + " " + masterService.ServiceName,
                                    ["dependent_service_description"] = layerNum + " " + service.ServiceName,
                                    ["inherits_parent"] = "1",
                                    ["execution_failure_criteria"] = "w,u,c",
                                }
                            });
                        }

                        //var mastersvcNotify = GetAttributeValueInt("naemon_services_static.mastersvc_notify", service.MergedAttributes);
                        if (service.MastersvcNotify != null && servicesStaticById.ContainsKey(service.MastersvcNotify.ToString()))
                        {
                            var masterService = servicesStaticById[service.MastersvcNotify.ToString()];
                            //var masterSvcName = (masterService.MergedAttributes["naemon_services_static.servicename"].Attribute.Value as AttributeScalarValueText)?.Value;

                            configObjs.Add(new ConfigObj
                            {
                                Type = "servicedependency",
                                Attributes = new Dictionary<string, string>
                                {
                                    ["hostgroup_name"] = ciItem.Value.Name,
                                    ["service_description"] = layerNum + " " + masterService.ServiceName,
                                    ["dependent_service_description"] = layerNum + " " + service.ServiceName,
                                    ["inherits_parent"] = "1",
                                    ["execution_failure_criteria"] = "w,u,c",
                                }
                            });
                        }
                    }
                }
            }
        }
    }
}
