import { useMutation } from '@apollo/client';
import React, { useState } from 'react';
import { mutations } from 'graphql/mutations'
import { Form, Button, Input, Checkbox, Alert } from "antd";
import { withRouter } from 'react-router-dom'
import LayerDropdown from "components/LayerDropdown";
import { useExplorerLayers } from 'utils/layers';

function AddNewCI(props) {

  let initialNewCI = {name: "", layerForName: null };
  const [newCI, setNewCI] = useState(initialNewCI);
  const { data: visibleAndWritableLayers } = useExplorerLayers(true, true);
  
  const [error, setError] = useState("");
  const [goToCIAfterCreation, setGoToCIAfterCreation] = useState(true);

  const [createNewCI] = useMutation(mutations.CREATE_CI);
  
  if (!visibleAndWritableLayers)
    return "Loading";
  else {

    return (
        <div style={{display: 'flex', justifyContent: 'center', alignItems: 'center', flexGrow: 1}}>
          <Form labelCol={{ span: "8" }} style={{display: 'flex', flexDirection: 'column', flexBasis: '500px'}} onFinish={e => {
                createNewCI({ variables: { name: newCI.name, layerIDForName: newCI.layerForName?.id }})
                .then(d => {
                  if (goToCIAfterCreation)
                    props.history.push(`/explorer/${d.data.createCIs.ciids[0]}`);
                  else
                    setNewCI(initialNewCI);
                }).catch(e => {
                  setError(e.message);
                });
              }}
              initialValues={{ name: newCI.name, layerForName: newCI.layerForName, goToCI: true }}
              >
            <h1>New CI</h1>

            <Form.Item label="Name" name="name">
                <Input type="text" placeholder="Enter name" value={newCI.name} onChange={e => setNewCI({...newCI, name: e.target.value})} />
            </Form.Item>
            <Form.Item label="Layer For Name" name="layerForName">
                <LayerDropdown layers={visibleAndWritableLayers} selectedLayer={newCI.layerForName} onSetSelectedLayer={l => setNewCI({...newCI, layerForName: l })} />
            </Form.Item>
            <Form.Item name="goToCI" valuePropName="checked" wrapperCol = {{ offset: "8" }} >
                <Checkbox checked={goToCIAfterCreation} onChange={e => setGoToCIAfterCreation(e.target.checked)}>Go to CI after creation</Checkbox>
            </Form.Item>
            <Form.Item>
                <Button style={{ width: "100%" }} type="primary" htmlType="submit">Create New CI</Button>
                {error && <Alert message={error} type="error" showIcon />}
            </Form.Item>
          </Form>

        </div>
    );
  }
}

export default withRouter(AddNewCI);