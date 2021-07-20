import React, { useState } from "react";
import { useQuery } from '@apollo/client';
import { queries } from '../graphql/queries'
import { AutoComplete } from "antd";

function PredicateSelect(props) {

    const { data: predicates, loading } = useQuery(queries.PredicateList, {
      variables: { }
    });

    var filteredPredicates = [];
    if (predicates) {
        const sortedPredicates = [...predicates.predicates]
        sortedPredicates.sort((a,b) => a.id.localeCompare(b.id));
        filteredPredicates = sortedPredicates.map(d => { return {key: d.id, value: d.id, label: d.id}; });
    }

    return <InnerPredicateSelect loading={loading} allPredicatesList={filteredPredicates} {...props} />;
}

function InnerPredicateSelect(props) {
    const {allPredicatesList, predicateID, setPredicateID, loading} = props;

    const [predicateIDOptions, setPredicateIDOptions] = useState(allPredicatesList);

    return <AutoComplete
        loading={loading}
        disabled={loading}
        value={predicateID}
        placeholder='Search+select or set Predicate'
        style={{ width: "100%" }}
        onChange={(value) => {
            setPredicateID(value);
        }}
        options={predicateIDOptions}
        onSearch={(searchText) => {
            if (!searchText) 
                setPredicateIDOptions(allPredicatesList);
            else {
                var filtered = allPredicatesList.filter(p => p.value.includes(searchText));
                setPredicateIDOptions(filtered);
            }
        }}
    />;
}

export default PredicateSelect;
