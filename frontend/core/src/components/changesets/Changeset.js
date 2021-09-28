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

        const groupedAttributesByCIID = _.groupBy(data.changeset.attributes, a => a.ciid);
        const groupedRemovedAttributesByCIID = _.groupBy(data.changeset.removedAttributes, a => a.ciid);

        return <div style={{marginTop: '1rem'}}>
            <Descriptions title="Changeset" bordered column={2}>
                <Descriptions.Item label="User"><UserTypeIcon userType={data.changeset.user.type} /> {data.changeset.user.displayName}</Descriptions.Item>
                <Descriptions.Item label="Timestamp">{formatTimestamp(data.changeset.timestamp)}</Descriptions.Item>
                <Descriptions.Item label="Layer"><LayerIcon layer={data.changeset.layer} /> {data.changeset.layer.id}</Descriptions.Item>
                <Descriptions.Item label="Origin-Type">{data.changeset.dataOrigin.type}</Descriptions.Item>
                <Descriptions.Item label="Changeset-ID" span={2}><ChangesetID id={data.changeset.id} link={false} /></Descriptions.Item>
            </Descriptions>
            <Tabs defaultActiveKey={(data.changeset.attributes.length > 0) ? "newAttributes" : ((data.changeset.removedAttributes.length > 0) ? "removedAttributes" : "relations")} style={{paddingTop: "1rem"}}>
                <TabPane 
                 tab={<CountBadge count={data.changeset.attributes.length}>New Attributes</CountBadge>}
                 key="newAttributes" disabled={data.changeset.attributes.length === 0}>
                    {_.values(_.mapValues(groupedAttributesByCIID, (attributes, ciid) => {
                        return <div key={ciid} style={{marginTop: '1.5rem'}}>
                            <Title level={5} style={{marginBottom: 0}}>CI <CIID id={ciid} link={true} /></Title>
                            {attributes.map(a => {
                                return <Attribute attribute={a} layerStack={[data.changeset.layer]} isEditable={false} visibleLayers={visibleLayers} hideNameLabel={false} controlIdSuffix="" key={a.id} />;
                            })}
                        </div>;
                    }))}
                </TabPane>
                <TabPane 
                 tab={<CountBadge count={data.changeset.removedAttributes.length}>Removed Attributes</CountBadge>}
                 key="removedAttributes" disabled={data.changeset.removedAttributes.length === 0}>
                    {_.values(_.mapValues(groupedRemovedAttributesByCIID, (attributes, ciid) => {
                        return <div key={ciid} style={{marginTop: '1.5rem'}}>
                            <Title level={5} style={{marginBottom: 0}}>CI <CIID id={ciid} link={true} /></Title>
                            {attributes.map(a => {
                                return <Attribute removed={true} attribute={a} layerStack={[data.changeset.layer]} isEditable={false} visibleLayers={visibleLayers} hideNameLabel={false} controlIdSuffix="" key={a.id} />;
                            })}
                        </div>;
                    }))}
                </TabPane>
                <TabPane 
                 tab={<CountBadge count={data.changeset.relations.length}>Relations</CountBadge>}
                 key="relations" disabled={data.changeset.relations.length === 0}>
                    {data.changeset.relations.map(r => {
                        return <Relation predicates={dataPredicates.predicates} relation={r} layer={data.changeset.layer} key={r.id} />;
                    })}
                </TabPane>
            </Tabs>
        </div>;

    } else {
        return "Loading...";
    }
}
