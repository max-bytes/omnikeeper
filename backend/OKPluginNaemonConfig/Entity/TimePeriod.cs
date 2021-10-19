using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("timeperiod", TraitOriginType.Core)]
    public class TimePeriod : TraitEntity 
    {
        // NOTE: here id shoud be as TraitEntityID, for now name attribute is as TraitEntityID until a fix
        [TraitAttribute("id", "naemon_timeperiod.id")]
        public readonly long Id;

        [TraitAttribute("name", "naemon_timeperiod.name")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Name;

        [TraitAttribute("alias", "naemon_timeperiod.alias")]
        public readonly string Alias;

        [TraitAttribute("span_mon", "naemon_timeperiod.span_mon")]
        public readonly string SpanMon;

        [TraitAttribute("span_tue", "naemon_timeperiod.span_tue")]
        public readonly string SpanTue;

        [TraitAttribute("span_wed", "naemon_timeperiod.span_wed")]
        public readonly string SpanWed;

        [TraitAttribute("span_thu", "naemon_timeperiod.span_thu")]
        public readonly string SpanThu;

        [TraitAttribute("span_fri", "naemon_timeperiod.span_fri")]
        public readonly string SpanFri;

        [TraitAttribute("span_sat", "naemon_timeperiod.span_sat")]
        public readonly string SpanSat;

        [TraitAttribute("span_sun", "naemon_timeperiod.span_sun")]
        public readonly string SpanSun;

        public TimePeriod()
        {
            Name = "";
            Alias = "";
            SpanMon = "";
            SpanTue = "";
            SpanWed = "";
            SpanThu = "";
            SpanFri = "";
            SpanSat = "";
            SpanSun = "";
        }
    }
}
