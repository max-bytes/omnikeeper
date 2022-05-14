import React, { useEffect, useState, useRef, useCallback } from "react";
import { graphviz } from "d3-graphviz";
import _ from 'lodash';

export default function Graph(props) {
    const {graphDefinition} = props;
    
    useEffect(() => {
        // produces <script src="wasm/@hpcc-js/index.min.js" type="javascript/worker"></script>
        // TODO: prevent multiple appends
        const script = document.createElement("script");
        script.type = "javascript/worker";
        script.src = process.env.PUBLIC_URL + "/wasm/@hpcc-js/index.min.js";
        document.head.appendChild(script);
    }, []);

    const ref = useRef(null);

    // HACK: constantly update size of graph div to be able to resize graph itself
    const [size, setSize] = useState([0,0]);
    const updateSize = useCallback(() => {
        setSize(currentSize => {
            if (ref.current) {
                if (currentSize[0] != ref.current.clientWidth || currentSize[1] != ref.current.clientHeight)
                    return [ref.current.clientWidth, ref.current.clientHeight];
                else
                    return currentSize;
            }
        });
    }, [ref, setSize]);
    useEffect(() => {
        updateSize();
        window.addEventListener('resize', updateSize);
        return () => {
            window.removeEventListener('resize', updateSize);
        }
    }, [updateSize]);

    useEffect(()=>{
        graphviz(`#graph-body`)
            .tweenPaths(false) // disabled for performance
            .tweenShapes(false) // disabled for performance
            .width(Math.max(0, size[0] - 10))
            .height(Math.max(0, size[1] - 10))
            .renderDot(graphDefinition);
    }, [graphDefinition, size]);

    return <div id="graph-body" style={{height: '100%'}} ref={ref}></div>;
}