import { useQuery } from "@apollo/client";
import React from "react";
import { Descriptions, Tabs, Typography } from 'antd';
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

const { TabPane } = Tabs;
const { Title } = Typography;

export default function Changeset(props) {
    const { changesetID } = useParams();
    
    // TODO: loading, error-handling

    const { data: visibleLayers } = useExplorerLayers(true);

    const { error, data } = useQuery(queries.FullChangeset, {
        variables: { id: changesetID, layers: visibleLayers.map((l) => l.id) },
        skip: visibleLayers.length === 0
    });
    
    const { data: dataPredicates } = useQuery(queries.PredicateList, { variables: {} });


    if (error) {
        // TODO: improve/generalize error handling
        return <pre>
            <span>Overall error: {error.message}</span><br />
            GraphQLErrors:<br /> 
            {error.graphQLErrors.map(({ message }, i) => (
            <span key={i}>{message}</span>
          ))}
        </pre>
    } else if (data && visibleLayers && dataPredicates) {

        const groupedAttributesByCIID = _.values(_.mapValues(_.groupBy(data.changeset.attributes, a => a.ciid), (attributes, ciid) => {
            return {
                ciid: ciid,
                attributes: attributes
            };
        }));
        const groupedRemovedAttributesByCIID = _.values(_.mapValues(_.groupBy(data.changeset.removedAttributes, a => a.ciid), (attributes, ciid) => {
            return {
                ciid: ciid,
                attributes: attributes
            };
        }));

        var defaultActiveTab = undefined;
        if (data.changeset.attributes.length > 0) 
            defaultActiveTab = "newAttributes"; 
        else if (data.changeset.removedAttributes.length > 0) 
            defaultActiveTab = "removedAttributes";
        else if (data.changeset.relations.length > 0) 
            defaultActiveTab = "newRelations";
        else if (data.changeset.removedRelations.length > 0) 
            defaultActiveTab = "removedRelations";

        const NewAttributeItem = (index) => {
            const {ciid, attributes} = groupedAttributesByCIID[index];
            return <div>
                <Title level={5} style={{marginBottom: 0}}>CI <CIID id={ciid} link={true} /></Title>
                {attributes.map(a => {
                    return <Attribute attribute={a} layerStack={[data.changeset.layer]} isEditable={false} visibleLayers={visibleLayers} hideNameLabel={false} controlIdSuffix="" key={a.id} />;
                })}
            </div>
        };

        const RemovedAttributeItem = (index) => {
            const {ciid, attributes} = groupedRemovedAttributesByCIID[index];
            return <div>
                <Title level={5} style={{marginBottom: 0}}>CI <CIID id={ciid} link={true} /></Title>
                {attributes.map(a => {
                    return <Attribute removed={true} attribute={a} layerStack={[data.changeset.layer]} isEditable={false} visibleLayers={visibleLayers} hideNameLabel={false} controlIdSuffix="" key={a.id} />;
                })}
            </div>
        };
        
        const NewRelationItem = (index) => {
            const r = data.changeset.relations[index];
            return <Relation predicates={dataPredicates.predicates} relation={r} layer={data.changeset.layer} key={r.id} />;
        };
        const RemovedRelationItem = (index) => {
            const r = data.changeset.removedRelations[index];
            return <Relation removed={true} predicates={dataPredicates.predicates} relation={r} layer={data.changeset.layer} key={r.id} />;
        };

        return <div style={{marginTop: '1rem', flex: '1', display: 'flex', flexDirection: 'row', gap: '20px'}}>
            <Descriptions title="Changeset" bordered column={1} style={{ flexBasis: '35%'}}>
                <Descriptions.Item label="User"><UserTypeIcon userType={data.changeset.user.type} /> {data.changeset.user.displayName}</Descriptions.Item>
                <Descriptions.Item label="Timestamp">{formatTimestamp(data.changeset.timestamp)}</Descriptions.Item>
                <Descriptions.Item label="Layer"><LayerIcon layer={data.changeset.layer} /> {data.changeset.layer.id}</Descriptions.Item>
                <Descriptions.Item label="Origin-Type">{data.changeset.dataOrigin.type}</Descriptions.Item>
                <Descriptions.Item label="Changeset-ID"><ChangesetID id={data.changeset.id} link={false} /></Descriptions.Item>
            </Descriptions>
            <Tabs defaultActiveKey={defaultActiveTab} style={{paddingTop: "1rem", flexBasis: '65%'}}>
                <TabPane 
                 tab={<CountBadge count={data.changeset.attributes.length}>New Attributes</CountBadge>}
                 key="newAttributes" disabled={data.changeset.attributes.length === 0}>
                    <AutoSizedList itemCount={groupedAttributesByCIID.length} item={NewAttributeItem} />
                </TabPane>
                <TabPane 
                 tab={<CountBadge count={data.changeset.removedAttributes.length}>Removed Attributes</CountBadge>}
                 key="removedAttributes" disabled={data.changeset.removedAttributes.length === 0}>
                    <AutoSizedList itemCount={groupedRemovedAttributesByCIID.length} item={RemovedAttributeItem} />
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
        return "Loading...";
    }
}
