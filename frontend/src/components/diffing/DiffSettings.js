import React, {useEffect} from 'react';
import { queries } from 'graphql/queries'
import { Dropdown } from 'semantic-ui-react'
import { useQuery } from '@apollo/react-hooks';
import Layers from 'components/Layers';
import { format2ShortGuid } from 'utils/shortGuid';
import { mergeSettingsAndSortLayers } from 'utils/layers'; 
import Form from 'react-bootstrap/Form'

function ChangesetDropdown(props) {
  const { ciids, layers, timeSettings, setTimeSettings } = props;

  var from = "2010-01-01 00:00:00"; // TODO?
  var to = "2022-01-01 00:00:00";
  const { loading, data } = useQuery(queries.Changesets, {
    variables: { from: from, to: to, ciids: ciids, layers: layers } // TODO: multiple ciids
  });

  let list = [];
  if (data) {

    list = data.changesets.map(d => {
      return { key: d.id, value: d.timestamp, text: d.timestamp };
    }).sort((a, b) => {
      return b.text.localeCompare(a.text);
    });
  }
  return <Dropdown loading={loading}
    disabled={loading}
    value={timeSettings?.timeThreshold}
    placeholder='Select Changeset...'
    onChange={(_, data) => setTimeSettings(ts => ({...ts, timeThreshold: data.value}))}
    fluid
    search
    selection
    options={list}
  />;
}


export function DiffTimeSettings(props) {
  
  const { ciids, layers, alignment, timeSettings, setTimeSettings } = props;

  const type = timeSettings?.type ?? 0;

  return (
    <div style={alignmentStyle(props.alignment)}>
        <div style={{display: 'flex'}}>
          <Form.Check style={{alignItems: 'center'}}
            checked={type===0}
            custom
            inline
            label="Now / Latest"
            type={'radio'}
            value={0}
            id={`time-range-select-latest-${alignment}`}
            onChange={() => setTimeSettings(ts => ({...ts, type: 0, timeThreshold: undefined}))}
          />
          <Form.Check style={{alignItems: 'center'}}
            checked={type===1}
            disabled={ciids && ciids.length === 0}
            custom
            inline
            label="Changeset"
            type={'radio'}
            value={1}
            id={`time-range-select-changeset-${alignment}`}
            onChange={() => setTimeSettings(ts => ({...ts, type: 1}))}
          />
      </div>
      {type === 1 && 
        <div style={{display: 'flex', flexBasis: '300px'}}>
          <ChangesetDropdown layers={layers} ciids={ciids} timeSettings={timeSettings} setTimeSettings={setTimeSettings} />
        </div>}
    </div>
  );
}

function DiffCISettingsSpecificCIs(props) {
  const { data, loading } = useQuery(queries.CIList, {
    variables: { layers: props.layers }
  });

  var ciList = [];
  if (data)
    ciList = data.compactCIs.map(d => {
      return { key: d.id, value: d.id, text: `${d.name ?? '[UNNAMED]'} - ${format2ShortGuid(d.id)}`, orderLast: !!d.name, sortName: d.name };
    }).sort((a, b) => {
      if (a.orderLast !== b.orderLast) return ((a.orderLast) ? -1 : 1);
      if (a.sortName && b.sortName) return a.sortName.localeCompare(b.sortName);
      else return a.key.localeCompare(b.key);
    }).map(({orderLast, sortName, ...rest}) => rest);

  return <Dropdown style={{flexBasis: '550px', flexGrow: 1}} loading={loading}
                  disabled={loading}
                  value={props.selectedCIIDs ?? []}
                  placeholder='Select CIs...'
                  onChange={(_, data) => { props.setSelectedCIIDs(data.value);}}
                  fluid
                  multiple
                  search
                  selection
                  options={ciList}
                />;
}

export function DiffCISettings(props) {
    
  const { layers, alignment, selectedCIIDs, setSelectedCIIDs } = props;

  const type = (selectedCIIDs === null) ? 0 : 1;

  return <div style={alignmentStyle(alignment)}>
    <div style={{display: 'flex'}}>
      <Form.Check style={{alignItems: 'center'}}
        checked={type===0}
        custom
        inline
        label="All"
        type={'radio'}
        value={0}
        id={`ci-select-all-${alignment}`}
        onChange={() => setSelectedCIIDs(ts => null)}
      />
      <Form.Check style={{alignItems: 'center'}}
        checked={type===1}
        custom
        inline
        label="Specific"
        type={'radio'}
        value={1}
        id={`ci-select-specific-${alignment}`}
        onChange={() => setSelectedCIIDs(ts => [])}
      />
    </div>
    {type === 1 && 
      <div style={{display: 'flex', flexBasis: '300px'}}>
      <DiffCISettingsSpecificCIs layers={layers} selectedCIIDs={selectedCIIDs} setSelectedCIIDs={setSelectedCIIDs} />
      </div>}
  </div>
}
  
export function DiffLayerSettings(props) {
  const { layerData, onLayersChange, layerSettings, setLayerSettings } = props;

  useEffect(() => {
      const layers = mergeSettingsAndSortLayers(layerData, layerSettings);
      onLayersChange(layers);
  }, [layerData, layerSettings, onLayersChange]);

  return (<div style={alignmentStyle(props.alignment)}>
      <Layers layerSettings={layerSettings}
        setLayerSettings={ setLayerSettings } />
    </div>
  );
}


function alignmentStyle(alignment) {
  switch (alignment) {
    case 'left':
      return {display: 'flex', justifyContent: 'flex-start', marginLeft: '20px', minHeight: '38px'};
    case 'right':
      return {display: 'flex', justifyContent: 'flex-end', marginRight: '20px', minHeight: '38px'};
    default:
      return {display: 'flex', justifyContent: 'center', minHeight: '38px'};
  };
}