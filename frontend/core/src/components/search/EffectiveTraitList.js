import React from "react";
import { Button, Radio, Space } from 'antd';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faWrench, faPlug, faArchive, faDatabase } from '@fortawesome/free-solid-svg-icons';

function EffectiveTraitList(props) {

    function onReset() {
        var newChecked = [];
        for(const et in props.checked) {
            newChecked[et] = 0;
        }
        props.setChecked(newChecked);
    }

    function onChange(effectiveTrait, e) {
        var value = parseInt(e.target.value, 10);
        props.setChecked({...props.checked, [effectiveTrait.id]: value});
    }

    return (
            <Space direction="vertical" style={styles.container}>
                <div key={-1} style={styles.traitElement}>
                    <h4 style={styles.title}>Effective Traits</h4>
                    <span style={styles.reset}>
                        <Button onClick={onReset} size="small">Reset</Button>
                    </span>
                </div>
            {props.effectiveTraitList.map((effectiveTrait, index) => {

                const icon = (function(originType) {
                    switch(originType) {
                    case 'PLUGIN':
                        return faPlug;
                    case 'CORE':
                        return faArchive;
                    case 'DATA':
                        return faWrench;
                    default:
                        return '';
                    }
                })(effectiveTrait.origin.type);

                return (
                    <div key={index} style={styles.traitElement}>
                        <span style={styles.traitsIcon}>
                            <FontAwesomeIcon icon={icon} style={{ marginRight: "0.5rem" }}/>
                        </span>
                        <span style={styles.traitsID}>
                            {effectiveTrait.id}
                        </span>
                        <span>
                                <Radio.Group buttonStyle="solid" size="small"
                                    onChange={(e) => onChange(effectiveTrait, e)} value={props.checked[effectiveTrait.id]?.toString()}>
                                    <Radio.Button value="-1">No</Radio.Button>
                                    <Radio.Button value="0">May</Radio.Button>
                                    <Radio.Button value="1">Yes</Radio.Button>
                                </Radio.Group>
                        </span>
                    </div>
                );
            })}
            </Space>
    );
}

export default EffectiveTraitList;

const styles = {
    container: {
        display: "flex"
    },
    title: {
        flexGrow: "1",
        marginBottom: "0px",
        alignSelf: "center",
    },
    traitElement: { 
        display: "flex" 
    },
    traitsID: {
        flexGrow: 1
    },
};
