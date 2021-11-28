import React, {useState, useEffect} from 'react';
import { DiffCISettings, DiffLayerSettings, DiffTimeSettings } from './DiffSettings';
import { DiffArea } from './DiffArea';
import { queries } from 'graphql/queries'
import { Fragments } from 'graphql/fragments';
import { useLocation } from 'react-router-dom'
import { Card, Divider } from "antd";
import { useQuery, useLazyQuery } from '@apollo/client';
import { Form, Row, Col, Button, Checkbox } from "antd";
import LoadingOverlay from 'react-loading-overlay'; // TODO: switch to antd spin
import queryString from 'query-string';
import { withRouter } from 'react-router-dom';
import gql from 'graphql-tag';
import _ from 'lodash';

function LeftLabel(props) {
  return (
  <div style={{display: 'flex', width: '80px', minHeight: '38px', alignItems: 'center', justifyContent: 'flex-end', fontWeight: 'bold'}}>
    {props.children}
  </div>);
}

function parseURLQuery(search) {
  const p = queryString.parse(search, {arrayFormat: 'comma'});

  let lls = null;
  try {
    lls = JSON.parse(p.leftLayerSettings);
  } catch {}
  let rls = null;
  try {
    rls = JSON.parse(p.rightLayerSettings);
  } catch {}
  
  let lts = null;
  try {
    lts = JSON.parse(atob(p.leftTimeSettings));
  } catch {}
  let rts = null;
  try {
    rts = JSON.parse(atob(p.rightTimeSettings));
  } catch {}

  let leftCIIDs = p.leftCIIDs;
  if (_.isString(leftCIIDs))
    leftCIIDs = [leftCIIDs];
  else if (_.isUndefined(leftCIIDs))
    leftCIIDs = [];

  let rightCIIDs = p.rightCIIDs;
  if (_.isString(rightCIIDs))
    rightCIIDs = [rightCIIDs];
  else if (_.isUndefined(rightCIIDs))
    rightCIIDs = [];

  return {
    leftLayerSettings: lls,
    rightLayerSettings: rls,
    leftCIIDs: leftCIIDs,
    rightCIIDs: rightCIIDs,
    leftTimeSettings: lts,
    rightTimeSettings: rts
  };
}

function stringifyURLQuery(leftLayerSettings, rightLayerSettings, leftCIIDs, rightCIIDs, leftTimeSettings, rightTimeSettings) {
  // NOTE: converting some parameters to base64 (btoa/atob) because they otherwise get modified when passing via URL ("+" gets transformed to " ")
  return queryString.stringify({
    leftLayerSettings: (leftLayerSettings) ? JSON.stringify(leftLayerSettings) : undefined,
    rightLayerSettings: (rightLayerSettings) ? JSON.stringify(rightLayerSettings) : undefined,
    leftCIIDs: leftCIIDs,
    rightCIIDs: rightCIIDs,
    leftTimeSettings: (leftTimeSettings) ? btoa(JSON.stringify(leftTimeSettings)) : undefined, 
    rightTimeSettings: (rightTimeSettings) ? btoa(JSON.stringify(rightTimeSettings)) : undefined
  }, {arrayFormat: 'comma'});
}

export function buildDiffingURLQueryBetweenChangesets(layerSettings, ciid, leftTimestamp, rightTimestamp) {
  return stringifyURLQuery(layerSettings, layerSettings, [ciid], [ciid], 
    (leftTimestamp) ? { type: 1, timeThreshold: leftTimestamp } : { type: 0 }, 
    (rightTimestamp) ? { type: 1, timeThreshold: rightTimestamp } : { type: 0 });
}

function Diffing(props) {
  let urlParams = parseURLQuery(useLocation().search);

  const { data: layerData } = useQuery(queries.Layers);

  var [ leftLayerSettings, setLeftLayerSettings ] = useState(urlParams.leftLayerSettings);
  var [ rightLayerSettings, setRightLayerSettings ] = useState(urlParams.rightLayerSettings);
  var [ leftLayers, setLeftLayers ] = useState([]);
  var [ rightLayers, setRightLayers ] = useState([]);
  
  var [ leftCIIDs, setLeftCIIDs ] = useState(urlParams.leftCIIDs);
  var [ rightCIIDs, setRightCIIDs ] = useState(urlParams.rightCIIDs);
  
  var [ leftTimeSettings, setLeftTimeSettings ] = useState(urlParams.leftTimeSettings);
  var [ rightTimeSettings, setRightTimeSettings ] = useState(urlParams.rightTimeSettings);
  
  var [ showEqual, setShowEqual ] = useState(true);
  var [ allowCrossCIDiffing, setAllowCrossCIDiffing ] = useState(true);

  // reset timesettings when ci changes TODO: this breaks url-based setting -> how to make this work?
  // useEffect(() => setLeftTimeSettings(null), [leftCIID]);
  // useEffect(() => setRightTimeSettings(null), [rightCIID]);

  const buildRelationComparisonGQLString = (outgoing) => {
    return `
    relationComparisons {
      predicateID
      ${(outgoing) ? "toCIID" : "fromCIID"}
      left {
        relation {
          id
          fromCIID
          toCIID
          ${(outgoing) ? "toCIName" : "fromCIName"}
          predicateID
          changesetID
        }
        layerStackIDs
        layerID
        layerStack {
            id
            description
            color
        }
      }
      right {
        relation {
          id
          fromCIID
          toCIID
          ${(outgoing) ? "toCIName" : "fromCIName"}
          predicateID
          changesetID
        }
        layerStackIDs
        layerID
        layerStack {
            id
            description
            color
        }
      }
      status
    }
    `
  }

  const [loadDiffResults, {data: dataDiffResults, loading: loadingDiffResults }] = useLazyQuery(gql`
    query($leftCIIDs: [Guid], $rightCIIDs: [Guid], 
      $leftLayers: [String]!, $rightLayers: [String]!, 
      $leftTimeThreshold: DateTimeOffset, $rightTimeThreshold: DateTimeOffset,
      $leftAttributes: [String], $rightAttributes: [String],
      $showEqual: Boolean!, $allowCrossCIDiffing: Boolean!) {
      ciDiffing(leftLayers: $leftLayers, rightLayers: $rightLayers, 
        leftAttributes: $leftAttributes, rightAttributes: $rightAttributes,
        leftCIIDs: $leftCIIDs, rightCIIDs: $rightCIIDs,
        leftTimeThreshold: $leftTimeThreshold, rightTimeThreshold: $rightTimeThreshold,
        showEqual: $showEqual, allowCrossCIDiffing: $allowCrossCIDiffing) {
        cis {
          leftCIID
          leftCIName
          rightCIID
          rightCIName
          attributeComparisons {
            name
            left {
              ...FullMergedAttribute
            }
            right{
              ...FullMergedAttribute
            }
            status
          }
        }
        outgoingRelations {
          leftCIID
          leftCIName
          rightCIID
          rightCIName
          ${buildRelationComparisonGQLString(true)}
        }
        incomingRelations {
          leftCIID
          leftCIName
          rightCIID
          rightCIName
          ${buildRelationComparisonGQLString(false)}
        }
        effectiveTraits {
          leftCIID
          leftCIName
          rightCIID
          rightCIName
          effectiveTraitComparisons {
            traitID
            leftHasTrait
            rightHasTrait
            status
          }
        }
      }
    }
  ${Fragments.mergedAttribute}
  ${Fragments.attribute}
  `);

  useEffect(() => {
    const search = stringifyURLQuery(leftLayerSettings, rightLayerSettings, leftCIIDs, rightCIIDs, leftTimeSettings, rightTimeSettings);
    props.history.push({search: `?${search}`});
  }, [leftLayerSettings, rightLayerSettings, leftCIIDs, rightCIIDs, leftTimeSettings, rightTimeSettings, props.history]);

  const visibleLeftLayerIDs = leftLayers.filter(l => l.visible).map(l => l.id);
  const visibleRightLayerIDs = rightLayers.filter(l => l.visible).map(l => l.id);

  function compare() {
      loadDiffResults({ 
        variables: {
          leftLayers: visibleLeftLayerIDs, leftTimeThreshold: leftTimeSettings?.timeThreshold, leftCIIDs: leftCIIDs,
          rightLayers: visibleRightLayerIDs, rightTimeThreshold: rightTimeSettings?.timeThreshold, rightCIIDs: rightCIIDs,
          showEqual: showEqual, allowCrossCIDiffing: allowCrossCIDiffing
        }
      })
  }

  if (layerData) {

    return (
      <div style={{ width: "100%", padding: "10px" }}>
        <Card style={{ "boxShadow": "0px 0px 5px 0px rgba(0,0,0,0.25)" }}>
          <Row>
            <Col span={4} style={{display: 'flex'}}>
              <LeftLabel>Layers:</LeftLabel>
            </Col>
            <Col span={8}>
              <DiffLayerSettings alignment='right' layerData={layerData.layers} layerSettings={leftLayerSettings} setLayerSettings={setLeftLayerSettings} onLayersChange={setLeftLayers} />
            </Col><Col span={8}>
              <DiffLayerSettings alignment='left' layerData={layerData.layers} layerSettings={rightLayerSettings} setLayerSettings={setRightLayerSettings} onLayersChange={setRightLayers} />
            </Col>
          </Row>
          <Divider />
          <Row>
            <Col span={4} style={{display: 'flex'}}>
              <LeftLabel>CIs:</LeftLabel>
            </Col>
            {/* TODO: does having two separate ci settings even make sense? is there any usecase for this? The only thing I can think
                of is comparing two (not more) different CIs with each other... but for this to work we need to change some things */}
            <Col span={8}>
              {visibleLeftLayerIDs.length > 0 && 
                <DiffCISettings alignment='right' layers={visibleLeftLayerIDs} selectedCIIDs={leftCIIDs} setSelectedCIIDs={setLeftCIIDs} />}
            </Col><Col span={8}>
              {visibleRightLayerIDs.length > 0 && 
                <DiffCISettings alignment='left' layers={visibleRightLayerIDs} selectedCIIDs={rightCIIDs} setSelectedCIIDs={setRightCIIDs} />}
            </Col>
          </Row>
          <Divider />
          <Row>
            <Col span={4} style={{display: 'flex'}}>
              <LeftLabel>Time:</LeftLabel>
            </Col>
            <Col span={8}>
              {visibleLeftLayerIDs.length > 0 && 
                <DiffTimeSettings alignment='right' layers={visibleLeftLayerIDs} ciids={leftCIIDs} timeSettings={leftTimeSettings} setTimeSettings={setLeftTimeSettings} />}
            </Col><Col span={8}>
              {visibleRightLayerIDs.length > 0 && 
                <DiffTimeSettings alignment='left' layers={visibleRightLayerIDs} ciids={rightCIIDs} timeSettings={rightTimeSettings} setTimeSettings={setRightTimeSettings} />}
            </Col>
          </Row>
          <Divider />
          <Row>
          <Col span={24}>
            <Form initialValues={{ checkboxShowEqual: true, checkboxAllowCrossCIDiffing: true }} style={{display: 'flex', justifyContent: 'center'}}>
              <Form.Item name="checkboxShowEqual" valuePropName="checked" style={{ display: "inline-block", verticalAlign: "baseline", marginBottom: 0 }}>
                <Checkbox checked={showEqual} onChange={d => setShowEqual(d.target.checked)}>Show Equal</Checkbox>
              </Form.Item>
              <Form.Item name="checkboxAllowCrossCIDiffing" valuePropName="checked" style={{ display: "inline-block", verticalAlign: "baseline", marginBottom: 0 }}>
                <Checkbox checked={allowCrossCIDiffing} onChange={d => setAllowCrossCIDiffing(d.target.checked)}>Allow Cross-CI Diffing (if applicable)</Checkbox>
              </Form.Item>
              <Button style={{ display: "inline-block", marginLeft: "8px" }} type="primary" size="large" onClick={() => compare()} disabled={(leftCIIDs && leftCIIDs.length === 0) || (rightCIIDs && rightCIIDs.length === 0)}>Compare</Button>
            </Form>
          </Col>
        </Row>
        </Card>
        <Row>
          <Col span={24}>
            <LoadingOverlay fadeSpeed={100} active={loadingDiffResults} spinner>
              <DiffArea diffResults={dataDiffResults?.ciDiffing} />
            </LoadingOverlay>
          </Col>
        </Row>
      </div>
    )
  } else return 'Loading';
}

export default withRouter(Diffing);
