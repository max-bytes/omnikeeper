import { Select, Spin } from 'antd';
import debounce from 'lodash/debounce';
import React from 'react';

function DebounceSelect({ fetchOptions, onChange, debounceTimeout = 800, ...props }) {
  const [fetching, setFetching] = React.useState(false);
  const [options, setOptions] = React.useState([]);
  const fetchRef = React.useRef(0);
  const debounceFetcher = React.useMemo(() => {
    const loadOptions = (value) => {
      fetchRef.current += 1;
      const fetchId = fetchRef.current;
      setOptions([]);
      setFetching(true);
      fetchOptions(value).then((newOptions) => {
        if (fetchId !== fetchRef.current) {
          // for fetch callback order
          return;
        }

        setOptions(newOptions);
        setFetching(false);
      });
    };

    return debounce(loadOptions, debounceTimeout);
  }, [fetchOptions, debounceTimeout]);
  return (
    <Select
      filterOption={false}
      onSearch={debounceFetcher}
      showSearch
      onChange={onChange}
      notFoundContent={fetching ? <Spin size="small" /> : "Nothing found"}
      {...props}
      options={options}
    />
  );
} // Usage of DebounceSelect

export default DebounceSelect;