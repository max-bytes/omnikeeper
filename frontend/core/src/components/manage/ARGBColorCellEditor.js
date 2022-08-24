import React, {useState, forwardRef, useImperativeHandle} from "react";
import { SketchPicker } from 'react-color';

// TODO: refactor to own file, avoid duplication
function argbToRGB(color) {
    return '#'+ ('000000' + (color & 0xFFFFFF).toString(16)).slice(-6);
}
function rgbaObject2argbInt(rgbaObject) {
    return (rgbaObject.a << 24) + (rgbaObject.r << 16) + (rgbaObject.g << 8) + (rgbaObject.b << 0);
}

export default forwardRef((props, ref) => {

    var [color, setColor] = useState(argbToRGB(props.value ?? -1));

    useImperativeHandle(ref, () => {
        return {
            getValue: () => {
                if (color.rgb)
                    return rgbaObject2argbInt(color.rgb);
                else return props.value ?? -1;
            }
        };
    });

    return <div style={{display: 'flex'}}>
        <SketchPicker
            color={ color }
            onChange={ value => setColor(value) }
            onChangeComplete={ value => setColor(value) }
        />
    </div>;
})