import React, {useState, forwardRef, useImperativeHandle, useRef} from "react";
import { Form, Select } from "antd"
import { useQuery } from '@apollo/client';
import { queries } from '../../graphql/queries_manage';
import _ from 'lodash';

export default forwardRef((props, ref) => {

    const { data, loading } = useQuery(queries.AvailablePermissions);
    var [selectedPermissions, setSelectedPermissions] = useState(props.value ?? []);

    const inputRef = useRef(null);
    
    useImperativeHandle(ref, () => {
        return {
            getValue: () => {
                return inputRef.current.props.value;
            },
            isPopup: () => true
        };
    });

    if (loading && !data) return "Loading...";
    
    var availablePermissions = data.manage_availablePermissions;

    // we mix in the currently set permissions, so permissions that don't exist anymore can still be managed
    const options = _.union(availablePermissions, props.value ?? []).map(p => 
        ({ key: p, text: p, value: p })
    );

    return <div style={{display: 'flex'}}>
        <Form style={{minWidth: '400px', margin: '10px'}}>
            <Select
                ref={inputRef}
                defaultActiveFirstOption={false}
                autoFocus={true}
                defaultOpen={true}
                value={selectedPermissions}
                tokenSeparators={[',']}
                placeholder='permissions'
                style={{ width: "100%" }}
                onChange={(value) => {
                    setSelectedPermissions(value);
                }}
                onBlur={() => {
                    console.log("blur");
                    props.api.stopEditing();
                }}
                onFocus={() => {
                    console.log("onFocus");
                    // props.api.stopEditing();
                }}
                showSearch
                mode="multiple"
                options={options}
            />
        </Form>
    </div>;
})