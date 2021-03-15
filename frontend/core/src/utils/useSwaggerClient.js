import { useState, useEffect } from "react";
import env from "@beam-australia/react-env";
import SwaggerClient from "swagger-client";

export default function useSwaggerClient() {
    const swaggerDefUrl = `${env('BACKEND_URL')}/../swagger/v1/swagger.json`; // HACK: BACKEND_URL contains /graphql suffix, remove!
    const [swaggerClient, setSwaggerClient] = useState(null);

    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    // get swagger JSON
    useEffect(() => {
        setLoading(true);
        try {
            const token = localStorage.getItem('token');
            new SwaggerClient(swaggerDefUrl, {
                authorizations: {
                    oauth2: { token: { access_token: token } },
                }
            }).then(d => {
                setSwaggerClient(d);
                setLoading(false);
            });
        } catch(e) {
            setError(e);
        }
    }, [swaggerDefUrl]);

    // update token before returning the client
    if (swaggerClient) swaggerClient.authorizations.oauth2.token.access_token = localStorage.getItem('token');

    return { data: swaggerClient, loading: loading, error: error };
}