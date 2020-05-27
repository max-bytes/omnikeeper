import React, {useState, useEffect} from 'react';
import { queries } from 'graphql/queries'
import { Dropdown } from 'semantic-ui-react'
import { useQuery } from '@apollo/react-hooks';
import Layers from 'components/Layers';
import { mergeSettingsAndSortLayers } from 'utils/layers'; 
import Form from 'react-bootstrap/Form'

export function DiffTimeSettings(props) {
  
  const { ciid, layers, alignment, setTimeThreshold } = props;

  const [selectedType, setSelectedType] = useState(0);
  const [selectedChangeset, setSelectedChangeset] = useState(undefined);

  // reset changeset when ci changes
  useEffect(() => setSelectedChangeset(undefined), [ciid]);

  // set resulting time threshold
  useEffect(() => {
    let resultingTimeThreshold = null;
    if (selectedType === 1 && selectedChangeset)
      resultingTimeThreshold = selectedChangeset; // TODO
    setTimeThreshold(resultingTimeThreshold);
  }, [selectedType, selectedChangeset, setTimeThreshold]);

  function ChangesetDropdown(props) {
    const { ciid, layers, selectedChangeset, setSelectedChangeset } = props;
    var from = "2010-01-01 00:00:00"; // TODO?
    var to = "2022-01-01 00:00:00";
    const { loading, data } = useQuery(queries.Changesets, {
      variables: { from: from, to: to, ciid: ciid, layers: layers }
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
      value={selectedChangeset}
      placeholder='Select Changeset...'
      onChange={(_, data) => setSelectedChangeset(data.value)}
      fluid
      search
      selection
      options={list}
    />;
  }

  return (
    <div style={alignmentStyle(props.alignment)}>
        <div style={{display: 'flex'}}>
          <Form.Check style={{alignItems: 'center'}}
            checked={selectedType===0}
            custom
            inline
            label="Now / Latest"
            type={'radio'}
            value={0}
            id={`time-range-select-latest-${alignment}`}
            onChange={() => setSelectedType(0)}
          />
          <Form.Check style={{alignItems: 'center'}}
            checked={selectedType===1}
            disabled={!!!ciid}
            custom
            inline
            label="Changeset"
            type={'radio'}
            value={1}
            id={`time-range-select-changeset-${alignment}`}
            onChange={() => setSelectedType(1)}
          />
      </div>
      {selectedType === 1 && 
        <div style={{display: 'flex', flexBasis: '300px'}}>
          <ChangesetDropdown layers={layers} ciid={ciid} selectedChangeset={selectedChangeset} setSelectedChangeset={setSelectedChangeset} />
        </div>}
    </div>
  );
}

export function DiffCISettings(props) {
    const { data, loading } = useQuery(queries.CIList, {
      variables: { layers: props.layers }
    });
  
    var ciList = [];
    if (data)
      ciList = data.compactCIs.map(d => {
        return { key: d.id, value: d.id, text: d.name ?? '[UNNAMED]', orderLast: !!d.name };
      }).sort((a, b) => {
        if (a.orderLast !== b.orderLast) return ((a.orderLast) ? -1 : 1);
        if (a.text && b.text) return a.text.localeCompare(b.text);
        else return a.key.localeCompare(b.key);
      }).map(({orderLast, ...rest}) => rest);
  
    return (<div style={alignmentStyle(props.alignment)}>
      <Dropdown style={{flexBasis: '400px'}} loading={loading}
                    disabled={loading}
                    value={props.selectedCIID}
                    placeholder='Select CI...'
                    onChange={(_, data) => props.setSelectedCIID(data.value)}
                    fluid
                    search
                    selection
                    options={ciList}
                  /></div>);
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
      return {display: 'flex', flexBasis: '400px', justifyContent: 'flex-start', marginLeft: '20px', minHeight: '38px'};
    case 'right':
      return {display: 'flex', flexBasis: '400px', justifyContent: 'flex-end', marginRight: '20px', minHeight: '38px'};
    default:
      return {display: 'flex', flexBasis: '400px', justifyContent: 'center', minHeight: '38px'};
  };
}