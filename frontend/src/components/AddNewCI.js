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
import LayerDropdown from "./LayerDropdown";
import { useLayers } from '../utils/useLayers';

function AddNewCI(props) {

  let initialNewCI = {name: "", layerForName: null, typeID: null };
  const [newCI, setNewCI] = useState(initialNewCI);
  const { data: sortedLayers } = useLayers();
  
  const [error, setError] = useState("");
  const [goToCIAfterCreation, setGoToCIAfterCreation] = useState(true);

  const { data: dataCITypes } = useQuery(queries.CITypeList);
  const [createNewCI] = useMutation(mutations.CREATE_CI);
  
  if (!dataCITypes || !sortedLayers)
    return "Loading";
  else {

    let visibleAndWritableLayers = sortedLayers.filter(l => l.visibility && l.writable && l.state === 'ACTIVE');

    return (
      <div style={{display: 'flex', flexDirection: 'column', height: '100%'}}>
        <div style={{display: 'flex', justifyContent: 'center', alignItems: 'center', flexGrow: 1}}>
          <Form style={{display: 'flex', flexDirection: 'column', flexBasis: '500px'}} onSubmit={e => {
              e.preventDefault();
              createNewCI({ variables: { name: newCI.name, layerIDForName: newCI.layerForName?.id, typeID: newCI.typeID }})
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
            <Form.Group as={Row} controlId="type">
              <Form.Label column>Type</Form.Label>
              <Col sm={9}>
                <Dropdown placeholder='Select CI type (optional)' fluid search selection value={newCI.type}
                  onChange={(e, data) => {
                    setNewCI({...newCI, typeID: data.value });
                  }}
                  options={dataCITypes.citypes.map(type => { return {key: type.id, value: type.id, text: type.id }; })}
                />
              </Col>
            </Form.Group>
            <Form.Group as={Row} controlId="type" style={{paddingLeft: "1.25rem"}}>
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