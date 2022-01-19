import React, { useState } from "react";
import { useMutation } from '@apollo/client';
import { mutations } from '../../graphql/mutations_manage';
import { Row, Col, Form, Input, Button, Card } from "antd";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus } from '@fortawesome/free-solid-svg-icons'
import { ErrorPopupButton } from "components/ErrorPopupButton";

const initialCreateLayerData = {id: ""};
function CreateLayer(props) {

  const {isEditable, onAfterCreation} = props;

  const [insertError, setInsertError] = useState(undefined);
  const [isOpen, setOpen] = useState(false);
  const [createLayerData, setCreateLayerData] = useState(initialCreateLayerData);

  const [createLayer] = useMutation(mutations.CREATE_LAYER);

  const createButton = <Button disabled={!isEditable} onClick={() => setOpen(!isOpen)} icon={<FontAwesomeIcon icon={faPlus} style={{marginRight: "10px"}}/>} type="primary">
    Create Layer
  </Button>

  const createLayerUI =
    <Card style={{ boxShadow: "0px 0px 5px 0px rgba(0,0,0,0.25)", width: '600px' }}>
      <Form labelCol={{ span: "4" }} onFinish={e => {
          setInsertError(undefined);
          createLayer({variables: { id: createLayerData.id} })
            .then(d => {
              setOpen(false);
              setCreateLayerData(initialCreateLayerData);
              onAfterCreation();
            }).catch(e => {
              setInsertError(e);
            });
        }}>

        <Row>
          <Col span={24}>
              <Form.Item name="id" label="Layer-ID">
                  <Input type="text" placeholder="Enter Layer-ID" value={createLayerData.id} 
                    onChange={e => {
                      const newValue = e.target.value;
                      setCreateLayerData(old => { return {...old, id: newValue}; });
                     }} />
              </Form.Item>
          </Col>
        </Row>

        <Button type="secondary" onClick={() => setOpen(false)} style={{ marginRight: "0.5rem" }}>Cancel</Button>
        <Button type="primary" htmlType="submit" disabled={!createLayerData.id}>Insert</Button>
        <ErrorPopupButton error={insertError} />
      </Form>
    </Card>;

  return <div>
      {!isOpen && createButton}
      {isOpen && createLayerUI}
    </div>;
}

export default CreateLayer;