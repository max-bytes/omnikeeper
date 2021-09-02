import { useParams } from "react-router-dom";
import React from "react";
import { queries } from "graphql/queries";
import { useQuery } from "@apollo/client";
import { Descriptions, Tabs } from "antd";
import TraitID from "utils/TraitID";
import CountBadge from "components/CountBadge";
import ReactJson from "react-json-view";
import _ from 'lodash';

const { TabPane } = Tabs;

function omitDeep(collection, excludeKeys) {
    function omitFn(value) {
      if (value && typeof value === 'object') {
        excludeKeys.forEach((key) => {
          delete value[key];
        });
      }
    }
    return _.cloneDeepWith(collection, omitFn);
  }

export default function Trait(props) {
    const { traitID } = useParams();

    const { data, error } = useQuery(queries.ActiveTrait, { variables: {id: traitID}});
    
    if (error) {
        // TODO: improve/generalize error handling
        return <pre>
            <span>Overall error: {error.message}</span><br />
            GraphQLErrors:<br /> 
            {error.graphQLErrors.map(({ message }, i) => (
            <span key={i}>{message}</span>
          ))}
        </pre>
    } else if (data) {

        const ancestorTraits = data.activeTrait.ancestorTraits
            .map(t => <TraitID key={t} id={t} link={true} />)
            .reduce((prev, curr) => prev === '' ? curr : [prev, ', ', curr], '');

        return <div style={{marginTop: '10px'}}>
            <Descriptions title="Trait" bordered column={2}>
                <Descriptions.Item label="Trait-ID"><TraitID id={data.activeTrait.id} link={false} /></Descriptions.Item>
                <Descriptions.Item label="Origin-Type">{data.activeTrait.origin.type}</Descriptions.Item>
                <Descriptions.Item label="Ancestor Traits" span={2}>{ancestorTraits}</Descriptions.Item>
            </Descriptions>
            <Tabs defaultActiveKey={"Required Attributes"} style={{paddingTop: "1rem"}}>
                <TabPane 
                    tab={<CountBadge count={data.activeTrait.requiredAttributes.length}>Required Attributes</CountBadge>}
                    key="Required Attributes" disabled={data.activeTrait.requiredAttributes.length === 0}>
                        {data.activeTrait.requiredAttributes.map((r, index) => {
                            return <ReactJson key={index} collapsed={false} name={false} src={omitDeep(JSON.parse(r), ["$type"])} enableClipboard={false} />
                        })}
                </TabPane>
                <TabPane 
                    tab={<CountBadge count={data.activeTrait.optionalAttributes.length}>Optional Attributes</CountBadge>}
                    key="Optional Attributes" disabled={data.activeTrait.optionalAttributes.length === 0}>
                        {data.activeTrait.optionalAttributes.map((r, index) => {
                            return <ReactJson key={index} collapsed={false} name={false} src={omitDeep(JSON.parse(r), ["$type"])} enableClipboard={false} />
                        })}
                </TabPane>
                <TabPane 
                    tab={<CountBadge count={data.activeTrait.requiredRelations.length}>Required Relations</CountBadge>}
                    key="Required Relations" disabled={data.activeTrait.requiredRelations.length === 0}>
                        {data.activeTrait.requiredRelations.map((r, index) => {
                            return <ReactJson key={index} collapsed={false} name={false} src={omitDeep(JSON.parse(r), ["$type"])} enableClipboard={false} />
                        })}
                </TabPane>
            </Tabs>
        </div>;

    } else {
        return "Loading...";
    }
}
