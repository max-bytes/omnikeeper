using OKPluginNaemonConfig.Entity;
using System.Collections.Generic;
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
                layersById.Add(ciItem.Value.Id.ToString(), ciItem.Value);
            }

            // getCommands -> We need only command ids here
            // NOTE - same as above
            var commandsById = new Dictionary<string, Command>();
            foreach (var ciItem in commands)
            {
                commandsById.Add(ciItem.Value.Id.ToString(), ciItem.Value);
            }

            // getTimeperiods -> We need a list with timeperiod ids

            // NOTE - same here
            var timeperiodsById = new Dictionary<string, TimePeriod>();
            foreach (var ciItem in timeperiods)
            {
                timeperiodsById.Add(ciItem.Value.Id.ToString(), ciItem.Value);
            }

            // prepare services
            // get SERVICES_STATIC
            var servicesStaticById = new Dictionary<string, ServiceStatic>();
            var servicesByModulesId = new Dictionary<string, List<ServiceStatic>>();
            foreach (var ciItem in servicesStatic)
            {
                servicesStaticById.Add(ciItem.Value.Id.ToString(), ciItem.Value);

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

                if (servicesByModulesId.ContainsKey(ciItem.Value.Id.ToString()))
                {
                    foreach (var service in servicesByModulesId[ciItem.Value.Id.ToString()])
                    {
                        // goto next if service is disabled
                        if (service.Disabled > 0)
                        {
                            continue;
                        }

                        var attributes = new Dictionary<string, string>();
                        var vars = new Dictionary<string, string>();

                        var serviceTemplates = new List<string> { "tsa-generic-service" };

                        if (service.Perf == 1)
                        {
                            serviceTemplates.Add("service-pnp");
                        }

                        attributes["use"] = string.Join(",", serviceTemplates);
                        attributes["hostgroup_name"] = ciItem.Value.Name;

                        var layerNum = layersById[service.Layer.ToString()]!.Num;

                        attributes["service_description"] = layerNum + " " + service.ServiceName;

                        var commandName = commandsById[service.CheckCommand.ToString()].Name;
                        var commandParts = new List<string> { commandName };

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

                        if (service.FreshnessThreshold != null)
                        {
                            attributes["check_freshness"] = "1";
                            attributes["freshness_threshold"] = service.FreshnessThreshold.ToString();
                        }

                        if (service.Passive == 1)
                        {
                            attributes["passive_checks_enabled"] = "1";
                            attributes["active_checks_enabled"] = "0";
                        }

                        if (service.CheckInterval != 0)
                        {
                            attributes["active_checks_enabled"] = service.CheckInterval.ToString();
                        }

                        if (service.MaxCheckAttempts != 0)
                        {
                            attributes["max_check_attempts"] = service.MaxCheckAttempts.ToString();
                        }

                        if (service.RetryInterval != 0)
                        {
                            attributes["retry_interval"] = service.RetryInterval.ToString();
                        }

                        if (timeperiodsById.ContainsKey(service.TimeperiodCheck.ToString()))
                        {
                            attributes["check_period"] = timeperiodsById[service.TimeperiodCheck.ToString()].Name;
                        }

                        if (timeperiodsById.ContainsKey(service.TimeperiodNotify.ToString()))
                        {
                            attributes["notification_period"] = timeperiodsById[service.TimeperiodNotify.ToString()].Name;
                        }

                        /* fill variables */
                        vars["Tickettarget"] = service.Target;

                        vars["Fehlerart"] = service.Checkprio;

                        /* customer cockpit vars */

                        vars["Visibility"] = service.MonviewVisibility.ToString();

                        vars["kpi_type"] = service.KPIAvail.ToString();

                        vars["metric_name"] = service.MetricName;

                        vars["metric_type"] = service.MetricType;

                        vars["metric_min"] = service.MetricMin.ToString();

                        vars["metric_max"] = service.MetricMax.ToString();

                        vars["metric_perfkey"] = service.MetricPerfkey;

                        vars["metric_perfunit"] = service.MetricPerfunit;

                        vars["monview_warn"] = service.MonviewWarn;

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

                        if (servicesStaticById.ContainsKey(service.MastersvcCheck.ToString()))
                        {
                            var masterService = servicesStaticById[service.MastersvcCheck.ToString()];

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

                        if (servicesStaticById.ContainsKey(service.MastersvcNotify.ToString()))
                        {
                            var masterService = servicesStaticById[service.MastersvcNotify.ToString()];

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
