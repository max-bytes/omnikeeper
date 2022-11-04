import { useQuery } from "@apollo/client";
import React from "react";
import { Descriptions, Spin, Tabs } from 'antd';
import { queries } from "../../graphql/queries";
import { useExplorerLayers } from "../../utils/layers";
import { useParams } from "react-router-dom";
import Attribute from "components/cis/Attribute";
import LayerIcon from "components/LayerIcon";
import { formatTimestamp } from "utils/datetime";
import UserTypeIcon from './../UserTypeIcon';
import Relation from "components/cis/Relation";
import _ from 'lodash';
import { ChangesetID, CIID } from "utils/uuidRenderers";
import CountBadge from "components/CountBadge";
import AutoSizedList from "utils/AutoSizedList";
import CITitle from "utils/CITitle";
import Text from "antd/lib/typography/Text";

const { TabPane } = Tabs;

export default function Changeset(props) {
    const { changesetID } = useParams();
    
    // TODO: loading, error-handling

    const { data: visibleLayers } = useExplorerLayers(true);

    const { error, data } = useQuery(queries.FullChangeset, {
        variables: { id: changesetID, layers: visibleLayers.map((l) => l.id) },
        skip: visibleLayers.length === 0
    });

    if (error) {
        // TODO: improve/generalize error handling
        return <pre>
            <span>Overall error: {error.message}</span><br />
            GraphQLErrors:<br /> 
            {error.graphQLErrors.map(({ message }, i) => (
            <span key={i}>{message}</span>
          ))}
        </pre>
    } else if (data && visibleLayers) {

        var defaultActiveTab = undefined;
        if (data.changeset.ciAttributes.length > 0) 
            defaultActiveTab = "newAttributes"; 
        else if (data.changeset.removedCIAttributes.length > 0) 
            defaultActiveTab = "removedAttributes";
        else if (data.changeset.relations.length > 0) 
            defaultActiveTab = "newRelations";
        else if (data.changeset.removedRelations.length > 0) 
            defaultActiveTab = "removedRelations";

        const NewAttributeItem = (index) => {
            const {ciid, ciName, attributes} = data.changeset.ciAttributes[index];
            return <div>
                <CITitle ciid={ciid} ciName={ciName} />
                {attributes.map(a => {
                    return <Attribute attribute={a} layerStack={[data.changeset.layer]} isEditable={false} visibleLayers={visibleLayers} hideNameLabel={false} controlIdSuffix="" key={a.id} />;
                })}
            </div>
        };

        const RemovedAttributeItem = (index) => {
            const {ciid, ciName, attributes} = data.changeset.removedCIAttributes[index];
            return <div>
                <CITitle ciid={ciid} ciName={ciName} />
                {attributes.map(a => {
                    return <Attribute removed={true} attribute={a} layerStack={[data.changeset.layer]} isEditable={false} visibleLayers={visibleLayers} hideNameLabel={false} controlIdSuffix="" key={a.id} />;
                })}
            </div>
        };
        
        const NewRelationItem = (index) => {
            const r = data.changeset.relations[index];
            return <Relation relation={r} layer={data.changeset.layer} key={r.id} />;
        };
        const RemovedRelationItem = (index) => {
            const r = data.changeset.removedRelations[index];
            return <Relation removed={true} relation={r} layer={data.changeset.layer} key={r.id} />;
        };

        return <div style={{marginTop: '1rem', flex: '1', display: 'flex', flexDirection: 'row', gap: '20px'}}>
            <Descriptions title="Changeset" bordered column={1} style={{ flexBasis: '35%'}}>
                <Descriptions.Item label="User"><UserTypeIcon userType={data.changeset.user.type} /> {data.changeset.user.displayName}</Descriptions.Item>
                <Descriptions.Item label="Timestamp">{formatTimestamp(data.changeset.timestamp)}</Descriptions.Item>
                <Descriptions.Item label="Layer"><LayerIcon layer={data.changeset.layer} /> {data.changeset.layer.id}</Descriptions.Item>
                <Descriptions.Item label="Origin-Type">{data.changeset.dataOrigin.type}</Descriptions.Item>
                <Descriptions.Item label="Changeset-ID"><ChangesetID id={data.changeset.id} link={false} copyable={true} /></Descriptions.Item>
                <Descriptions.Item label="Data-CIID">{(data.changeset.dataCIID) ? <CIID id={data.changeset.dataCIID} link={true} copyable={true} /> : <Text type="secondary">None</Text>}</Descriptions.Item>
            </Descriptions>
            <Tabs defaultActiveKey={defaultActiveTab} style={{paddingTop: "1rem", flexBasis: '65%'}}>
                <TabPane 
                 tab={<CountBadge count={_.sum(_.map(data.changeset.ciAttributes, (x) => x.attributes.length))}>New Attributes</CountBadge>}
                 key="newAttributes" disabled={data.changeset.ciAttributes.length === 0}>
                    <AutoSizedList itemCount={data.changeset.ciAttributes.length} item={NewAttributeItem} />
                </TabPane>
                <TabPane 
                 tab={<CountBadge count={_.sum(_.map(data.changeset.removedCIAttributes, (x) => x.attributes.length))}>Removed Attributes</CountBadge>}
                 key="removedAttributes" disabled={data.changeset.removedCIAttributes.length === 0}>
                    <AutoSizedList itemCount={data.changeset.removedCIAttributes.length} item={RemovedAttributeItem} />
                </TabPane>
                <TabPane 
                 tab={<CountBadge count={data.changeset.relations.length}>New Relations</CountBadge>}
                 key="newRelations" disabled={data.changeset.relations.length === 0}>
                    <AutoSizedList itemCount={data.changeset.relations.length} item={NewRelationItem} />
                </TabPane>
                <TabPane 
                 tab={<CountBadge count={data.changeset.removedRelations.length}>Removed Relations</CountBadge>}
                 key="removedRelations" disabled={data.changeset.removedRelations.length === 0}>
                    <AutoSizedList itemCount={data.changeset.removedRelations.length} item={RemovedRelationItem} />
                </TabPane>
            </Tabs>
        </div>;

    } else {
        return <div style={{display: "flex", height: "100%"}}><Spin spinning={true} size="large" tip="Loading...">&nbsp;</Spin></div>;
    }
}
