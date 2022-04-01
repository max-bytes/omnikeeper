import React, { useCallback, useEffect } from "react";
import { Button, Radio, Space, Checkbox } from 'antd';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faWrench, faPlug, faArchive, faFolder } from '@fortawesome/free-solid-svg-icons';
import _ from 'lodash';
import TraitID from "utils/TraitID";
import { Tree } from 'antd';
import { useLocalStorage } from 'utils/useLocalStorage';

function TraitList(props) {

    const {checked, setChecked, traitList} = props;

    function onReset() {
        var newChecked = {};
        for(const et in checked) {
            newChecked[et] = 0;
        }
        setChecked(newChecked);
    }

    function onChange(effectiveTrait, e) {
        var value = parseInt(e, 10);
        setChecked({...checked, [effectiveTrait.id]: value});
    }

    const [expandedKeys, setExpandedKeys] = useLocalStorage('searchTraitsExpanded', []);

    const traitTuples = traitList
        .map(t => {return {trait: t, treeKey: t.id.split('.')};});
    function groupByTraitID(traitTuples, parentTreeKey) {
        return _.values(
            _.mapValues(
                _.groupBy(traitTuples, (t) => t.treeKey[0]),
                (subTraitTuples, treeKeyPrefix) => {
                    const currentTreeKey = [parentTreeKey, treeKeyPrefix].filter(x => x).join('.');

                    if (subTraitTuples.length === 1) {
                        const subTraitTuple = subTraitTuples[0];
                        return {
                            title: subTraitTuple.trait.id,
                            key: currentTreeKey,
                            children: [],
                            effectiveTrait: subTraitTuple.trait
                        }
                    }

                    const tmp = subTraitTuples.map(subTraitTuple => {
                        const subTree = subTraitTuple.treeKey.slice(1);
                        return {...subTraitTuple, treeKey: subTree }
                    });
                    return {
                        title: treeKeyPrefix, 
                        key: currentTreeKey,
                        children: groupByTraitID(tmp, currentTreeKey)
                    };
                }
            )
        );
    }
    const treeData = groupByTraitID(traitTuples, null);

    return (
            <Space direction="vertical" style={styles.container}>
                <Space style={styles.traitElement}>
                    <h4 style={styles.title}>Traits</h4>
                    <span style={styles.reset}>
                        <Button onClick={onReset} size="small">Reset</Button>
                    </span>
                </Space>

                <Tree
                    defaultExpandAll={true}
                    blockNode={true}
                    showline={true}
                    expandedKeys={expandedKeys}
                    onExpand={(newExpandedKeys, _) => { 
                        setExpandedKeys(newExpandedKeys);
                    }}
                    selectable={false}
                    treeData={treeData}
                    titleRender={(nodeData)=> {
                        const {effectiveTrait, key, title} = nodeData;
                        if (!effectiveTrait)
                            return <EffectiveTraitSelectGroup key={key} title={title} />;
                        else
                            return <EffectiveTraitSelectItem key={key} effectiveTrait={effectiveTrait} onChange={onChange} value={checked[effectiveTrait.id]?.toString()} />;
                    }}
                    />
            </Space>
    );
}

function EffectiveTraitSelectGroup(props) {
    const {title} = props;
    return <div style={styles.traitElement}>
        <span>
            <FontAwesomeIcon icon={faFolder} style={{ marginRight: "0.5rem" }}/>
        </span>
        <span style={styles.traitsID}>
            <bdi>{title}</bdi>
        </span>
    </div>;
}

function EffectiveTraitSelectItem(props) {
    const {effectiveTrait, onChange, value} = props;

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

    return <div style={styles.traitElement}>
        <span style={styles.traitsIcon}>
            <FontAwesomeIcon icon={icon} style={{ marginRight: "0.5rem" }}/>
        </span>
        <span style={styles.traitsID}>
            <bdi><TraitID id={effectiveTrait.id} link={true} /></bdi>
        </span>
        <Radio.Group buttonStyle="solid" size="small" style={{display: 'flex'}}
            onChange={(e) => onChange(effectiveTrait, e.target.value)} value={value}>
            <Radio.Button value="-1">No</Radio.Button>
            <Radio.Button value="0">May</Radio.Button>
            <Radio.Button value="1">Yes</Radio.Button>
        </Radio.Group>
    </div>;
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
