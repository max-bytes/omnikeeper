import React from "react";
import { Badge } from "antd";

export default function CountBadge(props) {
    const {children, count} = props;
    return <Badge count={count} size="small" offset={[0, -5]} style={{ backgroundColor: '#096dd9' }}>
      {children}
    </Badge>;
  }