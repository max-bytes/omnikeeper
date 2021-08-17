import { useQuery } from "@apollo/client";
import React from "react";
import { Descriptions, Tabs, Typography } from 'antd';
import { queries } from "../../graphql/queries";
import { useExplorerLayers } from "../../utils/layers";
import { useParams } from "react-router-dom";
import Attribute from "components/Attribute";
import LayerIcon from "components/LayerIcon";
import { formatTimestamp } from "utils/datetime";
import UserTypeIcon from './../UserTypeIcon';
import Relation from "components/Relation";
import _ from 'lodash';
import { ChangesetID, CIID } from "utils/uuidRenderers";

const { TabPane } = Tabs;
const { Title } = Typography;

export default function Changeset(props) {
    const { changesetID } = useParams();
    
    const { data: visibleLayers, loading: loadingLayers } = useExplorerLayers(true);

    const { loading, error, data } = useQuery(queries.FullChangeset, {
        variables: { id: changesetID }
    });
    
    // TODO: loading, error
    const { loading: loadingPredicates, error: errorPredicates, data: dataPredicates } = useQuery(queries.PredicateList, { variables: {} });


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

        return <div style={{margin: '10px'}}>
            <Descriptions title="Changeset" bordered column={2}>
                <Descriptions.Item label="User"><UserTypeIcon userType={data.changeset.user.type} /> {data.changeset.user.displayName}</Descriptions.Item>
                <Descriptions.Item label="Timestamp">{formatTimestamp(data.changeset.timestamp)}</Descriptions.Item>
                <Descriptions.Item label="Layer"><LayerIcon layer={data.changeset.layer} /> {data.changeset.layer.id}</Descriptions.Item>
                <Descriptions.Item label="Changeset-ID"><ChangesetID id={data.changeset.id} link={false} /></Descriptions.Item>
            </Descriptions>
            <Tabs defaultActiveKey={(data.changeset.attributes.length === 0) ? "relations" : "attributes"} style={{padding: "1rem"}}>
                <TabPane tab={`Attributes (${data.changeset.attributes.length})`} key="attributes" disabled={data.changeset.attributes.length === 0}>
                    {_.values(_.mapValues(groupedAttributesByCIID, (attributes, ciid) => {
                        return <div key={ciid} style={{marginTop: '1.5rem'}}>
                            <Title level={5} style={{marginBottom: 0}}>CI <CIID id={ciid} link={true} /></Title>
                            {attributes.map(a => {
                                return <Attribute attribute={a} layerStack={[data.changeset.layer]} isEditable={false} visibleLayers={visibleLayers} hideNameLabel={false} controlIdSuffix="" />;
                            })}
                        </div>;
                    }))}
                </TabPane>
                <TabPane tab={`Relations (${data.changeset.relations.length})`} key="relations" disabled={data.changeset.relations.length === 0}>
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
