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

function LeftLabel(props) {
  return (
  <div style={{display: 'flex', width: '220px', minHeight: '38px', alignItems: 'center', justifyContent: 'flex-end', fontWeight: 'bold'}}>
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

  return {
    leftLayerSettings: lls,
    rightLayerSettings: rls,
    leftCIID: p.leftCIID,
    rightCIID: p.rightCIID
  };
}

function stringifyURLQuery(leftLayerSettings, rightLayerSettings, leftCIID, rightCIID) {
  return queryString.stringify({
    leftLayerSettings: (leftLayerSettings) ? JSON.stringify(leftLayerSettings) : undefined,
    rightLayerSettings: (rightLayerSettings) ? JSON.stringify(rightLayerSettings) : undefined,
    leftCIID: leftCIID,
    rightCIID: rightCIID
  }, {arrayFormat: 'comma'});
}

function Diffing(props) {
  let urlParams = parseURLQuery(useLocation().search);

  const { data: layerData } = useQuery(queries.Layers);

  var [ leftLayerSettings, setLeftLayerSettings ] = useState(urlParams.leftLayerSettings);
  var [ rightLayerSettings, setRightLayerSettings ] = useState(urlParams.rightLayerSettings);
  var [ leftLayers, setLeftLayers ] = useState([]);
  var [ rightLayers, setRightLayers ] = useState([]);
  
  var [ leftCIID, setLeftCIID ] = useState(urlParams.leftCIID);
  var [ rightCIID, setRightCIID ] = useState(urlParams.rightCIID);
  
  var [ leftTimeThreshold, setLeftTimeThreshold ] = useState(null);
  var [ rightTimeThreshold, setRightTimeThreshold ] = useState(null);
  
  var [ showEqual, setShowEqual ] = useState(true);

  useEffect(() => {
    const search = stringifyURLQuery(leftLayerSettings, rightLayerSettings, leftCIID, rightCIID);
    props.history.push({search: `?${search}`});
  }, [leftLayerSettings, rightLayerSettings, leftCIID, rightCIID, props.history]);

  const visibleLeftLayerNames = leftLayers.filter(l => l.visible).map(l => l.name);
  const visibleRightLayerNames = rightLayers.filter(l => l.visible).map(l => l.name);

  const [loadLeftCI, { data: dataLeftCI, loading: loadingLeftCI }] = useLazyQuery(queries.FullCI, {
    variables: { includeRelated: 0 }
  });
  const [loadRightCI, { data: dataRightCI, loading: loadingRightCI }] = useLazyQuery(queries.FullCI, {
    variables: { includeRelated: 0 }
  });

  function compare() {
    if (leftCIID)
      loadLeftCI({ variables: {layers: visibleLeftLayerNames, timeThreshold: leftTimeThreshold, identity: leftCIID},
        fetchPolicy: 'cache-and-network' });
    if (rightCIID)
      loadRightCI({ variables: {layers: visibleRightLayerNames, timeThreshold: rightTimeThreshold, identity: rightCIID},
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
                <DiffCISettings alignment='right' layers={visibleLeftLayerNames} selectedCIID={leftCIID} setSelectedCIID={setLeftCIID} />}
            </Col><Col>
              {visibleRightLayerNames.length > 0 && 
                <DiffCISettings alignment='left' layers={visibleRightLayerNames} selectedCIID={rightCIID} setSelectedCIID={setRightCIID} />}
            </Col>
          </Row>
          <Divider />
          <Row>
            <Col xs={'auto'} style={{display: 'flex'}}>
              <LeftLabel>Time:</LeftLabel>
            </Col>
            <Col>
              {visibleLeftLayerNames.length > 0 && 
                <DiffTimeSettings alignment='right' layers={visibleLeftLayerNames} ciid={leftCIID} timeThreshold={leftTimeThreshold} setTimeThreshold={setLeftTimeThreshold} />}
            </Col><Col>
              {visibleRightLayerNames.length > 0 && 
                <DiffTimeSettings alignment='left' layers={visibleRightLayerNames} ciid={rightCIID} timeThreshold={rightTimeThreshold} setTimeThreshold={setRightTimeThreshold} />}
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
                  <Button size='lg' onClick={() => compare()} disabled={!leftCIID || !rightCIID}>Compare</Button>
                </Form.Group>
              </Form>
            </div>
          </Col>
        </Row>
        </Segment>
        <Row>
          <Col>
            <LoadingOverlay fadeSpeed={100} active={loadingLeftCI || loadingRightCI} spinner>
              <DiffArea showEqual={showEqual} leftCI={dataLeftCI?.ci} rightCI={dataRightCI?.ci} leftLayers={leftLayers} rightLayers={rightLayers} />
            </LoadingOverlay>
          </Col>
        </Row>
      </Container>
    </div>)
  } else return 'Loading';
}

export default withRouter(Diffing);
