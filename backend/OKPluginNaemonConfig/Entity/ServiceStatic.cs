using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("service_static", TraitOriginType.Core)]
    public class ServiceStatic : TraitEntity
    {
        [TraitAttribute("id", "naemon_services_static.id")]
        public readonly long Id;

        [TraitAttribute("servicename", "naemon_services_static.servicename")]
        [TraitEntityID]
        public readonly string ServiceName;

        [TraitAttribute("module", "naemon_services_static.module")]
        public readonly long Module;

        [TraitAttribute("disabled", "naemon_services_static.disabled")]
        public readonly long Disabled;

        [TraitAttribute("perf", "naemon_services_static.perf")]
        public readonly long Perf;

        [TraitAttribute("layer", "naemon_services_static.layer")]
        public readonly long Layer;

        [TraitAttribute("checkcommand", "naemon_services_static.checkcommand")]
        public readonly long CheckCommand;

        [TraitAttribute("freshness_threshold", "naemon_services_static.freshness_threshold")]
        public readonly long FreshnessThreshold;

        [TraitAttribute("passive", "naemon_services_static.passive")]
        public readonly long Passive;

        [TraitAttribute("check_interval", "naemon_services_static.check_interval")]
        public readonly long CheckInterval;

        [TraitAttribute("max_check_attempts", "naemon_services_static.max_check_attempts")]
        public readonly long MaxCheckAttempts;

        [TraitAttribute("retry_interval", "naemon_services_static.retry_interval")]
        public readonly long RetryInterval;

        [TraitAttribute("timeperiod_check", "naemon_services_static.timeperiod_check")]
        public readonly long TimeperiodCheck;

        [TraitAttribute("timeperiod_notify", "naemon_services_static.timeperiod_notify")]
        public readonly long TimeperiodNotify;

        [TraitAttribute("target", "naemon_services_static.target")]
        public readonly string Target;

        [TraitAttribute("checkprio", "naemon_services_static.checkprio")]
        public readonly string Checkprio;

        [TraitAttribute("monview_visibility", "naemon_services_static.monview_visibility")]
        public readonly long MonviewVisibility;

        [TraitAttribute("kpi_avail", "naemon_services_static.kpi_avail")]
        public readonly long KPIAvail;

        [TraitAttribute("metric_name", "naemon_services_static.metric_name")]
        public readonly string MetricName;

        [TraitAttribute("metric_type", "naemon_services_static.metric_type")]
        public readonly string MetricType;

        [TraitAttribute("metric_min", "naemon_services_static.metric_min")]
        public readonly long MetricMin;

        [TraitAttribute("metric_max", "naemon_services_static.metric_max")]
        public readonly long MetricMax;

        [TraitAttribute("metric_perfkey", "naemon_services_static.metric_perfkey")]
        public readonly string MetricPerfkey;

        [TraitAttribute("metric_perfunit", "naemon_services_static.metric_perfunit")]
        public readonly string MetricPerfunit;

        [TraitAttribute("monview_warn", "naemon_services_static.monview_warn")]
        public readonly string MonviewWarn;

        [TraitAttribute("monview_crit", "naemon_services_static.monview_crit")]
        public readonly string MonviewCrit;

        [TraitAttribute("mastersvc_check", "naemon_services_static.mastersvc_check")]
        public readonly long MastersvcCheck;

        [TraitAttribute("mastersvc_notify", "naemon_services_static.mastersvc_notify")]
        public readonly long MastersvcNotify;

        [TraitAttribute("arg1", "naemon_services_static.arg1")]
        public readonly string Arg1;

        [TraitAttribute("arg2", "naemon_services_static.arg2")]
        public readonly string Arg2;

        [TraitAttribute("arg3", "naemon_services_static.arg3")]
        public readonly string Arg3;

        [TraitAttribute("arg4", "naemon_services_static.arg4")]
        public readonly string Arg4;

        [TraitAttribute("arg5", "naemon_services_static.arg5")]
        public readonly string Arg5;

        [TraitAttribute("arg6", "naemon_services_static.arg6")]
        public readonly string Arg6;

        [TraitAttribute("arg7", "naemon_services_static.arg7")]
        public readonly string Arg7;

        [TraitAttribute("arg8", "naemon_services_static.arg8")]
        public readonly string Arg8;

        public ServiceStatic()
        {
            ServiceName = "";
            Target = "";
            Checkprio = "";
            MetricName = "";
            MetricType = "";
            MetricPerfkey = "";
            MetricPerfunit = "";
            MonviewWarn = "";
            MonviewCrit = "";
            Arg1 = "";
            Arg2 = "";
            Arg3 = "";
            Arg4 = "";
            Arg5 = "";
            Arg6 = "";
            Arg7 = "";
            Arg8 = "";
        }
    }
}
