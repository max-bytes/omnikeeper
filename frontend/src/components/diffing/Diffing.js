import React, {useState, useEffect} from 'react';
import { DiffCISettings, DiffLayerSettings, DiffTimeSettings } from './DiffSettings';
import { DiffArea } from './DiffArea';
import { queries } from 'graphql/queries'
import { useLocation } from 'react-router-dom'
import { Segment, Divider } from 'semantic-ui-react'
import { useQuery, useLazyQuery } from '@apollo/react-hooks';
import { Button, Container, Row, Col, Form } from 'react-bootstrap';
import LoadingOverlay from 'react-loading-overlay';
import queryString from 'query-string';
import { withRouter } from 'react-router-dom'
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
    lts = JSON.parse(p.leftTimeSettings);
  } catch {}
  let rts = null;
  try {
    rts = JSON.parse(p.rightTimeSettings);
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
  return queryString.stringify({
    leftLayerSettings: (leftLayerSettings) ? JSON.stringify(leftLayerSettings) : undefined,
    rightLayerSettings: (rightLayerSettings) ? JSON.stringify(rightLayerSettings) : undefined,
    leftCIIDs: leftCIIDs,
    rightCIIDs: rightCIIDs,
    leftTimeSettings: (leftTimeSettings) ? JSON.stringify(leftTimeSettings) : undefined, 
    rightTimeSettings: (rightTimeSettings) ? JSON.stringify(rightTimeSettings) : undefined
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

  const perPredicateLimit = 100;

  var [ leftLayerSettings, setLeftLayerSettings ] = useState(urlParams.leftLayerSettings);
  var [ rightLayerSettings, setRightLayerSettings ] = useState(urlParams.rightLayerSettings);
  var [ leftLayers, setLeftLayers ] = useState([]);
  var [ rightLayers, setRightLayers ] = useState([]);
  
  var [ leftCIIDs, setLeftCIIDs ] = useState(urlParams.leftCIIDs);
  var [ rightCIIDs, setRightCIIDs ] = useState(urlParams.rightCIIDs);
  
  var [ leftTimeSettings, setLeftTimeSettings ] = useState(urlParams.leftTimeSettings);
  var [ rightTimeSettings, setRightTimeSettings ] = useState(urlParams.rightTimeSettings);
  
  var [ showEqual, setShowEqual ] = useState(true);

  // reset timesettings when ci changes TODO: this breaks url-based setting -> how to make this work?
  // useEffect(() => setLeftTimeSettings(null), [leftCIID]);
  // useEffect(() => setRightTimeSettings(null), [rightCIID]);

  const [loadLeftCI, { data: dataLeftCI, loading: loadingLeftCI }] = useLazyQuery(queries.FullCIs, {
    variables: { includeRelated: perPredicateLimit }
  });
  const [loadRightCI, { data: dataRightCI, loading: loadingRightCI }] = useLazyQuery(queries.FullCIs, {
    variables: { includeRelated: perPredicateLimit }
  });

  useEffect(() => {
    const search = stringifyURLQuery(leftLayerSettings, rightLayerSettings, leftCIIDs, rightCIIDs, leftTimeSettings, rightTimeSettings);
    props.history.push({search: `?${search}`});
  }, [leftLayerSettings, rightLayerSettings, leftCIIDs, rightCIIDs, leftTimeSettings, rightTimeSettings, props.history]);

  const visibleLeftLayerNames = leftLayers.filter(l => l.visible).map(l => l.name);
  const visibleRightLayerNames = rightLayers.filter(l => l.visible).map(l => l.name);

  function compare() {
    // if (leftCIIDs)
      loadLeftCI({ variables: {layers: visibleLeftLayerNames, timeThreshold: leftTimeSettings?.timeThreshold, ciids: leftCIIDs},
        fetchPolicy: 'cache-and-network' });
    // if (rightCIIDs)
      loadRightCI({ variables: {layers: visibleRightLayerNames, timeThreshold: rightTimeSettings?.timeThreshold, ciids: rightCIIDs},
        fetchPolicy: 'cache-and-network' });
  }

  if (layerData) {

    return (<div style={{marginTop: '10px', marginBottom: '20px'}}>
      <Container fluid>
        <Segment>
          <Row>
            <Col xs={'auto'} style={{display: 'flex'}}>
              <LeftLabel>Layers:</LeftLabel>
            </Col>
            <Col>
              <DiffLayerSettings alignment='right' layerData={layerData.layers} layerSettings={leftLayerSettings} setLayerSettings={setLeftLayerSettings} onLayersChange={setLeftLayers} />
            </Col><Col>
              <DiffLayerSettings alignment='left' layerData={layerData.layers} layerSettings={rightLayerSettings} setLayerSettings={setRightLayerSettings} onLayersChange={setRightLayers} />
            </Col>
          </Row>
          <Divider />
          <Row>
            <Col xs={'auto'} style={{display: 'flex'}}>
              <LeftLabel>CIs:</LeftLabel>
            </Col>
            <Col>
              {visibleLeftLayerNames.length > 0 && 
                <DiffCISettings alignment='right' layers={visibleLeftLayerNames} selectedCIIDs={leftCIIDs} setSelectedCIIDs={setLeftCIIDs} />}
            </Col><Col>
              {visibleRightLayerNames.length > 0 && 
                <DiffCISettings alignment='left' layers={visibleRightLayerNames} selectedCIIDs={rightCIIDs} setSelectedCIIDs={setRightCIIDs} />}
            </Col>
          </Row>
          <Divider />
          <Row>
            <Col xs={'auto'} style={{display: 'flex'}}>
              <LeftLabel>Time:</LeftLabel>
            </Col>
            <Col>
              {visibleLeftLayerNames.length > 0 && 
                <DiffTimeSettings alignment='right' layers={visibleLeftLayerNames} ciids={leftCIIDs} timeSettings={leftTimeSettings} setTimeSettings={setLeftTimeSettings} />}
            </Col><Col>
              {visibleRightLayerNames.length > 0 && 
                <DiffTimeSettings alignment='left' layers={visibleRightLayerNames} ciids={rightCIIDs} timeSettings={rightTimeSettings} setTimeSettings={setRightTimeSettings} />}
            </Col>
          </Row>
          <Divider />
          <Row>
          <Col xs={'auto'}>
            <LeftLabel>&nbsp;</LeftLabel>
          </Col>
          <Col>
            <div style={{display: 'flex', justifyContent: 'center'}}>
              <Form inline>
                <Form.Group controlId="compare">
                  <Form.Check
                    custom
                    inline
                    label="Show Equal"
                    type={'checkbox'}
                    checked={showEqual}
                    id={`checkbox-show-equal`}
                    onChange={d => setShowEqual(d.target.checked) }
                  />
                  <Button size='lg' onClick={() => compare()} disabled={(leftCIIDs && leftCIIDs.length === 0) || (rightCIIDs && rightCIIDs.length === 0)}>Compare</Button>
                </Form.Group>
              </Form>
            </div>
          </Col>
        </Row>
        </Segment>
        <Row>
          <Col>
            <LoadingOverlay fadeSpeed={100} active={loadingLeftCI || loadingRightCI} spinner>
              <DiffArea showEqual={showEqual} leftCIs={dataLeftCI?.cis} rightCIs={dataRightCI?.cis} />
            </LoadingOverlay>
          </Col>
        </Row>
      </Container>
    </div>)
  } else return 'Loading';
}

export default withRouter(Diffing);
