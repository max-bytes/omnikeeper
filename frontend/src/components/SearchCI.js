import { useQuery } from '@apollo/client';
import React, { useState } from 'react';
import { queries } from '../graphql/queries'
import { Input } from 'semantic-ui-react'
import { withRouter, Link } from 'react-router-dom'

function SearchCI(props) {
  const initialState = { results: [], searchString: '', withEffectiveTraits: [] }

  // TODO: make withEffectiveTraits dynamic, settable via UI
  // see https://www.mhx.at/openproject/projects/landscape-registry/work_packages/419/activity

  const { loading, data: dataCIs, refetch: search } = useQuery(queries.SearchCIs, {variables: 
    {searchString: initialState.searchString, withEffectiveTraits: initialState.withEffectiveTraits }
  });
  const [state, setState] = useState(initialState);

  const handleSearchChange = (e, { value }) => {
    setState({...state, searchString: value});
    // TODO: cancel previous searches -> see: https://evilmartians.com/chronicles/aborting-queries-and-mutations-in-react-apollo
    search({searchString: value, withEffectiveTraits: state.withEffectiveTraits });
  };

  return (
    <div style={{display: 'flex', flexDirection: 'column', height: '100%'}}>
      <div style={{display: 'flex', justifyContent: 'center', marginTop: '50px'}}>
        <Input icon='search' placeholder='Search...' loading={loading} value={state.searchString} onChange={handleSearchChange} />
      </div>
      <div style={{flexGrow: 1, overflowY: 'auto', margin: '20px auto', minWidth: '50%'}}>
        {dataCIs &&
          dataCIs.searchCIs.map((result, index) => {
            return (
              <Link key={result.id} to={`/explorer/${result.id}`}>
                <div style={{display: 'flex', padding: '10px', backgroundColor: ((index % 2 === 0) ? '#eee' : '#fff')}}>
                  <div style={{flexGrow: '2', fontWeight: 'bold', flexBasis: '0'}}>{result.name ?? '[UNNAMED]'}</div><div style={{flexGrow: '2', flexBasis: '0'}}>{result.id}</div>
                </div>
              </Link>);
          })
        }
      </div>
    </div>
  );
}

export default withRouter(SearchCI);