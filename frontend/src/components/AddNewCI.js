import { useQuery, useMutation } from '@apollo/client';
import React, { useState } from 'react';
import { queries } from '../graphql/queries'
import { mutations } from '../graphql/mutations'
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import Col from 'react-bootstrap/Col'
import Row from 'react-bootstrap/Row'
import { Dropdown, Message, Icon } from 'semantic-ui-react'
import { withRouter } from 'react-router-dom'

function AddNewCI(props) {

  let initialNewCI = {ciIdentity: "", typeID: null };
  const [newCI, setNewCI] = useState(initialNewCI);
  
  const [error, setError] = useState("");
  const [goToCIAfterCreation, setGoToCIAfterCreation] = useState(false);

  const { data: dataCITypes } = useQuery(queries.CITypeList);
  const [createNewCI] = useMutation(mutations.CREATE_CI);

  if (!dataCITypes)
    return "Loading";
  else
  return (
    <div style={{display: 'flex', flexDirection: 'column', height: '100%'}}>
      <div style={{display: 'flex', justifyContent: 'center', alignItems: 'center', flexGrow: 1}}>
        <Form style={{display: 'flex', flexDirection: 'column', flexBasis: '500px'}} onSubmit={e => {
            e.preventDefault();
            createNewCI({ variables: { ciIdentity: newCI.ciIdentity, typeID: newCI.typeID }})
            .then(d => {
              if (goToCIAfterCreation)
                props.history.push(`/explorer/${newCI.ciIdentity}`);
              else
                setNewCI(initialNewCI);
            }).catch(e => {
              setError(e.message);
            });
          }}>
          <Form.Group as={Row} controlId="ciid">
            <Form.Label column>CI ID</Form.Label>
            <Col sm={10}>
              <Form.Control type="text" placeholder="Enter CI ID" value={newCI.ciIdentity} onChange={e => setNewCI({...newCI, ciIdentity: e.target.value})} />
            </Col>
          </Form.Group>
          <Form.Group as={Row} controlId="type">
            <Form.Label column>Type</Form.Label>
            <Col sm={10}>
              <Dropdown placeholder='Select CI type' fluid search selection value={newCI.type}
                onChange={(e, data) => {
                  setNewCI({...newCI, typeID: data.value });
                }}
                options={dataCITypes.citypes.map(type => { return {key: type.id, value: type.id, text: type.id }; })}
              />
            </Col>
          </Form.Group>
          <Form.Group as={Row} controlId="type" style={{paddingLeft: "1.25rem"}}>
            <Form.Check type='checkbox' label='Go to CI after creation' value={goToCIAfterCreation} onChange={e => setGoToCIAfterCreation(e.target.checked)} />
          </Form.Group>
          <Button variant="primary" type="submit">Create New CI</Button>
          {error && <Message attached='bottom' error>
            <Icon name='warning sign' />
            {error}
          </Message>}
        </Form>
      </div>
    </div>
  );
}

export default withRouter(AddNewCI);