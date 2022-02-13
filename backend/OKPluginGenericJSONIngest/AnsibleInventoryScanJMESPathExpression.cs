namespace OKPluginGenericJSONIngest
{
    // TODO: move/replace with config
    public static class AnsibleInventoryScanJMESPathExpression
    {
        public static readonly string Expression = @"
[
	[?regexIsMatch('setup_facts_.*.json', document)] | [].{
		fqdn: store('fqdn', regexMatch('setup_facts_(.*).json', document) | [1]),
		cis: data.ansible_facts.[
			{
				tempID: ciid(retrieve('fqdn')),
				idMethod: idMethodByData(['fqdn']),
				attributes: [
					attribute('__name', retrieve('fqdn')),
					attribute('fqdn',ansible_fqdn),
					attribute('os_family',ansible_distribution),
					attribute('architecture',ansible_architecture),
					attribute('ansible.inventory.cmdline', ansible_cmdline, 'JSON'),
					attribute('ansible.inventory.last_scan_time',to_string(ansible_date_time.iso8601)),
					attribute('default_ipv4',ansible_default_ipv4, 'JSON'),
					attribute('hostname', ansible_hostname),
					attribute('distribution', ansible_distribution),
					attribute('distribution_file_variety', ansible_distribution_file_variety),
					attribute('distribution_major_version', ansible_distribution_major_version),
					attribute('distribution_release', ansible_distribution_release),
					attribute('distribution_version', ansible_distribution_version),
					attribute('processor_vcpus', ansible_processor_vcpus, 'Integer'),
					attribute('processor_cores', ansible_processor_cores, 'Integer'),
					attribute('processor_count', ansible_processor_count, 'Integer'),
					attribute('kernel', ansible_kernel),
					attribute('memtotal_mb', ansible_memtotal_mb, 'Integer'),
					attribute('interfaces', ansible_interfaces),
					attribute('dns', ansible_dns, 'JSON')
				]
			},
			ansible_mounts[].{
				tempID: ciid(retrieve('fqdn'), 'ansible_mounts', idx('ansible_mounts')),
				idMethod: idMethodByData(['__name', 'device', 'mount']),
				attributes: [
					attribute('__name', join(':', [retrieve('fqdn'), mount])),
					attribute('device', device),
					attribute('mount', mount),
					attribute('block_available', block_available, 'Integer'),
					attribute('block_size', block_size, 'Integer'),
					attribute('block_total', block_total, 'Integer'),
					attribute('block_used', block_used, 'Integer'),
					attribute('fstype', fstype),
					attribute('inode_available', inode_available, 'Integer'),
					attribute('inode_total', inode_total, 'Integer'),
					attribute('inode_used', inode_used, 'Integer'),
					attribute('options', options),
					attribute('size_available', size_available, 'Integer'),
					attribute('size_total', size_total, 'Integer'),
					attribute('uuid', uuid)
				]
			},
			values(filterHashKeys(map(&join('', ['ansible_', stringReplace(@, '-', '_')]), ansible_interfaces[]), @))[].{
				tempID: ciid(retrieve('fqdn'), 'ansible_interfaces', idx('ansible_interfaces')),
				idMethod: idMethodByData(['__name']),
				attributes: [
					attribute('__name', join('', ['Network Interface ', device, '@', retrieve('fqdn')])),
					attribute('device', device),
					attribute('active', to_string(active)),
					attribute('type', type),
					attribute('macaddress', macaddress)
				] | []
			}
		] | [],
		relations: [
			map(&relation(ciid(retrieve('fqdn')), 'has_mounted_device', ciid(retrieve('fqdn'), 'ansible_mounts', idx('ansible_mounts_redo'))), data.ansible_facts.ansible_mounts[]),
			map(&relation(ciid(retrieve('fqdn')), 'has_network_interface', ciid(retrieve('fqdn'), 'ansible_interfaces', idx('ansible_interfaces_redo'))), data.ansible_facts.ansible_interfaces[])
		] | []
	},

	[?regexIsMatch('yum_installed_.*.json', document)] | [].{
		fqdn: store('fqdn', regexMatch('yum_installed_(.*).json', document) | [1]),
		cis: data.[{
			tempID: ciid(retrieve('fqdn'), 'yum_installed'),
			idMethod: idMethodByTempID(ciid(retrieve('fqdn'))),
			attributes: [
				attribute('yum.installed', results, 'JSON')
			]
		}]
	},
	[?regexIsMatch('yum_repos_.*.json', document)] | [].{
		fqdn: store('fqdn', regexMatch('yum_repos_(.*).json', document) | [1]),
		cis: data.[{
			tempID: ciid(retrieve('fqdn'), 'yum_repos'),
			idMethod: idMethodByTempID(ciid(retrieve('fqdn'))),
			attributes: [
				attribute('yum.repos', results, 'JSON')
			]
		}]
	},
	[?regexIsMatch('yum_updates_.*.json', document)] | [].{
		fqdn: store('fqdn', regexMatch('yum_updates_(.*).json', document) | [1]),
		cis: data.[{
			tempID: ciid(retrieve('fqdn'), 'yum_updates'),
			idMethod: idMethodByTempID(ciid(retrieve('fqdn'))),
			attributes: [
				attribute('yum.updates', results, 'JSON')
			]
		}]
	}

] | []

| { cis: [].cis | [], relations: [].relations | [] }
        
";
    }
}
