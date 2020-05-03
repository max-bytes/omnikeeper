import { useQuery } from '@apollo/client';
import { queries } from '../graphql/queries'

export function useLayers(skipInvisible = false, skipReadonly = false) {
    const { error, data, loading } = useQuery(queries.Layers);

    if (data) {
        // sort based on order
        let layers = data.layers.concat();
        layers.sort((a,b) => {
            var o = b.sort - a.sort;
            if (o === 0) return ('' + a.name).localeCompare(b.name);
            return o;
        });
        if (skipInvisible)
            layers = layers.filter(l => l.visibility);
        if (skipReadonly)
            layers = layers.filter(l => l.writable && l.state === 'ACTIVE');
        return {error: error, data: layers, loading: loading};
    }
    return {error: error, data: [], loading: loading};
}