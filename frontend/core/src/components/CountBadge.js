import React from "react";
import { Badge } from "antd";

export default function CountBadge(props) {
  const {children, count} = props;
  return <Badge count={count} size="small" overflowCount={9999} offset={[0, -5]} style={{ backgroundColor: '#4fa0f8' }}>
    {children}
  </Badge>;
}
