import React from 'react';
import { FixedSizeList as List } from 'react-window';
import AutoSizer from "react-virtualized-auto-sizer";

export default function AutoSizedList(props) {
    const {item, itemCount, itemSize} = props;
    return <AutoSizer>
      {({ height, width }) => (
          <List
          height={height}
          itemCount={itemCount}
          itemSize={itemSize ?? 42}
          width={width}
          >
          {item}
          </List>
      )}
    </AutoSizer>;
};