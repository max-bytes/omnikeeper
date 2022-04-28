namespace OKPluginInsightDiscoveryScanIngest
{
    public static class DefaultJMESPathExpression
    {
        public static readonly string Expression = @"
{
    cis: [
        [].{
            tempID: ciid(join('::', ['Host', FQDN || '', UUID || '', Hostname || ''])), 
            idMethod: idMethodByUnion([
                idMethodByAttribute(attribute('insight_discovery.fqdn', FQDN)),
                idMethodByAttribute(attribute('insight_discovery.uuid', UUID)),
                idMethodByAttribute(attribute('insight_discovery.hostname', Hostname))
            ]),
            sameTargetCIHandling: 'DropAndWarn',
            attributes: [
                attribute('__name', join(' - ', ['Host', FQDN || Hostname || 'Unknown'])),

                attribute('insight_discovery.type', 'host'),

                attribute('insight_discovery.fqdn', FQDN || ''),
                attribute('insight_discovery.hostname', Hostname || ''),
                attribute('insight_discovery.object_hash', ObjectHash),
                attribute('insight_discovery.uuid', UUID || ''),

                attribute('insight_discovery.os.name', OS.OSName || ''),
                attribute('insight_discovery.os.manufacturer', OS.Manufacturer || ''),
                attribute('insight_discovery.os.architecture', OS.OSArchitecture || ''),
                attribute('insight_discovery.os.version', OS.Version || ''),
                attribute('insight_discovery.os.build_number', OS.BuildNumber || ''),
                attribute('insight_discovery.os.kernel', OS.Kernel || ''),
                attribute('insight_discovery.os.servicepack_major_version', OS.ServicePackMajorVersion || ''),
                attribute('insight_discovery.os.servicepack_minor_version', OS.ServicePackMinorVersion || ''),

                attribute('insight_discovery.cpu_count', CPUCount, 'Integer')
            ]
        },
        [].VirtualGuests[].{
            tempID: ciid(join('::', ['VirtualHost', Hostname || '', UUID || '', Name])),
            idMethod: idMethodByUnion([
                idMethodByAttribute(attribute('insight_discovery.fqdn', Hostname)),
                idMethodByAttribute(attribute('insight_discovery.uuid', UUID))
            ]),
            sameTargetCIHandling: 'Merge',
            attributes: [
                attribute('__name', join(' - ', ['Host', Hostname || Name || 'Unknown'])),

                attribute('insight_discovery.type', 'host'),

                attribute('insight_discovery.fqdn', Hostname || ''),
                attribute('insight_discovery.virtualized_name', Name),
                attribute('insight_discovery.uuid', UUID || ''),

                attribute('insight_discovery.os.name', OS.OSName || '')
            ]
        },
        [].Patches[].{
            tempID: ciid(join('::', ['Patch', HotFixID])),
            idMethod: idMethodByAttribute(attribute('insight_discovery.hotfix_id', HotFixID)),
            sameTargetCIHandling: 'Drop',
            attributes: [
                attribute('__name', join(' - ', ['Patch', HotFixID])),
                attribute('insight_discovery.type', 'patch'),
                attribute('insight_discovery.hotfix_id', HotFixID),
                attribute('insight_discovery.caption', Caption),
                attribute('insight_discovery.description', Description)
            ]
        }
    ] | [],
    relations: [
        [].{
          hostid: store('hostid', join('::', ['Host', FQDN || '', UUID || '', Hostname || ''])),
          tmp: map(&relation(ciid(join('::', ['VirtualHost', Hostname || '', UUID || '', Name])), 'runs_on', ciid(retrieve('hostid'))), VirtualGuests)
        } | [].tmp | [],
        [].{
            hostid: store('hostid', join('::', ['Host', FQDN || '', UUID || '', Hostname || ''])),
            tmp: map(&relation(ciid(retrieve('hostid')), 'has_patch_installed', ciid(join('::', ['Patch', HotFixID]))), Patches)
        } | [].tmp | []
    ] | []
}
";
    }
}