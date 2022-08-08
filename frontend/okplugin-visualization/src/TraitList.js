import React from "react";
import { Button, Space } from 'antd';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faWrench, faPlug, faArchive, faFolder } from '@fortawesome/free-solid-svg-icons';
import _ from 'lodash';
import TraitID from "utils/TraitID";
import { Tree } from 'antd';
import { useLocalStorage } from 'utils/useLocalStorage';

function TraitList(props) {

    const {checked, onCheck, traitList} = props;

    const [expandedKeys, setExpandedKeys] = useLocalStorage('searchTraitsExpanded', []);

    const traitTuples = traitList
        .map(t => {return {trait: t, treeKey: t.id.split('.')};});
    function groupByTraitID(traitTuples, parentTreeKey) {
        return _.values(
            _.mapValues(
                _.groupBy(traitTuples, (t) => t.treeKey[0]),
                (subTraitTuples, treeKeyPrefix) => {
                    const currentTreeKey = [parentTreeKey, treeKeyPrefix].filter(x => x).join('.');

                    if (subTraitTuples.length === 1 && subTraitTuples[0].treeKey.length == 1) {
                        const subTraitTuple = subTraitTuples[0];
                        return {
                            title: treeKeyPrefix,//subTraitTuple.trait.id,//
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
                        <Button onClick={() => onCheck([])} size="small">Reset</Button>
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
                    onCheck={onCheck}
                    checkedKeys={checked}
                    checkable
                    selectable={false}
                    treeData={treeData}
                    titleRender={(nodeData)=> {
                        const {effectiveTrait, key, title} = nodeData;
                        if (!effectiveTrait)
                            return <EffectiveTraitSelectGroup key={key} title={title} />;
                        else
                            return <EffectiveTraitSelectItem key={key} title={title} effectiveTrait={effectiveTrait} />;
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
    const {effectiveTrait, title} = props;

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
        <span>
            <FontAwesomeIcon icon={icon} style={{ marginRight: "0.5rem" }}/>
        </span>
        <span style={styles.traitsID}>
            <bdi><TraitID id={effectiveTrait.id} title={title} link={true} /></bdi>
        </span>
    </div>;
}

export default TraitList;

const styles = {
    container: {
        display: "flex",
        overflowX: "hidden",
        overflowY: "scroll",
        paddingRight: "10px",
        width: '100%',
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
