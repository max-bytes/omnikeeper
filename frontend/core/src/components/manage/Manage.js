import React, {useState} from 'react';
import { Link  } from 'react-router-dom'
import { Modal } from 'antd';
import { useQuery } from '@apollo/client';
import { queries } from '../../graphql/queries'
import env from "@beam-australia/react-env";
import { Popover } from "antd";

const FRONTEND_PLUGINS = "okplugin-plugintest1@0.9.3 okplugin-plugintest2@0.9.9"; // Hardcoded fake ENV-var for frontend-plugins // TODO: get real one

export default function Manage(props) {

  const { data: pluginsData } = useQuery(queries.Plugins);

  const [pluginModalOpen, setPluginModalOpen] = useState({open: false, managementEndpoint: undefined, name: undefined});

  const pluginModal = <Modal
    title={pluginModalOpen.name}
    style={{ top: 20 }}
    visible={pluginModalOpen.open}
    onOk={() => setPluginModalOpen({...pluginModalOpen, open: false})}
    onCancel={() => setPluginModalOpen({...pluginModalOpen, open: false})}
    footer={null}
    width={1000}
  >
    <iframe src={`${env('BACKEND_URL')}/../${pluginModalOpen.managementEndpoint}`} // HACK: BACKEND_URL contains /graphql suffix, remove!}
      title={pluginModalOpen.name}
      width="950px"
      height="550px"
      style={{border: '0px'}}/>
  </Modal>;

  // ########## MODULE DEVELOPMENT #########

  const frontendPlugins = (() => {
    const frontendPluginsStringArray = FRONTEND_PLUGINS.split(" ");

    return frontendPluginsStringArray.map(s => {
        try{
            // parse FRONTEND_PLUGINS
            const pluginName = s.split("@")[0];
            const pluginVersion = s.split("@")[1];

            let plugin;
            switch (pluginName) {
                case "okplugin-plugintest1":
                    // plugin = require("./../../local_plugins_for_dev/okplugin-plugintest1/src/test.js"); // FOR DEPLOYMENT ONLY !! // TODO: don't use in prod!
                    plugin = require("okplugin-plugintest1");
                    break;
                case "okplugin-plugintest2":
                    plugin = require("okplugin-plugintest2");
                    break;
                default:
                    return null;
            }

            // thows and error, if plugin doesn't have a default()
            const PluginComponent = plugin.default(pluginVersion);

            return (
                <li key={pluginName}>
                    <Popover placement="right" trigger="click" content={<PluginComponent/>}>
                        <a href="/#" onClick={(e) => e.preventDefault()}>{pluginName}</a>
                    </Popover>
                </li>
            );

        } catch(e) {
            // console.log(e);
            return null;
        }
    });
  })();

  // #######################################

  const plugins = (() => {
    if (!pluginsData) return <></>;
    if (!pluginsData.plugins) return <div>No configurable plugins loaded</div>;
    return pluginsData.plugins.map(lp => {
      if (lp.managementEndpoint)
        return <li key={lp.name}><a href="/#" onClick={(e) => {
          e.preventDefault();
          setPluginModalOpen({...pluginModalOpen, open: true, managementEndpoint: lp.managementEndpoint, name: lp.name});
        }}>{lp.name}</a></li>;
      else
        return null;
    })
  })();
  

  return <div style={{ padding: '10px' }}><h2>Management</h2>
    <h3>Core Management</h3>
    <ul>
      <li><Link to="/manage/baseConfiguration">Base Configuration</Link></li>
      <li><Link to="/manage/predicates">Predicates</Link></li>
      <li><Link to="/manage/layers">Layers</Link></li>
      <li><Link to="/manage/traits">Traits</Link></li>
      <li><Link to="/manage/oiacontexts">Online Inbound Layer Contexts</Link></li>
      <li><Link to="/manage/odataapicontexts">OData API Contexts</Link></li>
    </ul>

    <h3>Debug</h3>
    <ul>
      <li><Link to="/manage/cache">Cache</Link></li>
      <li><Link to="/manage/version">Version</Link></li>
      <li><Link to="/manage/current-user">Current User Data</Link></li>
      <li><Link to="/manage/logs">Logs</Link></li>
    </ul>

    <h3>Plugin Management</h3>
    <ul>
        {frontendPlugins}
        {plugins}
    </ul>
    {pluginModal}
  </div>;
}
