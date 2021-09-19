import React, { useCallback } from "react";
import { createGraphiQLFetcher } from '@graphiql/toolkit';
import GraphiQL from "graphiql";
import env from "@beam-australia/react-env";
import 'graphiql/graphiql.min.css';

export default function GraphQLPlayground(props) {
    const fetcher = useCallback((graphQLParams) => {
        const token = localStorage.getItem('token');
        const f = createGraphiQLFetcher({
            url: env('BACKEND_URL') + "/graphql",
            headers: {
                ...(token ? {authorization: `Bearer ${token}`} : {}),
            }
        });
        return f(graphQLParams);
    }, []);
    return <GraphiQL fetcher={fetcher} />;
}