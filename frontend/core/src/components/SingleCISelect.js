import React from "react";
import { useQuery } from '@apollo/client';
import { queries } from '../graphql/queries'
import DebounceSelect from "./DebounceSelect";

function SingleCISelect(props) {

  const {layers, selectedCIID, setSelectedCIID } = props;

  // HACK: use useQuery+skip because useLazyQuery does not return a promise, see: https://github.com/apollographql/react-apollo/issues/3499
  const { refetch: searchCIs } = useQuery(queries.AdvancedSearchCIs, { skip: true });

  return <DebounceSelect
    placeholder="Search+select CI (min 3 characters)"
    value={selectedCIID}
    onChange={(value) => { setSelectedCIID(value); }}
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

        const ciList = data.advancedSearchCIs.map(d => {
          return { value: d.id, label: `${d.name ?? `[UNNAMED] - ${d.id}`}` };
        })
        return ciList;
      });
    }}
  />;
}

export default SingleCISelect;