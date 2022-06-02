import React, {useEffect} from 'react'
import { Route  } from 'react-router-dom'

export default function Page(props) {
    const { title, ...rest } = props;

    useEffect(() => {
        if (title)
            document.title = `${title} | omnikeeper`;
        else
            document.title = "omnikeeper";
      return () => document.title = "omnikeeper";
    }, [title]);
  
    return <Route {...rest} />;
};