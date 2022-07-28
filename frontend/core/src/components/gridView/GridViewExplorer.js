import React, { useState, useEffect, useCallback } from "react";
import { Input, Button, Popconfirm } from "antd";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faSearch, faSync } from "@fortawesome/free-solid-svg-icons";
import { withRouter, Link } from "react-router-dom";
import FeedbackMsg from "components/FeedbackMsg.js";
import _ from "lodash";

function GridViewExplorer(props) {
    const swaggerClient = props.swaggerClient;
    const apiVersion = props.apiVersion;

    const [loading, setLoading] = useState(true);
    const [context, setContext] = useState(null);
    const [searchString, setSearchString] = useState("");

    const [swaggerMsg, setSwaggerMsg] = useState("");
    const [swaggerErrorJson, setSwaggerErrorJson] = useState(false);
  
    // get contexts
    const refresh = useCallback(async () => {
        try {
            setLoading(true);
            // reload
            const context = await swaggerClient.apis.GridView.GridView_GetGridViewContexts({ version: apiVersion })
                .then((result) => result.body);
            setContext(context); // set context

            // INFO: don't show message on basic load
        } catch(e) {
            setSwaggerErrorJson(JSON.stringify(e.response, null, 2));
            setSwaggerMsg(e.toString());
        }
        setLoading(false);
    }, [swaggerClient, apiVersion])

    useEffect(() => {refresh();}, [refresh]);

    const refreshButton = (<Button onClick={refresh}><FontAwesomeIcon icon={faSync} spin={loading} color={"grey"} style={{ padding: "2px"}} /></Button>)

    return (
        <>
            <div style={{display: 'flex', justifyContent: 'center', marginTop: '50px', width: "205px", margin: "50px auto 0"}}>
                <Input suffix={<FontAwesomeIcon icon={faSearch} color="grey" />} placeholder='Search...' onChange={(e) => setSearchString(e.target.value)} />
                {refreshButton}
            </div>
            <div style={{flexGrow: 1, overflowY: 'auto', margin: '20px auto', minWidth: '50%'}}>
                {swaggerMsg && <FeedbackMsg alertProps={{message: swaggerMsg, type: swaggerErrorJson ? "error": "success", showIcon: true, banner: true}} swaggerErrorJson={swaggerErrorJson} />}
                {(context && context.contexts && !loading) ?
                    _.filter(context.contexts, c => 
                        _.lowerCase(c.speakingName.toString()).includes(_.lowerCase(searchString)) || 
                        _.lowerCase(c.id.toString()).includes(_.lowerCase(searchString)) || 
                        _.lowerCase(c.description.toString()).includes(_.lowerCase(searchString)))
                    .map((result, index) => {
                        return (
                                <div key={result.id} style={{display: 'flex', padding: '10px', backgroundColor: ((index % 2 === 0) ? '#eee' : '#fff')}}>
                                    <Link to={`explorer/${result.id}`} style={{display: "flex", flexGrow: '10', flexBasis: '0'}}>
                                        <div style={{flexGrow: '2', fontWeight: 'bold', flexBasis: '0'}}>{result.speakingName}</div>
                                        <div style={{flexGrow: '2', flexBasis: '0'}}>{result.id}</div>
                                        <div style={{flexGrow: '6', fontStyle: 'italic', flexBasis: '0', marginLeft: "0.5rem"}}>{result.description ?? 'No description.'}</div>
                                    </Link>
                                    <div style={{flexGrow: '4', flexBasis: '0', textAlign: "center", marginLeft: "0.5rem"}}>
                                        <Button htmlType="submit" type="primary" onClick={() => props.history.push(`edit-context/${result.id}`)}>Edit</Button>
                                        <Popconfirm
                                            title={`Are you sure to delete ${result.speakingName}?`}
                                            onConfirm={async () => {
                                                try {
                                                    await swaggerClient.apis.GridView.GridView_DeleteContext(
                                                            {
                                                                version: apiVersion,
                                                                name: result.id,
                                                            }
                                                        )
                                                        .then((result) => result.body);

                                                    setSwaggerErrorJson(false);
                                                    setSwaggerMsg("'" + result.id + "' has been removed.");
                                                    refresh(); // reload
                                                } catch(e) {
                                                    setSwaggerErrorJson(JSON.stringify(e.response, null, 2));
                                                    setSwaggerMsg(e.toString());
                                                }
                                            }}
                                            okText="Yes"
                                            okButtonProps={{type: "danger"}}
                                            cancelText="No"
                                            cancelButtonProps={{size: "normal"}}
                                            placement="topRight"
                                        >
                                            <Button htmlType="submit" type="danger" style={{ marginLeft: "0.5rem" }}>Remove</Button>
                                        </Popconfirm>
                                    </div>
                                </div>
                            );
                        })
                    : <>Loading...</>
                }
            </div>
        </>
    );
}

export default withRouter(GridViewExplorer);