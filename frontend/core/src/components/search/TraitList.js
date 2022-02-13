import React, { useCallback, useEffect } from "react";
import { Button, Radio, Space, Checkbox } from 'antd';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faWrench, faPlug, faArchive } from '@fortawesome/free-solid-svg-icons';
import _ from 'lodash';
import TraitID from "utils/TraitID";

function TraitList(props) {

    const {checked, setChecked, showMetaTraits, setShowMetaTraits, showEmptyTrait, setShowEmptyTrait, traitList} = props;

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

    // reset empty trait to "may"/0 when the show-empty-trait checkbox is unchecked
    useEffect(() => {
        if (!showEmptyTrait) {
            if (checked[emptyTraitID] !== 0) {
                setChecked(oldChecked => {return {...oldChecked, [emptyTraitID]: 0}});
            }
        }
    }, [showEmptyTrait, emptyTraitID, setChecked, checked]);

    return (
            <Space direction="vertical" style={styles.container}>
                <div key={-1} style={styles.traitElement}>
                    <h4 style={styles.title}>Traits</h4>
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
            {traitList
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
                    <div key={effectiveTrait.id} style={styles.traitElement}>
                        <span style={styles.traitsIcon}>
                            <FontAwesomeIcon icon={icon} style={{ marginRight: "0.5rem" }}/>
                        </span>
                        <span style={styles.traitsID}>
                            <bdi><TraitID id={effectiveTrait.id} link={true} /></bdi>
                        </span>
                        <Radio.Group buttonStyle="solid" size="small" style={{display: 'flex'}}
                            onChange={(e) => onChange(effectiveTrait, e.target.value)} value={checked[effectiveTrait.id]?.toString()}>
                            <Radio.Button value="-1">No</Radio.Button>
                            <Radio.Button value="0">May</Radio.Button>
                            <Radio.Button value="1">Yes</Radio.Button>
                        </Radio.Group>
                    </div>
                );
            })}
            </Space>
    );
}

export default TraitList;

const styles = {
    container: {
        display: "flex",
        overflowX: "hidden",
        overflowY: "scroll",
        paddingRight: "10px",
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
        flexGrow: 1,
        overflow: "hidden",
        textOverflow: "ellipsis",
        whiteSpace: "nowrap",
        direction: "rtl",
        textAlign: "left",
        paddingRight: "5px",
    },
};
