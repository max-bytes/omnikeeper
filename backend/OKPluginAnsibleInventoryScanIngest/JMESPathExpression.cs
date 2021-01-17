using System;
using System.Collections.Generic;
using System.Text;

namespace OKPluginAnsibleInventoryScanIngest
{
    public static class JMESPathExpression
    {
        public static readonly string Expression = @"
[
	[?regexIsMatch('setup_facts_.*', document)] | [].{
		fqdn: store('fqdn', regexMatch('setup_facts_(.*)', document) | [1]),
		cis: data.ansible_facts.[
			{
				tempID: ciid(retrieve('fqdn')),
				idMethod: {
					method: 'byData',
					attributes: ['fqdn']
				},
				attributes: [
					{name: '__name', value:ansible_fqdn, type:'Text'},
					{name: 'fqdn', value:ansible_fqdn, type:'Text'},
					{name: 'os_family', value:ansible_architecture, type:'Text'},
					{name: 'ansible.inventory.cmdline', value:ansible_cmdline, type:'JSON'},
					{name: 'ansible.inventory.last_scan_time', value:ansible_date_time.iso8601, type:'Text'},
					{name: 'default_ipv4', value:ansible_default_ipv4, type:'JSON'},
					{name: 'hostname', value: ansible_hostname, type:'Text'},
					{name: 'distribution', value: ansible_distribution, type:'Text'},
					{name: 'distribution_file_variety', value: ansible_distribution_file_variety, type:'Text'},
					{name: 'distribution_major_version', value: ansible_distribution_major_version, type:'Text'},
					{name: 'distribution_release', value: ansible_distribution_release, type:'Text'},
					{name: 'distribution_version', value: ansible_distribution_version, type:'Text'},
					{name: 'processor_vcpus', value: ansible_processor_vcpus, type:'Integer'},
					{name: 'processor_cores', value: ansible_processor_cores, type:'Integer'},
					{name: 'processor_count', value: ansible_processor_count, type:'Integer'},
					{name: 'kernel', value: ansible_kernel, type:'Text'},
					{name: 'memtotal_mb', value: ansible_memtotal_mb, type:'Integer'},
					{name: 'interfaces', value: ansible_interfaces, type:'Text'},
					{name: 'dns', value: ansible_dns, type:'JSON'}
				]
			},
			ansible_mounts[].{
				tempID: ciid(retrieve('fqdn'), 'ansible_mounts', idx('ansible_mounts')),
				idMethod: {
					method: 'byData',
					attributes: ['__name', 'device', 'mount']
				},
				attributes: [
					{name: '__name', value: join(':', [retrieve('fqdn'), mount]), type:'Text'},
					{name: 'device', value: device, type:'Text'},
					{name: 'mount', value: mount, type:'Text'},
					{name: 'block_available', value: block_available, type:'Integer'},
					{name: 'block_size', value: block_size, type:'Integer'},
					{name: 'block_total', value: block_total, type:'Integer'},
					{name: 'block_used', value: block_used, type:'Integer'},
					{name: 'device', value: device, type:'Text'},
					{name: 'fstype', value: fstype, type:'Text'},
					{name: 'inode_available', value: inode_available, type:'Integer'},
					{name: 'inode_total', value: inode_total, type:'Integer'},
					{name: 'inode_used', value: inode_used, type:'Integer'},
					{name: 'mount', value: mount, type:'Text'},
					{name: 'options', value: options, type:'Text'},
					{name: 'size_available', value: size_available, type:'Integer'},
					{name: 'size_total', value: size_total, type:'Integer'},
					{name: 'uuid', value: uuid, type:'Text'}
				]
			},
			values(filterHashKeys(map(&join('', ['ansible_', stringReplace(@, '-', '_')]), ansible_interfaces[]), @))[].{
				tempID: ciid(retrieve('fqdn'), 'ansible_interfaces', idx('ansible_interfaces')),
					idMethod: {
						method: 'byData',
						attributes: ['__name']
					},
				attributes: [
					{name: '__name', value: join('', ['Network Interface ', device, '@', retrieve('fqdn')]), type:'Text'},
					{name: 'device', value: device, type:'Text'},
					{name: 'active', value: to_string(active), type:'Text'},
					{name: 'type', value: type, type:'Text'},
					macaddress.{name: 'macaddress', value: @, type:'Text'}
				] | []
			}
		] | [],
		relations: [
			map(&{from: ciid(retrieve('fqdn')), predicate: 'has_mounted_device', to: ciid(retrieve('fqdn'), 'ansible_mounts', idx('ansible_mounts_redo'))}, data.ansible_facts.ansible_mounts[]),
			map(&{from: ciid(retrieve('fqdn')), predicate: 'has_network_interface', to: ciid(retrieve('fqdn'), 'ansible_interfaces', idx('ansible_interfaces_redo'))}, data.ansible_facts.ansible_interfaces[])
		] | []
	},

	[?regexIsMatch('yum_installed_.*', document)] | [].{
		fqdn: store('fqdn', regexMatch('yum_installed_(.*)', document) | [1]),
		cis: data.[{
			tempID: ciid(retrieve('fqdn')),
			idMethod: {
				method: 'byData',
				attributes: ['fqdn']
			},
			attributes: [
				{name: '__name', value: retrieve('fqdn'), type: 'Text'},
				{name: 'fqdn', value: retrieve('fqdn'), type: 'Text'},
				{name: 'yum.installed', value: results, type: 'JSON'}
			]
		}]
	},
	[?regexIsMatch('yum_repos_.*', document)] | [].{
		fqdn: store('fqdn', regexMatch('yum_repos_(.*)', document) | [1]),
		cis: data.[{
			tempID: ciid(retrieve('fqdn')),
			idMethod: {
				method: 'byData',
				attributes: ['fqdn']
			},
			attributes: [
				{name: '__name', value: retrieve('fqdn'), type: 'Text'},
				{name: 'fqdn', value: retrieve('fqdn'), type: 'Text'},
				{name: 'yum.repos', value: results, type: 'JSON'}
			]
		}]
	},
	[?regexIsMatch('yum_updates_.*', document)] | [].{
		fqdn: store('fqdn', regexMatch('yum_updates_(.*)', document) | [1]),
		cis: data.[{
			tempID: ciid(retrieve('fqdn')),
			idMethod: {
				method: 'byData',
				attributes: ['fqdn']
			},
			attributes: [
				{name: '__name', value: retrieve('fqdn'), type: 'Text'},
				{name: 'fqdn', value: retrieve('fqdn'), type: 'Text'},
				{name: 'yum.updates', value: results, type: 'JSON'}
			]
		}]
	}

] | []

| { cis: [].cis | [], relations: [].relations | [] }
        
";
    }
}
