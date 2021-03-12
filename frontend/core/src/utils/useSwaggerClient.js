import { useState, useEffect, useCallback } from "react";
import env from "@beam-australia/react-env";
import SwaggerClient from "swagger-client";

export default function useSwaggerClient() {
    const swaggerDefUrl = `${env('BACKEND_URL')}/../swagger/v1/swagger.json`; // HACK: BACKEND_URL contains /graphql suffix, remove!
    const [swaggerClient, setSwaggerClient] = useState(null);

    // get swagger JSON
    // NOTE: we use a useEffect to (re)load the client itself
    // to make the client usable for others, the getSwaggerClient() callback is used
    // the reason this callback exists is for setting the correct, updated access token
    useEffect(() => { 
        try {
            const token = localStorage.getItem('token');
            new SwaggerClient(swaggerDefUrl, {
                authorizations: {
                    oauth2: { token: { access_token: token } },
                }
            }).then(d => {
                setSwaggerClient(d);
            });
        } catch(e) {
            return { data: null, loading: false, error: e };
        }
    }, [swaggerDefUrl]);
    const getSwaggerClient = useCallback(() => {
        // update token before returning the client
        swaggerClient.authorizations.oauth2.token.access_token = localStorage.getItem('token');
        return swaggerClient;
    }, [swaggerClient]);

    if (!swaggerClient) return { data: null, loading: true, error: null };

    try {
        return { data: getSwaggerClient(), loading: false, error: null };
    } catch(e) {
        return { data: null, loading: false, error: e };
    }
}