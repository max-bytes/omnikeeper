import React, {useEffect} from 'react';
import { queries } from 'graphql/queries'
import { useQuery } from '@apollo/client';
import Layers from 'components/Layers';
import { format2ShortGuid } from 'utils/shortGuid';
import { mergeSettingsAndSortLayers } from 'utils/layers'; 
import { Form, Radio, Select } from "antd";
import MultiCISelect from 'components/MultiCISelect';
const { Option } = Select;

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
  return <Select 
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
        <div style={{display: 'flex', flexBasis: '300px'}}>
          <ChangesetDropdown layers={layers} ciids={ciids} timeSettings={timeSettings} setTimeSettings={setTimeSettings} />
        </div>}
    </div>
  );
}

function DiffCISettingsSpecificCIs(props) {

  return <Form.Item style={{flexGrow: 1, marginBottom: '0px'}}>
    <MultiCISelect layers={props.layers} selectedCIIDs={props.selectedCIIDs} setSelectedCIIDs={props.setSelectedCIIDs} />
  </Form.Item>;
  // const { data, loading } = useQuery(queries.CIList, {
  //   variables: { layers: props.layers }
  // });

  // var ciList = [];
  // if (data)
  //   ciList = data.compactCIs.map(d => {
  //     return <Option key={d.id} value={d.id}>{`${d.name ?? '[UNNAMED]'} - ${format2ShortGuid(d.id)}`}</Option>;
  //   });
    
  // return <Select
  //   mode="multiple"
  //   disabled={loading}
  //   allowClear
  //   filterOption={(input, option) => {
  //     return option.children.toLowerCase().indexOf(input.toLowerCase()) >= 0;
  //   }}
  //   style={{ width: '100%' }}
  //   placeholder="Select CIs..."
  //   value={props.selectedCIIDs ?? []}
  //   onChange={(value) => { props.setSelectedCIIDs(value); }}
  // >{ciList}
  // </Select>;
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