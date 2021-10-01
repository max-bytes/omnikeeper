import React from "react";
import { useQuery } from '@apollo/client';
import { queries } from '../graphql/queries'
import DebounceSelect from "./DebounceSelect";

function MultiCISelect(props) {

  const {layers, selectedCIIDs, setSelectedCIIDs } = props;

  // HACK: use useQuery+skip because useLazyQuery does not return a promise, see: https://github.com/apollographql/react-apollo/issues/3499
  const { refetch: searchCIs } = useQuery(queries.AdvancedSearchFullCIs, { skip: true });

  return <DebounceSelect
    mode="multiple"
    placeholder="Search+select CIs (min 3 characters)"
    value={selectedCIIDs}
    onChange={(value) => { setSelectedCIIDs(value); }}
    fetchOptions={async searchString => {

      if (searchString.length < 3) {
        return Promise.resolve([]);
      }

      return searchCIs({ 
        layers: layers, 
        searchString: searchString,
        withEffectiveTraits: [],
        withoutEffectiveTraits: [],
      }).then(({data, error}) => {
        if (!data) {
          console.log(error); // TODO
          return [];
        }

        const ciList = data.advancedSearchFullCIs.map(d => {
          return { value: d.id, label: `${d.name ?? `[UNNAMED] - ${d.id}`}` };
        })
        return ciList;
      });
    }}
  />;
}

export default MultiCISelect;
