import { useMutation } from '@apollo/client';
import React, { useState } from 'react';
import { mutations } from '../graphql/mutations'
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import Col from 'react-bootstrap/Col'
import Row from 'react-bootstrap/Row'
import { Message, Icon } from 'semantic-ui-react'
import { withRouter } from 'react-router-dom'
import LayerDropdown from "./LayerDropdown";
import { useExplorerLayers } from '../utils/layers';

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
      <div style={{display: 'flex', flexDirection: 'column', height: '100%'}}>
        <div style={{display: 'flex', justifyContent: 'center', alignItems: 'center', flexGrow: 1}}>
          <Form style={{display: 'flex', flexDirection: 'column', flexBasis: '500px'}} onSubmit={e => {
              e.preventDefault();
              createNewCI({ variables: { name: newCI.name, layerIDForName: newCI.layerForName?.id }})
              .then(d => {
                if (goToCIAfterCreation)
                  props.history.push(`/explorer/${d.data.createCIs.ciids[0]}`);
                else
                  setNewCI(initialNewCI);
              }).catch(e => {
                setError(e.message);
              });
            }}>
            <h1>New CI</h1>
              
            <Form.Group as={Row} controlId="name">
              <Form.Label column>Name</Form.Label>
              <Col sm={9}>
                <Form.Control type="text" placeholder="Enter name" value={newCI.name} onChange={e => setNewCI({...newCI, name: e.target.value})} />
              </Col>
            </Form.Group>
            <Form.Group as={Row} controlId="layerForName">
              <Form.Label column>Layer For Name</Form.Label>
              <Col sm={9}>
                <LayerDropdown layers={visibleAndWritableLayers} selectedLayer={newCI.layerForName} onSetSelectedLayer={l => setNewCI({...newCI, layerForName: l })} />
              </Col>
            </Form.Group>
            <Form.Group as={Row} controlId="goToCI" style={{paddingLeft: "1.25rem"}}>
              <Form.Check type='checkbox' label='Go to CI after creation' checked={goToCIAfterCreation} onChange={e => setGoToCIAfterCreation(e.target.checked)} />
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
}

export default withRouter(AddNewCI);