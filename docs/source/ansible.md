# Ansible

DRAFT

omnikeeper offers a collection of ansible plugins to help working with omnikeeper from an ansible-based environment:

Ansible collection on Ansible Galaxy: <https://galaxy.ansible.com/maxbytes/omnikeeper>

Source code: <https://github.com/max-bytes/omnikeeper-ansible-collection>

## Plugins

### lookup_graphql

run GraphQL queries against omnikeeper, mainly used for fetching data.

Example task that fetches a list of hosts, including their network interfaces, filtered by hostname via regex, making the result data available for further processing through Ansible:

```ansible
  - name: ansible omnikeeper experiment
      hosts: localhost
      gather_facts: no
      connection: local
      tasks:
      - name: "Install required python libraries" # only required when not already installed
          pip:
              name: "{{ item }}"
              state: latest
          with_items:
              - oauthlib
              - requests-oauthlib
              - gql[aiohttp]
      - set_fact:
              query_variables:
                  hostname_regex: '^host.*$'
              query_string: |
                  query ($hostname_regex: String!) {
                      traitEntities(layers: ["cmdb"]) {
                          host {
                              filtered(filter: {hostname: {regex: {pattern: $hostname_regex}}}) {
                                  entity {
                                      hostname
                                      interfaces {
                                          entity {
                                              ip
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
      - name: perform query
          set_fact:
              query_response: "{{ query('maxbytes.omnikeeper.lookup_graphql', query_string, query_variables=query_variables, url='https://[replace-me]', username='[replace-me]', password='[replace-me]') }}"
      - name: Debug print
          ansible.builtin.debug:
              var: query_response
```
