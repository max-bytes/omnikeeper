import React, {useEffect, useState} from 'react';
import { queries } from 'graphql/queries'
import { useQuery } from '@apollo/client';
import Layers from 'components/Layers';
import { mergeSettingsAndSortLayers } from 'utils/layers'; 
import { Form, Radio, Select } from "antd";
import MultiCISelect from 'components/MultiCISelect';
import moment from 'moment';

function ChangesetDropdown(props) {
  const { ciids, layers, timeSettings, setTimeSettings } = props;

  const [timerange] = useState({ from: moment().subtract(5, 'years').format(), to: moment().format() });

  // TODO: we should update the timerange and requery on layer- and ci-changes

  const { loading, data } = useQuery(queries.ChangesetsForCI, {
    variables: { ...timerange, ciids: ciids, layers: layers }
  });

  let list = [];
  if (data) {

    list = data.changesets.map(d => {
      return { key: d.id, value: d.timestamp, text: d.timestamp };
    }).sort((a, b) => {
      return b.text.localeCompare(a.text);
    });
  }
  return <Select style={{flexGrow: '1'}}
    loading={loading}
    disabled={loading}
    value={timeSettings?.timeThreshold}
    placeholder='Select Changeset...'
    onChange={(value) => setTimeSettings(ts => ({...ts, timeThreshold: value}))}
    showSearch
    options={list}
  />;
}

export function DiffTimeSettings(props) {
  
  const { ciids, layers, alignment, timeSettings, setTimeSettings } = props;

  const type = timeSettings?.type ?? 0;

  return (
    <div style={alignmentStyle(props.alignment)}>
      <div style={{display: 'flex', alignItems: 'center'}}>
        <Radio.Group onChange={(e) => setTimeSettings((ts) => e.target.value === 0 ? { ...ts, type: 0, timeThreshold: undefined } : { ...ts, type: 1 })} defaultValue={type}>
          <Radio id={`time-range-select-latest-${alignment}`} value={0} checked={type === 0}>Now / Latest</Radio>
          <Radio id={`time-range-select-changeset-${alignment}`} value={1} checked={type === 1} disabled={ciids && ciids.length === 0}>Changeset</Radio>
        </Radio.Group>
      </div>
      {type === 1 && 
        <div style={{display: 'flex', marginTop: '10px'}}>
          <ChangesetDropdown layers={layers} ciids={ciids} timeSettings={timeSettings} setTimeSettings={setTimeSettings} />
        </div>}
    </div>
  );
}

function DiffCISettingsSpecificCIs(props) {

  return <Form.Item style={{flexGrow: 1, marginBottom: '0px'}}>
    <MultiCISelect layers={props.layers} selectedCIIDs={props.selectedCIIDs} setSelectedCIIDs={props.setSelectedCIIDs} />
  </Form.Item>;
}

export function DiffCISettings(props) {
    
  const { layers, alignment, selectedCIIDs, setSelectedCIIDs } = props;

  const type = (selectedCIIDs === null) ? 0 : 1;

  return <div style={alignmentStyle(alignment)}>
    <div style={{display: 'flex', alignItems: 'center'}}>
      <Radio.Group onChange={(e) => setSelectedCIIDs((ts) => e.target.value === 0 ? null : [])} defaultValue={type}>
        <Radio id={`ci-select-all-${alignment}`} value={0} checked={type === 0}>All</Radio>
        <Radio id={`ci-select-specific-${alignment}`} value={1} checked={type === 1}>Specific</Radio>
      </Radio.Group>
    </div>
    {type === 1 && 
      <div style={{display: 'flex', marginTop: '10px'}}>
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
      return {display: 'flex', marginLeft: '20px', flexDirection: 'column'};
    case 'right':
      return {display: 'flex', marginRight: '20px', flexDirection: 'column'};
    default:
      return {display: 'flex', flexDirection: 'column'};
  };
}