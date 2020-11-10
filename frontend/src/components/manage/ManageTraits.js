import React, { useState, useEffect } from 'react';
import { Link  } from 'react-router-dom'
import { Icon, Button } from 'semantic-ui-react';
import Form from 'react-bootstrap/Form';
import { queries } from 'graphql/queries'
import { mutations } from 'graphql/mutations'
import 'ace-builds';
import { useQuery, useMutation, useApolloClient } from '@apollo/react-hooks';
import 'ace-builds/webpack-resolver';
import AceEditor from "react-ace";
import { ErrorPopupButton } from "../ErrorPopupButton";

import "ace-builds/src-noconflict/mode-json";

export default function ManageCITypes() {

  const { data, loading } = useQuery(queries.TraitSet, {fetchPolicy: 'network-only'});
  const [setTraitSet, { loading: setTraitSetLoading, error: setTraitSetError }] = useMutation(mutations.SET_TRAITSET);
  const apolloClient = useApolloClient();
  var [hasErrors, setHasErrors] = useState(false);
  const [config, setConfig] = useState("Loading");
  useEffect(() => {
    if (!!data) {
      var prettyStr = JSON.stringify(JSON.parse(data.traitSet),null,2);  
      setConfig(prettyStr);
    }
  }, [data]);

  return <div style={{ display: 'flex', flexDirection: 'column', padding: '10px', height: '100%' }}>
    <h2>Traits</h2>
    <div><Link to="/manage"><Icon name="angle left" fitted /> Back</Link></div>
    <Form style={{margin:'10px 0px'}} onSubmit={e => {
            e.preventDefault();
            setTraitSet({ variables: { traitSet: config } })
            .then(d => {
              var prettyStr = JSON.stringify(JSON.parse(d.data.setTraitSet),null,2);  
              setConfig(prettyStr);
            })
            .then(r => apolloClient.resetStore());
          }}>
      <AceEditor
              value={config}
              onValidate={a => {
                  const e = a.filter(a => a.type === 'error').length > 0;
                  setHasErrors(e);
              }}
              mode="json"
              theme="textmate"
              onChange={newValue => setConfig(newValue)}
              name="TraitSet Editor"
              width={'unset'}
              style={{marginBottom: '10px', flexGrow: 1, border: "1px solid #ced4da", borderRadius: ".25rem"}}
              setOptions={{ 
                  showPrintMargin: false
              }}
          />
      <Button primary type="submit" disabled={loading || hasErrors || setTraitSetLoading}>Save</Button>
      <ErrorPopupButton error={setTraitSetError} />
    </Form>
    
  </div>;
}
