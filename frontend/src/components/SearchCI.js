import { useQuery, useLazyQuery } from '@apollo/client';
import React, { useState, useEffect } from 'react';
import { queries } from '../graphql/queries'
import { Search, Input } from 'semantic-ui-react'
import { withRouter, Link } from 'react-router-dom'
import PropTypes from 'prop-types'

function SearchCI(props) {
  const initialState = { results: [], value: '' }

  const [search, { loading, data: dataCIs }] = useLazyQuery(queries.SearchCIs);
  const [state, setState] = useState(initialState);
  useEffect(() => {

    if (dataCIs && state.value !== '') {
      setState(s => ({...s, results: dataCIs.searchCIs.map(d => {
        var nd = { id: d.id, title: d.name ?? '[UNNAMED]' };
        return nd;
      })}));
    } else {
      setState(s => ({...s, results: []}));
    }

}, [dataCIs, loading, state.value]);

  const handleSearchChange = (e, { value }) => {
    if (value.length < 1) return setState(initialState);
    setState({...state, value: value});

    search({variables: {searchString: value}});
  };

  return (
    <div style={{display: 'flex', flexDirection: 'column', height: '100%'}}>
      <div style={{display: 'flex', justifyContent: 'center', marginTop: '50px'}}>
        <Input icon='search' placeholder='Search...' loading={loading} value={state.value} onChange={handleSearchChange} />
      </div>
      <div style={{flexGrow: 1, overflowY: 'auto', margin: '20px auto', minWidth: '50%'}}>
        {state.results.map((result, index) => {
          return (
            <Link key={result.id} to={`/explorer/${result.id}`}>
              <div style={{display: 'flex', padding: '10px', backgroundColor: ((index % 2 === 0) ? '#eee' : '#fff')}}>
                <div style={{flexGrow: '2', fontWeight: 'bold', flexBasis: '0'}}>{result.title}</div><div style={{flexGrow: '2', flexBasis: '0'}}>{result.id}</div>
              </div>
            </Link>);
        })}
      </div>
    </div>
  );
}

export default withRouter(SearchCI);