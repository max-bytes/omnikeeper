import { useLazyQuery } from "@apollo/client";
import React, { useState, useEffect, useCallback } from "react";
import { queries } from "../../graphql/queries";
import { SearchResults } from "./SearchResults.js";
import { useExplorerLayers } from "../../utils/layers";
import TraitList from "./TraitList.js";
import { withRouter } from "react-router-dom";
import queryString from 'query-string';
import { Spin, Form, Input } from 'antd';
import { useLocation } from 'react-router-dom'
import _ from 'lodash';

function SearchCIAdvanced(props) {
    let urlParams = parseURLQuery(useLocation().search);

    const { data: visibleLayers, loading: loadingLayers } = useExplorerLayers(true);

    const [searchString, setSearchString] = useState(urlParams.searchString);
    const [showMetaTraits, setShowMetaTraits] = useState(urlParams.showMetaTraits);
    const [showEmptyTrait, setShowEmptyTrait] = useState(urlParams.showEmptyTrait);
    

    const [loadActiveTraits, { data: activeTraits, loading: loadingActiveTraits }] = useLazyQuery(queries.ActiveTraits);
    useEffect(() => {
        if (visibleLayers) {
            loadActiveTraits({});
        }
    }, [loadActiveTraits, visibleLayers]);
    
    var [initialSetOfCheckedTraits, setIitialSetOfCheckedTraits] = useState(false); // HACK: shouldn't need this flag
    var [checkedTraits, setCheckedTraits] = useState([]);
    useEffect(() => {
        if (!initialSetOfCheckedTraits && activeTraits) {
            const newChecked = [];
            for(const et of activeTraits.activeTraits) {
                if (urlParams.requiredTraits.includes(et.id))
                    newChecked[et.id] = 1;
                else if (urlParams.disallowedTraits.includes(et.id))
                    newChecked[et.id] = -1;
                else
                    newChecked[et.id] = 0;
            }
            setCheckedTraits(newChecked);
            setIitialSetOfCheckedTraits(true);
        }
    }, [activeTraits, checkedTraits, setCheckedTraits, urlParams, initialSetOfCheckedTraits, setIitialSetOfCheckedTraits]);

    
    useEffect(() => {
        if (_.keys(checkedTraits).length !== 0) // we only start updating the url/history once the checkedTraits are loaded
        {
            const search = stringifyURLQuery(searchString, checkedTraits, showMetaTraits, showEmptyTrait);
            props.history.replace({search: `?${search}`});
        }
      }, [searchString, checkedTraits, showMetaTraits, showEmptyTrait, props.history]);

    // TODO: we would like to keep the old data around while loading, and recent graphql (3.3.0+) recommends using
    // return variable "previousData"... but there is a bug that prevents this from working with useLazyQuery:
    // https://github.com/apollographql/apollo-client/issues/7396
    // update graphql and implement previousData as soon as bug is fixed
    // NOTE: caching big results is really slow with apollo, so we completely bypass the cache, hence fetchPolicy: "no-cache"
    const [search, { loading, data: dataCIs }] = useLazyQuery(queries.SearchCIs, {fetchPolicy: "no-cache"});

    // debounce search, so its not called too often
    const debouncedSearch = useCallback(_.debounce(search, 500), [search]);

    useEffect(() => {
        if (activeTraits && visibleLayers) {
            // TODO: cancel previous searches -> see: https://evilmartians.com/chronicles/aborting-queries-and-mutations-in-react-apollo
            // but... it seems like a clusterfuck :(
            debouncedSearch({
                variables: {
                    searchString: searchString,
                    withEffectiveTraits: activeTraits.activeTraits.map(et => et.id)
                        .filter((et) => checkedTraits[et] === 1),
                    withoutEffectiveTraits: activeTraits.activeTraits.map(et => et.id)
                        .filter((et) => checkedTraits[et] === -1),
                    layers: visibleLayers.map((l) => l.id),
                }
            });
        }
    }, [searchString, debouncedSearch, activeTraits, visibleLayers, checkedTraits]);

    return (
        <div style={styles.container}>
            <Spin
                spinning={loadingLayers || loadingActiveTraits}>
                {/* left column - search */}
                <div style={styles.searchColumn}>
                    <h2>Search CIs</h2>
                    <div style={styles.searchColumnEntry}>
                        <h4>Name or CI-ID</h4>
                        <Form.Item initialValue={searchString ?? ""} style={{ marginBottom: 0 }}>
                            <Input
                                style={styles.searchField}
                                icon="search"
                                placeholder="Search..."
                                value={searchString ?? ""}
                                onChange={(e) => setSearchString(e.target.value)}
                            />
                        </Form.Item>
                    </div>
                    <div style={styles.searchColumnEntry}>
                        {activeTraits && 
                            <TraitList traitList={activeTraits.activeTraits} 
                            checked={checkedTraits} setChecked={setCheckedTraits}
                            showMetaTraits={showMetaTraits} setShowMetaTraits={setShowMetaTraits}
                            showEmptyTrait={showEmptyTrait} setShowEmptyTrait={setShowEmptyTrait} />
                        }
                    </div>
                </div>
                {/* right column - results */}
                <div style={styles.resultsColumn}>
                    <SearchResults cis={dataCIs?.cis} loading={loading} />
                </div>
            </Spin>
        </div>
    );
}

export default withRouter(SearchCIAdvanced);

function stringifyURLQuery(searchString, checkedTraits, showMetaTraits, showEmptyTrait) {
    return queryString.stringify({
        searchString: (searchString) ? searchString : undefined,
        rt: _.toPairs(checkedTraits).filter(t => t[1] === 1).map(t => t[0]),
        dt: _.toPairs(checkedTraits).filter(t => t[1] === -1).map(t => t[0]),
        mt: (showMetaTraits) ? showMetaTraits : undefined,
        et: (showEmptyTrait) ? showEmptyTrait : undefined,
    }, {arrayFormat: 'comma'});
  }

function parseURLQuery(search) {
    const p = queryString.parse(search, {arrayFormat: 'comma'});
  
    const rt = p.rt ?? [];
    const dt = p.dt ?? [];

    return {
      searchString: p.searchString ?? "",
      requiredTraits: [].concat(rt),
      disallowedTraits: [].concat(dt),
      showMetaTraits: p.mt === 'true',
      showEmptyTrait: p.et === 'true',
    };
  }

const styles = {
    container: {
        display: "flex",
        flexDirection: "row",
        height: "100%",
    },
    // left column - search
    searchColumn: {
        display: "flex",
        flexDirection: "column",
        padding: "10px",
        overflowY: "scroll",
        width: "30%",
        minWidth: "300px",
    },
    searchColumnEntry: {
        marginBottom: "20px",
    },
    searchField: {
        width: "100%",
    },
    // right column - results
    resultsColumn: {
        display: "flex",
        flexDirection: "column",
        margin: "10px",
        flex: "1 1 auto",
    },
};
