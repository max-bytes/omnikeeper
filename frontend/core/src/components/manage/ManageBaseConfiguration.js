import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faChevronLeft } from '@fortawesome/free-solid-svg-icons';
import { Form, Input, Button } from 'antd';
import { queries } from 'graphql/queries_manage'
import { mutations } from 'graphql/mutations_manage'
import 'ace-builds';
import { useQuery, useMutation } from '@apollo/client';
import 'ace-builds/webpack-resolver';
import { ErrorPopupButton } from "../ErrorPopupButton";

import "ace-builds/src-noconflict/mode-json";

export default function ManageBaseConfiguration() {

  const { data, loading } = useQuery(queries.BaseConfiguration, {fetchPolicy: 'network-only'});
  const [setBaseConfiguration, { loading: setBaseConfigurationLoading, error: setBaseConfigurationError }] = useMutation(mutations.SET_BASECONFIGURATION);
  const [config, setConfig] = useState(null);
  useEffect(() => {
    if (!!data) setConfig(data.manage_baseConfiguration);
  }, [data]);

  return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
    <h2>Base Configuration</h2>
    <div><Link to="."><FontAwesomeIcon icon={faChevronLeft} /> Back</Link></div>
    { config ?
        <div style={{ display: 'flex', justifyContent: 'center', flexGrow: 1 }}>
            <Form 
                labelCol={{ span: "8" }}
                style={{ display: 'flex', flexDirection: 'column', flexBasis: '1000px', margin:'10px 0px' }}
                onFinish={e => {
                    e.ConfigLayerset = e.ConfigLayerset.split(",");
                    setBaseConfiguration({ variables: { baseConfiguration: JSON.stringify(e) } }).then(d => {
                        setConfig(data.manage_baseConfiguration);
                    }).catch(e => {});
                }}
                initialValues={JSON.parse(config)}
            >
                <Form.Item name="$type" label="$type" rules={[{ required: true }]} hidden>
                    <Input />
                </Form.Item>
                <Form.Item name="ConfigLayerset" label="Config Layerset" rules={[{ required: true }]}>
                    <Input />
                </Form.Item>
                <Form.Item name="ConfigWriteLayer" label="Config Write Layer" rules={[{ required: true }]}>
                    <Input />
                </Form.Item>
                <Form.Item name="ArchiveChangesetThreshold" label="Archive Changeset Threshold" rules={[{ required: true }]}>
                    <Input />
                </Form.Item>
                <Form.Item name="CLBRunnerInterval" label="CLB Runner Interval" rules={[{ required: true }]}>
                    <Input />
                </Form.Item>
                <Form.Item name="MarkedForDeletionRunnerInterval" label="Marked For Deletion Runner Interval" rules={[{ required: true }]}>
                    <Input />
                </Form.Item>
                <Form.Item name="ExternalIDManagerRunnerInterval" label="External ID Manager Runner Interval" rules={[{ required: true }]}>
                    <Input />
                </Form.Item>
                <Form.Item name="ArchiveOldDataRunnerInterval" label="Archive Old Data Runner Interval" rules={[{ required: true }]}>
                    <Input />
                </Form.Item>
                <div style={{ display: "flex", justifyContent: "center" }}>
                    <Button type="primary" htmlType="submit" disabled={loading || setBaseConfigurationLoading} style={{ width: "100%"}}>Save</Button>
                    <ErrorPopupButton error={setBaseConfigurationError} />
                </div>
            </Form>
        </div>
    : "Loading..." }
  </div>;
}
