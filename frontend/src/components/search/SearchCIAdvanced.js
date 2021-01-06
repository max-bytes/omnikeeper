import { useLazyQuery } from "@apollo/client";
import React, { useState, useEffect, useCallback } from "react";
import { queries } from "../../graphql/queries";
import { Input } from "semantic-ui-react";
import ExplorerLayers from "../ExplorerLayers.js";
import { SearchResults } from "./SearchResults.js";
import { useExplorerLayers } from "../../utils/layers";
import EffectiveTraitList from "./EffectiveTraitList.js";
import { withRouter } from "react-router-dom";
import queryString from 'query-string';
import { Spin } from 'antd';
import { useLocation } from 'react-router-dom'
import _ from 'lodash';

function SearchCIAdvanced(props) {
    let urlParams = parseURLQuery(useLocation().search);

    const { data: visibleLayers, loading: loadingLayers } = useExplorerLayers(true);

    const [searchString, setSearchString] = useState(urlParams.searchString);
    

    const [loadEffectiveTraits, { data: effectiveTraits, loading: loadingActiveTraits }] = useLazyQuery(queries.ActiveTraits);
    useEffect(() => {
        if (visibleLayers) {
            loadEffectiveTraits({});
        }
    }, [loadEffectiveTraits, visibleLayers]);
    
    var [checkedTraits, setCheckedTraits] = useState([]);
    useEffect(() => {
        if (_.keys(checkedTraits).length === 0 && effectiveTraits) {
            const newChecked = [];
            for(const et of effectiveTraits.activeTraits) {
                if (urlParams.requiredTraits.includes(et))
                    newChecked[et] = 1;
                else if (urlParams.disallowedTraits.includes(et))
                    newChecked[et] = -1;
                else
                    newChecked[et] = 0;
            }
            setCheckedTraits(newChecked);
        }
    }, [effectiveTraits, checkedTraits, setCheckedTraits, urlParams]);
    
    useEffect(() => {
        if (_.keys(checkedTraits).length !== 0) // we only start updating the url/history once the checkedTraits are loaded
        {
            const search = stringifyURLQuery(searchString, checkedTraits);
            props.history.replace({search: `?${search}`});
        }
      }, [searchString, checkedTraits, props.history]);

    // TODO: we would like to keep the old data around while loading, and recent graphql (3.3.0+) recommends using
    // return variable "previousData"... but there is a bug that prevents this from working with useLazyQuery:
    // https://github.com/apollographql/apollo-client/issues/7396
    // update graphql and implement previousData as soon as bug is fixed
    // NOTE: caching big results is really slow with apollo, so we completely bypass the cache, hence fetchPolicy: "no-cache"
    const [search, { loading, data: dataCIs }] = useLazyQuery(queries.AdvancedSearchCIs, {fetchPolicy: "no-cache"});

    // debounce search, so its not called too often
    const debouncedSearch = useCallback(_.debounce(search, 500), [search]);

    useEffect(() => {
        if (effectiveTraits && visibleLayers) {
            // TODO: cancel previous searches -> see: https://evilmartians.com/chronicles/aborting-queries-and-mutations-in-react-apollo
            // but... it seems like a clusterfuck :(
            debouncedSearch({
                variables: {
                    searchString: searchString,
                    withEffectiveTraits: effectiveTraits.activeTraits
                        .filter((et) => checkedTraits[et] === 1),
                    withoutEffectiveTraits: effectiveTraits.activeTraits
                        .filter((et) => checkedTraits[et] === -1),
                    layers: visibleLayers.map((l) => l.name),
                }
            });
        }
    }, [searchString, debouncedSearch, effectiveTraits, visibleLayers, checkedTraits]);

    return (
        <div style={styles.container}>
            <Spin
                spinning={loadingLayers || loadingActiveTraits}>
                {/* left column - search */}
                <div style={styles.searchRow}>
                    <div style={styles.searchRowEntry}>
                        <h4>Layers</h4>
                        <ExplorerLayers />
                    </div>
                    <div style={styles.searchRowEntry}>
                        <h4>Name or CI-ID</h4>
                        <Input
                            style={styles.searchField}
                            icon="search"
                            placeholder="Search..."
                            value={searchString}
                            onChange={(e, { value }) => setSearchString(value)}
                        />
                    </div>
                    <div style={styles.searchRowEntry}>
                        {effectiveTraits && 
                            <EffectiveTraitList effectiveTraitList={effectiveTraits.activeTraits} checked={checkedTraits} setChecked={setCheckedTraits} />
                        }
                    </div>
                </div>
                {/* right column - results */}
                <div style={styles.resultsRow}>
                    <SearchResults advancedSearchCIs={dataCIs?.advancedSearchCIs} loading={loading} />
                </div>
            </Spin>
        </div>
    );
}

export default withRouter(SearchCIAdvanced);

function stringifyURLQuery(searchString, checkedTraits) {
    return queryString.stringify({
        searchString: (searchString) ? searchString : undefined,
        rt: _.toPairs(checkedTraits).filter(t => t[1] === 1).map(t => t[0]),
        dt: _.toPairs(checkedTraits).filter(t => t[1] === -1).map(t => t[0]),
    }, {arrayFormat: 'comma'});
  }

function parseURLQuery(search) {
    const p = queryString.parse(search, {arrayFormat: 'comma'});
  
    return {
      searchString: p.searchString ?? "",
      requiredTraits: p.rt ?? [],
      disallowedTraits: p.dt ?? [],
    };
  }

const styles = {
    container: {
        display: "flex",
        flexDirection: "row",
        height: "100%",
    },
    // left column - search
    searchRow: {
        display: "flex",
        flexDirection: "column",
        padding: "10px",
        overflowY: "auto",
        width: "30%",
        minWidth: "300px",
    },
    searchRowEntry: {
        marginBottom: "20px",
    },
    searchField: {
        width: "100%",
    },
    // right column - results
    resultsRow: {
        display: "flex",
        flexDirection: "column",
        padding: "10px",
        flex: "1 1 auto",
    },
};
