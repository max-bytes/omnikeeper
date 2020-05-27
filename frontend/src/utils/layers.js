import { useQuery } from '@apollo/client';
import { queries } from 'graphql/queries';
import _ from 'lodash';

export function useExplorerLayers(skipInvisible = false, skipReadonly = false) {
    const { error, data, loading } = useQuery(queries.Layers);
    var { data: layerSettingsData } = useQuery(queries.LayerSettings);

    if (data && layerSettingsData) {
        let layers = mergeSettingsAndSortLayers(data.layers, layerSettingsData.layerSettings);

        if (skipInvisible)
            layers = layers.filter(l => l.visible);
        if (skipReadonly)
            layers = layers.filter(l => l.writable && l.state === 'ACTIVE');
            
        return {error: error, data: layers, loading: loading};
    }
    return {error: error, data: undefined, loading: loading};
}

export function mergeSettingsAndSortLayers(layers, layerSettings) {

    if (!layerSettings) {
        let baseSortOrder = 0;
        // no layer settings specified, choose convention to show all layers in default order
        layers = _.map(layers, layer => {
            return {...layer, visible: true, sort: baseSortOrder++};
        });
    } else {
        // add visibility settings
        layers = _.map(layers, layer => {
            var s = _.find(layerSettings, ls => ls.layerID === layer.id)
            if (s)
                return {...layer, visible: s.visible};
            else
                return {...layer, visible: true }; // default visible
        });

        // add sort order offset settings
        let baseSortOrder = 0;
        layers = _.map(layers, layer => {
            var s = _.find(layerSettings, ls => ls.layerID === layer.id);
            if (s)
                return {...layer, sort: baseSortOrder++ + s.sortOffset };
            else
                return {...layer, sort: baseSortOrder++ };
        });
    }

    // sort
    layers.sort((a,b) => {
        var o = b.sort - a.sort;
        if (o === 0) return ('' + a.name).localeCompare('' + b.name);
        return o;
    });
    return layers;
}