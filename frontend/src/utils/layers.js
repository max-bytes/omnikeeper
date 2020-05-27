import { useQuery } from '@apollo/client';
import { queries } from 'graphql/queries';
import _ from 'lodash';

export function useExplorerLayers(skipInvisible = false, skipReadonly = false) {
    const { error, data, loading } = useQuery(queries.Layers);
    var { data: visibleLayersData } = useQuery(queries.VisibleLayers);
    var { data: { layerSortOffsets } } = useQuery(queries.LayerSortOffsets);

    if (data && visibleLayersData && layerSortOffsets) {
        let layers = mergeAndSortLayers(data.layers, visibleLayersData.visibleLayers, layerSortOffsets);

        if (skipInvisible)
            layers = layers.filter(l => l.visible);
        if (skipReadonly)
            layers = layers.filter(l => l.writable && l.state === 'ACTIVE');
            
        return {error: error, data: layers, loading: loading};
    }
    return {error: error, data: undefined, loading: loading};
}

export function mergeAndSortLayers(layers, visibleLayers, layerSortings) {

    if (!visibleLayers) {
        // no visible layers specified, choose convention to show all layers
        layers = _.map(layers, layer => {
            return {...layer, visible: true};
        });
    } else {
        // add visibility settings
        layers = _.map(layers, layer => {
            if (_.includes(visibleLayers, layer.id))
                return {...layer, visible: true};
            else
                return {...layer, visible: false};
        });
    }

    // add sort order offset settings
    var baseSortOrder = 0;
    layers = _.map(layers, layer => {
        var s = _.find(layerSortings, ls => ls.layerID === layer.id);
        if (s)
            return {...layer, sort: baseSortOrder++ + s.sortOffset };
        else
            return {...layer, sort: baseSortOrder++ };
    });

    // sort
    layers.sort((a,b) => {
        var o = b.sort - a.sort;
        if (o === 0) return ('' + a.name).localeCompare('' + b.name);
        return o;
    });
    return layers;
}