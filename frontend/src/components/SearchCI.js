import { useQuery } from '@apollo/client';
import React, { useState } from 'react';
import { queries } from '../graphql/queries'
import { Search } from 'semantic-ui-react'
import { withRouter, Link } from 'react-router-dom'
import PropTypes from 'prop-types'


const resultRenderer = (d) => <Link to={`/explorer/${d.identity}`}>{d.identity}</Link>

resultRenderer.propTypes = {
  identity: PropTypes.string
}


function SearchCI(props) {
  const initialState = { isLoading: false, results: [], value: '' }

  // TODO: this doesn't do any lazy search loading, but loads the full ci list and filters in the frontend
  const { data: dataCIs } = useQuery(queries.CIList);
  const [state, setState] = useState(initialState);

  const handleResultSelect = (e, { result }) => setState({ ...state, value: result.title });

  const handleSearchChange = (e, { value }) => {
    // if (value.length < 1) return setState(initialState);

    RegExp.escape = function(s) {
        return s.replace(/[-/\\^$*+?.()|[\]{}]/g, '\\$&');
    };

    const re = new RegExp(RegExp.escape(value), 'i');
    const isMatch = (result) => re.test(result);

    setState({
      isLoading: false,
      value,
      results: dataCIs.ciids.filter(isMatch).map(d => {
        var nd = { identity: d, title: d };
        return nd;
      })
    });
  }

  if (!dataCIs)
    return "Loading";
  else
  return (
    <div style={{display: 'flex', flexDirection: 'column', height: '100%'}}>
      <div style={{display: 'flex', justifyContent: 'center', marginTop: '50px'}}>
        <Search size="huge" className={"CISearch"}
          minCharacters={0}
            loading={state.isLoading}
            onResultSelect={handleResultSelect}
            // onSearchChange={_.debounce(handleSearchChange, 500, {
            //   leading: true,
            // })}
            onSearchChange={handleSearchChange} // TODO: no debouncing
            results={state.results}
            value={state.value}
            resultRenderer={resultRenderer}
          />
      </div>
    </div>
  );
}

export default withRouter(SearchCI);