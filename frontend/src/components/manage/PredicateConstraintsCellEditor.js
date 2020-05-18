import React, {useEffect, forwardRef, useImperativeHandle, useRef} from "react";

export default forwardRef((props, ref) => {

    console.log(props.value);

    const inputRef = useRef();
    useImperativeHandle(ref, () => {
        return {
            getValue: () => {
                return inputRef.current.value;
            }
        };
    });
    return <div>
        from: <input type="text" ref={inputRef} defaultValue={props.value.preferredTraitsFrom}/>
        <br />
        To: <input type="text" ref={inputRef} defaultValue={props.value.preferredTraitsTo}/>
    </div>;
})