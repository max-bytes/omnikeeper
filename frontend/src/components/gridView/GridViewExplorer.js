import React, { useState, useEffect } from "react";
import { Input, Button, Popconfirm, Alert } from "antd";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faSearch } from "@fortawesome/free-solid-svg-icons";
import { withRouter, Link } from "react-router-dom";
import _ from "lodash";

function GridViewExplorer(props) {
    const swaggerJson = props.swaggerJson;
    const apiVersion = props.apiVersion;

    const [context, setContext] = useState(null);
    const [searchString, setSearchString] = useState("");

    const [swaggerMsg, setSwaggerMsg] = useState("")
    const [swaggerError, setSwaggerError] = useState(false)

    // get context
    useEffect(() => {
        if (swaggerJson) {
            const fetchContext = async () => {
                const context = await swaggerJson.apis.GridView.GetContexts({ version: apiVersion })
                    .then((result) => result.body);
                setContext(context); // set context
                };
            fetchContext();
        }
    }, [swaggerJson, apiVersion]);

    return (
        <>
            <div style={{display: 'flex', justifyContent: 'center', marginTop: '50px', width: "205px", margin: "50px auto 0"}}>
                <Input suffix={<FontAwesomeIcon icon={faSearch} color="grey" />}  placeholder='Search...'  onChange={(e) => setSearchString(e.target.value)} />
            </div>
            <div style={{flexGrow: 1, overflowY: 'auto', margin: '20px auto', minWidth: '50%'}}>
                {swaggerMsg && <Alert message={swaggerMsg} type={swaggerError ? "error": "success"} showIcon banner/>}
                {(context && context.contexts) ?
                    _.filter(context.contexts, c => 
                        _.lowerCase(c.speakingName.toString()).includes(_.lowerCase(searchString)) || 
                        _.lowerCase(c.name.toString()).includes(_.lowerCase(searchString)) || 
                        _.lowerCase(c.description.toString()).includes(_.lowerCase(searchString)))
                    .map((result, index) => {
                        return (
                                <div key={result.name} style={{display: 'flex', padding: '10px', backgroundColor: ((index % 2 === 0) ? '#eee' : '#fff')}}>
                                    <Link to={`/explorer/${result.name}`} style={{display: "flex", flexGrow: '10', flexBasis: '0'}}>
                                        <div style={{flexGrow: '3', fontWeight: 'bold', flexBasis: '0'}}>{result.speakingName ?? result.name}</div>
                                        <div style={{flexGrow: '8', fontWeight: 'italic', flexBasis: '0', marginLeft: "0.5rem"}}>{result.description ?? 'No description.'}</div>
                                    </Link>
                                    <div style={{flexGrow: '3', flexBasis: '0', textAlign: "center", marginLeft: "0.5rem"}}>
                                        <Button htmlType="submit" type="primary" onClick={() => props.history.push(`/edit-context/${result.name}`)}>Edit</Button>
                                        <Popconfirm
                                            title={`Are you sure to delete ${result.speakingName}?`}
                                            onConfirm={async () => {
                                                try {
                                                    if (swaggerJson) {
                                                        await swaggerJson.apis.GridView.DeleteContext(
                                                                    {
                                                                        version: apiVersion,
                                                                        name: result.name,
                                                                    }
                                                                )
                                                            .then((result) => result.body);

                                                            setSwaggerError(false);
                                                            setSwaggerMsg("'" + result.name + "' has been removed.");
                                                    }
                                                } catch(e) { // TODO: find a way to get HTTP-Error-Code and -Msg and give better feedback!
                                                    setSwaggerError(true);
                                                    setSwaggerMsg(e.toString());
                                                }
                                                
                                                // reload
                                                const context = await swaggerJson.apis.GridView.GetContexts({ version: apiVersion })
                                                    .then((result) => result.body);
                                                setContext(context); // set context
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