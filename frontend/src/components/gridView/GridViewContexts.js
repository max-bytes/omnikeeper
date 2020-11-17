import React, { useState, useEffect } from "react";
import { Input } from "antd";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faSearch } from "@fortawesome/free-solid-svg-icons";
import { withRouter, Link } from "react-router-dom";
import SwaggerClient from "swagger-client";
import env from "@beam-australia/react-env";
import _ from "lodash";

const swaggerDefUrl = `${env('BACKEND_URL')}/../swagger/v1/swagger.json`; // TODO, HACK: BACKEND_URL contains /graphql suffix, remove!
const apiVersion = 1;

function SearchCI(props) {
    const [context, setContext] = useState(null);
    const [searchString, setSearchString] = useState("");

    // get context
    useEffect(() => {
        const fetchContext = async () => {
            const context = await new SwaggerClient(swaggerDefUrl)
                .then((client) =>
                    client.apis.GridView.GetContexts({ version: apiVersion })
                )
                .then((result) => result.body);
            setContext(context); // set context
            };
        fetchContext();
    }, []);

    return (
        <div style={{display: 'flex', flexDirection: 'column', height: '100%'}}>
            <div style={{display: 'flex', justifyContent: 'center', marginTop: '50px', width: "205px", margin: "50px auto 0"}}>
                <Input suffix={<FontAwesomeIcon icon={faSearch} color="grey" />}  placeholder='Search...'  onChange={(e) => setSearchString(e.target.value)} />
            </div>
            <div style={{flexGrow: 1, overflowY: 'auto', margin: '20px auto', minWidth: '50%'}}>
                {(context && context.contexts) ?
                    _.filter(context.contexts, c => 
                        _.lowerCase(c.id.toString()).includes(_.lowerCase(searchString)) || 
                        _.lowerCase(c.speakingName.toString()).includes(_.lowerCase(searchString)) || 
                        _.lowerCase(c.name.toString()).includes(_.lowerCase(searchString)) || 
                        _.lowerCase(c.description.toString()).includes(_.lowerCase(searchString)))
                    .map((result, index) => {
                        return (
                            <Link key={result.name} to={`/grid-view/${result.name}`}>
                                <div style={{display: 'flex', padding: '10px', backgroundColor: ((index % 2 === 0) ? '#eee' : '#fff')}}>
                                    <div style={{flexGrow: '1', flexBasis: '0'}}>{result.id}</div>
                                        <div style={{flexGrow: '2', fontWeight: 'bold', flexBasis: '0'}}>{result.speakingName ?? result.name}</div>
                                    <div style={{flexGrow: '8', fontWeight: 'italic', flexBasis: '0'}}>{result.description ?? 'No description.'}</div>
                                </div>
                            </Link>);
                        })
                    : <>Loading...</>
                }
            </div>
        </div>
    );
}

export default withRouter(SearchCI);