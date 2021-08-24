import React, { useCallback, useEffect } from "react";
import { Button, Radio, Space, Checkbox } from 'antd';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faWrench, faPlug, faArchive } from '@fortawesome/free-solid-svg-icons';
import _ from 'lodash';

function EffectiveTraitList(props) {

    const {checked, setChecked, showMetaTraits, setShowMetaTraits, showEmptyTrait, setShowEmptyTrait, effectiveTraitList} = props;

    function onReset() {
        var newChecked = {};
        for(const et in checked) {
            newChecked[et] = 0;
        }
        setChecked(newChecked);
        setShowMetaTraits(false);
        setShowEmptyTrait(false);
    }

    function onChange(effectiveTrait, e) {
        var value = parseInt(e, 10);
        setChecked({...checked, [effectiveTrait.id]: value});
    }

    const isMetaTraitID = useCallback((id) => {
        return id.startsWith("__meta");
    }, []);

    const emptyTraitID = "empty";
    const isEmptyTraitID = useCallback((id) => {
        return id === emptyTraitID;
    }, [emptyTraitID]); 

    // reset meta traits when the show-meta-traits checkbox is unchecked
    useEffect(() => {
        if (!showMetaTraits) {
            var newUnchecked = {};
            for(const et in checked) {
                if (checked[et] !== 0 && isMetaTraitID(et))
                    newUnchecked[et] = 0;
            }
            if (!_.isEmpty(newUnchecked)) {
                setChecked(oldChecked => {return {...oldChecked, ...newUnchecked}});
            }
        }
    }, [showMetaTraits, isMetaTraitID, setChecked, checked]);

    // reset empty trait to "must not"/-1 when the show-empty-trait checkbox is unchecked
    useEffect(() => {
        if (!showEmptyTrait) {
            if (checked[emptyTraitID] !== -1) {
                setChecked(oldChecked => {return {...oldChecked, [emptyTraitID]: -1}});
            }
        }
    }, [showEmptyTrait, emptyTraitID, setChecked, checked]);

    return (
            <Space direction="vertical" style={styles.container}>
                <div key={-1} style={styles.traitElement}>
                    <h4 style={styles.title}>Effective Traits</h4>
                    <span style={styles.reset}>
                        <Checkbox
                            checked={showEmptyTrait}
                            onChange={(e) => setShowEmptyTrait(e.target.checked)}
                        >Show empty</Checkbox>
                        <Checkbox
                            checked={showMetaTraits}
                            onChange={(e) => setShowMetaTraits(e.target.checked)}
                        >Show meta</Checkbox>
                        <Button onClick={onReset} size="small">Reset</Button>
                    </span>
                </div>
            {effectiveTraitList
                .filter(effectiveTrait => (showMetaTraits || !isMetaTraitID(effectiveTrait.id)) && (showEmptyTrait || !isEmptyTraitID(effectiveTrait.id)))
                .map((effectiveTrait, index) => {

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
                                    onChange={(e) => onChange(effectiveTrait, e.target.value)} value={checked[effectiveTrait.id]?.toString()}>
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
