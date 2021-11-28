import React from 'react';
import AutoSizer from "react-virtualized-auto-sizer";
import { Virtuoso } from 'react-virtuoso';

export default function AutoSizedList(props) {
  const {item, itemCount} = props;

  const list = ({ height, width }) => (
    <Virtuoso
    style={{ height: height, width: width }}
    totalCount={itemCount}
    itemContent={index => item(index)}
    />
  );

  return <AutoSizer>
    {list}
  </AutoSizer>;
};