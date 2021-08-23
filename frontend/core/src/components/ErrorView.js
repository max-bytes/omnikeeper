import React from 'react'

export function ErrorView({error, inPopup}) {

  var inner;
  if (error.hasOwnProperty('graphQLErrors')) {
    // error is a graphQL error
    inner = <>
        <h3>GraphQL errors</h3>
        <div style={{overflowY:'scroll', whiteSpace: 'pre-wrap'}}>
          {error.graphQLErrors.map((e, i) => {
            return <div key={`gql{i}`}>{e.message}</div>;
          })}
        </div>

        <h3>Network error</h3>
        <div>{error.networkError.message}</div>
        
        {error.networkError.result && <>
          <h3>Inner network errors</h3>
          <div style={{overflowY:'scroll', whiteSpace: 'pre-wrap'}}>
            {error.networkError.result.errors.map((e, i) => {
              return <div key={`nw{i}`}>{e.message}</div>;
            })}
          </div>
        </>}
    </>;
  } else {
    inner = <><h3>{error.name}</h3>
        <p style={{overflowY:'scroll'}}>{error.message}</p>
        <pre style={{overflowY:'scroll', whiteSpace: 'pre-wrap'}}>{error.stack}</pre></>;
  }

  return (
    <div style={(inPopup) ? {maxWidth: '700px', maxHeight: '400px', display: 'flex', flexDirection: 'column'} : {}}>
      {inner}
    </div>
  );
}
