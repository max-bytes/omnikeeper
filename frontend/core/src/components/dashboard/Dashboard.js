import React, {useCallback} from "react";
import { Card, Col, Row, Space, Spin, Statistic } from "antd";
import { renderToStaticMarkup } from 'react-dom/server';
import { queries } from "graphql/queries";
import { useQuery } from "@apollo/client";

export default function Dashboard(props) {
    const { data, loading: loadingStatistics } = useQuery(queries.Statistics);

    const statistics = data?.statistics;
    const cis = statistics?.cis ?? 0;
    const layers = statistics?.layers ?? 0;
    const activeAttributes = statistics?.activeAttributes ?? 0;
    const activeRelations = statistics?.activeRelations ?? 0;
    const attributeChanges = statistics?.attributeChanges ?? 0;
    const relationChanges = statistics?.relationChanges ?? 0;
    const changesets = statistics?.changesets ?? 0;
    const traits = statistics?.traits ?? 0;
    const predicates = statistics?.predicates ?? 0;
    const generators = statistics?.generators ?? 0;

    const StatisticsCard = (props) => {
        const {number, name} = props;
        const Max = 1.0;
        const [time, setTime] = React.useState(0.0); // moving from 0 to Max
        useAnimationFrame(deltaTime => {
            setTime(prevTime => {
                if (prevTime < Max) {
                    return Math.min(Max, prevTime + deltaTime / 1000.0);
                }
                return Max;
            });
        })
        const f = time / Max;
        const startNumber = Math.max(number - 100, 0);
        const easedF = easeOutExpo(f);
        const currentNumber = startNumber + Math.round((number - startNumber) * easedF);
        return <Card style={createBackgroundStyle(number, easedF)}><Statistic title={name} value={currentNumber} /></Card>;
    }

    return <>
    <h1 style={{textAlign: 'center', paddingTop: '40px'}}>Welcome to omnikeeper!</h1>
        <Spin spinning={loadingStatistics}>
            <Space size={32} direction="vertical">
            <Row gutter={48} justify="center" align="top">
                <Col span={5}>
                    <StatisticsCard number={cis} name="CIs" />
                </Col>
                <Col span={5}>
                    <StatisticsCard number={activeAttributes} name="Active Attributes" />
                </Col>
                <Col span={5}>
                    <StatisticsCard number={activeRelations} name="Active Relations" />
                </Col>
            </Row>
            <Row gutter={48} justify="center" align="top">
                <Col span={5}>
                    <StatisticsCard number={changesets} name="Changesets" />
                </Col>
                <Col span={5}>
                    <StatisticsCard number={attributeChanges} name="Attribute Changes" />
                </Col>
                <Col span={5}>
                    <StatisticsCard number={relationChanges} name="Relation Changes" />
                </Col>
            </Row>
            <Row gutter={48} justify="center" align="top">
                <Col span={5}>
                    <StatisticsCard number={layers} name="Layers" />
                </Col>
                <Col span={5}>
                    <StatisticsCard number={traits} name="Traits" />
                </Col>
            </Row>
            <Row gutter={48} justify="center" align="top">
                <Col span={5}>
                    <StatisticsCard number={predicates} name="Predicates" />
                </Col>
                <Col span={5}>
                    <StatisticsCard number={generators} name="Generators" />
                </Col>
            </Row>
            </Space>
        </Spin>
    </>;
}

class SvgBackground extends React.Component {

    render() {

        const number = this.props.number;
        const animationTime = this.props.animationTime;

        const width = 100; // background is stretched anyway
        const height = 100; // background is stretched anyway

        function log(base, number) {
            return Math.log(number) / Math.log(base);
        }

        let logBase = 1;
        while(log(logBase, number) >= logBase) {
            logBase++;
        }
        const numRows = logBase;
        
        const bitGap = 3;

        const barHeight = (((height) / numRows) - bitGap);

        const yOffset = bitGap / 2;
        
        const numberInOtherBaseAsString = (logBase <= 1) ? (number).toString() : (number).toString(logBase);// weird, but works

        const bars = [];
        for(var y = 0;y < numRows;y++) {
            var digit = (numberInOtherBaseAsString)[numberInOtherBaseAsString.length - y - 1]; // weird, but works
            var digitInBase10 = (digit) ? ((logBase <= 1) ? parseInt(digit, 10) : parseInt(digit, logBase)) : 0;
            const barWidth = bitGap + digitInBase10 * (width / numRows) * animationTime;
            bars.push({x: width - barWidth, y: y * (barHeight + bitGap) + yOffset, width: barWidth});
        }

        return (
            <svg xmlns='http://www.w3.org/2000/svg' width={width} height={height}>
                {/* <rect width={width} height={height} fill='#269' /> */}
                <g opacity={0.3}>
                {bars.map(bar => {
                    return <rect key={`${bar.x}-${bar.y}`} width={bar.width} height={barHeight} x={bar.x} y={bar.y} fill="#1890ff" />;
                })}
                </g>
            </svg>
        );
    }
}

function createBackgroundStyle(number, animationTime) {
    const svgString = encodeURIComponent(renderToStaticMarkup(<SvgBackground number={number} animationTime={animationTime} />));
    const dataUri = `url("data:image/svg+xml,${svgString}")`;
    
    return {
        backgroundImage: dataUri,
        backgroundPosition: 'right',
        backgroundRepeat: 'no-repeat',
        backgroundSize: 'contain'
    }
}

function useAnimationFrame(callback) {
    // Use useRef for mutable variables that we want to persist
    // without triggering a re-render on their change
    const requestRef = React.useRef();
    const previousTimeRef = React.useRef();
    
    const animate = useCallback(time => {
      if (previousTimeRef.current !== undefined) {
        const deltaTime = time - previousTimeRef.current;
        callback(deltaTime)
      }
      previousTimeRef.current = time;
      requestRef.current = requestAnimationFrame(animate);
    }, [requestRef, previousTimeRef, callback]);
    
    React.useEffect(() => {
      requestRef.current = requestAnimationFrame(animate);
      return () => cancelAnimationFrame(requestRef.current);
    }, [animate]); // Make sure the effect runs only once
}

function easeOutExpo(x) {
    return x >= 1 ? 1 : 1 - Math.pow(2, -10 * x);
}
    