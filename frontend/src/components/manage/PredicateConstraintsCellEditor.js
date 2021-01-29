import React, {useState, forwardRef, useImperativeHandle, useRef} from "react";
import { Form, Row, Col, Select } from "antd"
import { useQuery } from '@apollo/client';
import { queries } from '../../graphql/queries';
import _ from 'lodash';

export default forwardRef((props, ref) => {

    const { data: traitsJSON, loading: loadingTraits } = useQuery(queries.TraitSet);
    var [preferredTraitsFrom, setPreferredTraitsFrom] = useState(props.value?.preferredTraitsFrom ?? []);
    var [preferredTraitsTo, setPreferredTraitsTo] = useState(props.value?.preferredTraitsTo ?? []);

    const inputRefFrom = useRef();
    const inputRefTo = useRef();

    useImperativeHandle(ref, () => {
        return {
            getValue: () => {
                return {
                    preferredTraitsFrom: inputRefFrom.current.props.value,
                    preferredTraitsTo: inputRefTo.current.props.value
                };
            },
            isPopup: () => true
        };
    });


    if (loadingTraits && !traitsJSON) return "Loading...";

    // TODO: rework traits fetching
    var traitNames = Object.keys(JSON.parse(traitsJSON.traitSet).Traits).filter(item => item !== "$type");

    // we mix in the currently set traits, so traits that don't exist anymore can still be managed
    const optionsFrom = _.union(traitNames, props.value?.preferredTraitsFrom ?? []).map(traitName => 
        ({
            key: traitName,
            text: traitName,
            value: traitName,
        }));
    const optionsTo = _.union(traitNames, props.value?.preferredTraitsTo ?? []).map(traitName => 
        ({
            key: traitName,
            text: traitName,
            value: traitName,
        }));

    return <div style={{display: 'flex'}}>
        <Form style={{minWidth: '600px', margin: '10px'}}>
            <Row>
                <Col span={4}>
                    <Form.Item style={{float: "right", paddingRight: "8px"}}>From Traits:</Form.Item>
                </Col>
                <Col span={20}>
                     <Select
                        ref={inputRefFrom}
                        value={preferredTraitsFrom}
                        placeholder='from'
                        style={{ width: "100%" }}
                        onChange={(value) => {
                            setPreferredTraitsFrom(value);
                        }}
                        showSearch
                        mode="multiple"
                        options={optionsFrom}
                    />
                </Col>
            </Row>
            <Row>
                <Col span={4}>
                    <Form.Item style={{float: "right", paddingRight: "8px", marginBottom: 0}}>To Traits:</Form.Item>
                </Col>
                <Col span={20}>
                    <Select
                        ref={inputRefTo} 
                        value={preferredTraitsTo}
                        placeholder='to'
                        style={{ width: "100%" }}
                        onChange={(value) => {
                            setPreferredTraitsTo(value);
                        }}
                        showSearch
                        mode="multiple"
                        options={optionsTo}
                    />
                </Col>
            </Row>
        </Form>
    </div>;
})