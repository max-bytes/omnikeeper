import React, {useState, forwardRef, useImperativeHandle, useRef} from "react";
import { Dropdown, Segment, Button, Icon } from 'semantic-ui-react'
import Form from 'react-bootstrap/Form';
import Col from 'react-bootstrap/Col';
import Row from 'react-bootstrap/Row';
import { useQuery } from '@apollo/client';
import { queries } from '../../graphql/queries'

export default forwardRef((props, ref) => {

    const { data: traitsJSON, loading: loadingTraits } = useQuery(queries.Traits);
    var [preferredTraitsFrom, setPreferredTraitsFrom] = useState(props.value.preferredTraitsFrom);
    var [preferredTraitsTo, setPreferredTraitsTo] = useState(props.value.preferredTraitsTo);

    const inputRefFrom = useRef();
    const inputRefTo = useRef();

    useImperativeHandle(ref, () => {
        return {
            getValue: () => {
                return {
                    preferredTraitsFrom: inputRefFrom.current.state.value,
                    preferredTraitsTo: inputRefTo.current.state.value
                };
            },
            isPopup: () => true
        };
    });


    if (loadingTraits && !traitsJSON) return "Loading...";

    // TODO: rework traits fetching
    var traitNames = Object.keys(JSON.parse(traitsJSON.traits));
    console.log(traitNames);

    // TODO
    const options = traitNames.map(traitName => 
        ({
            key: traitName,
            text: traitName,
            value: traitName,
        }));

    return <div style={{display: 'flex'}}>
        <Form onSubmit={e => { e.preventDefault(); }} style={{minWidth: '400px', margin: '10px'}}>
          <Form.Group as={Row} controlId="from">
            <Form.Label column>From</Form.Label>
            <Col sm={10}>
                <Dropdown placeholder='from' ref={inputRefFrom} fluid multiple search selection value={preferredTraitsFrom}
                    onChange={(e, data) => {
                        setPreferredTraitsFrom(data.value);
                    }}
                    options={options}
                />
            </Col>
          </Form.Group>
          <Form.Group as={Row} controlId="to">
            <Form.Label column>To</Form.Label>
            <Col sm={10}>
            <Dropdown placeholder='to' ref={inputRefTo} fluid multiple search selection value={preferredTraitsTo}
                onChange={(e, data) => {
                    setPreferredTraitsTo(data.value);
                }}
                options={options}
              />
            </Col>
          </Form.Group>
        </Form>
    </div>;
})