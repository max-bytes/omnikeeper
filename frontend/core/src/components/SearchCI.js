// import { useQuery } from '@apollo/client';
// import React, { useState } from 'react';
// import { queries } from '../graphql/queries'
// import { Input } from 'semantic-ui-react'
// import { withRouter, Link } from 'react-router-dom'

// function SearchCI(props) {
//   const initialState = { results: [], searchString: '' }

//   const { loading, data: dataCIs, refetch: search } = useQuery(queries.SimpleSearchCIs, {
//     variables: {searchString: initialState.searchString },
//     fetchPolicy: 'network-only'
//   });
//   const [state, setState] = useState(initialState);

//   const handleSearchChange = (e, { value }) => {
//     setState({...state, searchString: value});
//     // TODO: cancel previous searches -> see: https://evilmartians.com/chronicles/aborting-queries-and-mutations-in-react-apollo
//     search({searchString: value });
//   };

//   return (
//     <div style={{display: 'flex', flexDirection: 'column', height: '100%'}}>
//       <div style={{display: 'flex', justifyContent: 'center', marginTop: '50px'}}>
//         <Input icon='search' placeholder='Search...' loading={loading} value={state.searchString} onChange={handleSearchChange} />
//       </div>
//       <div style={{flexGrow: 1, overflowY: 'auto', margin: '20px auto', minWidth: '50%'}}>
//         {dataCIs &&
//           dataCIs.simpleSearchCIs.map((result, index) => {
//             return (
//               <Link key={result.id} to={`/explorer/${result.id}`}>
//                 <div style={{display: 'flex', padding: '10px', backgroundColor: ((index % 2 === 0) ? '#eee' : '#fff')}}>
//                   <div style={{flexGrow: '2', fontWeight: 'bold', flexBasis: '0'}}>{result.name ?? '[UNNAMED]'}</div><div style={{flexGrow: '2', flexBasis: '0'}}>{result.id}</div>
//                 </div>
//               </Link>);
//           })
//         }
//       </div>
//     </div>
//   );
// }

// export default withRouter(SearchCI);