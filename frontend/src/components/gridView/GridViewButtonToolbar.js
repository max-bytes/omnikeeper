import React, { useState } from "react";
import { Button, Select, Popover } from "antd";
const { Option } = Select;

export default function GridViewButtonToolbar(props) {
    const [usedContext, setUsedContext] = useState(null);

    if (props.context)
        return (
            <div className="button-toolbar">
                <div
                    className="button-toolbar-row"
                    style={{
                        display: "flex",
                        justifyContent: "space-between",
                        marginTop: "10px",
                        marginBottom: "10px",
                    }}
                >
                    <div
                        className="contex-panel"
                        style={{
                            display: "flex",
                            width: "15%",
                        }}
                    >
                        <Select
                            showSearch={true}
                            defaultValue={null}
                            onSelect={(value) => {
                                props.applyContext(value);
                                setUsedContext(value);
                            }}
                            onClear={props.applyContext}
                            style={{ minWidth: "75%" }}
                            placeholder={"Please choose context."}
                        >
                            {/* TODO: change 'contexts' to 'configuredContexts', when changed in BE */}
                            {props.context.contexts
                                ? props.context.contexts.map(
                                      (configuredContext) => (
                                          <Option
                                              key={configuredContext.name}
                                              value={configuredContext.name}
                                          >
                                              <Popover
                                                  title={
                                                      configuredContext.speakingName
                                                  }
                                                  content={
                                                      configuredContext.description
                                                  }
                                                  placement={"right"}
                                              >
                                                  {
                                                      configuredContext.speakingName
                                                  }
                                              </Popover>
                                          </Option>
                                      )
                                  )
                                : ""}
                        </Select>
                    </div>
                    <div
                        style={{
                            display: "flex",
                        }}
                    >
                        {/* New rows: */}
                        {/* <Button type="text" style={{ cursor: "default" }}>
                        New rows:
                    </Button>
                    <Button value={1} onClick={props.newRows}>
                        1
                    </Button>
                    <Button value={10} onClick={props.newRows}>
                        10
                    </Button>
                    <Button value={50} onClick={props.newRows}>
                        50
                    </Button> */}

                        {/* Delete row */}
                        {/* <Button
                        style={{ marginLeft: "10px" }}
                        onClick={props.markRowAsDeleted}
                    >
                        Delete row
                    </Button> */}

                        {/* Reset row */}
                        <Button
                            // style={{ marginLeft: "10px" }}
                            onClick={props.resetRow}
                        >
                            Reset row
                        </Button>
                    </div>

                    <div
                        style={{
                            display: "flex",
                        }}
                    >
                        {/* Set cell to '[not set]' (= null/undefined) */}
                        <Button
                            style={{ marginLeft: "10px" }}
                            onClick={props.setCellToNotSet}
                        >
                            Set to '[not set]'
                        </Button>

                        {/* Set cell empty */}
                        <Button
                            onClick={props.setCellToEmpty}
                            style={{ marginLeft: "10px" }}
                        >
                            Set empty
                        </Button>
                    </div>

                    <div
                        style={{
                            display: "flex",
                        }}
                    >
                        {/* Fit */}
                        <Button
                            style={{ marginRight: "10px" }}
                            onClick={props.autoSizeAll}
                        >
                            Fit
                        </Button>

                        {/* Save */}
                        <Button onClick={() => props.save(usedContext)}>
                            Save
                        </Button>

                        {/* Refresh */}
                        <Button onClick={() => props.refreshData(usedContext)}>
                            Refresh
                        </Button>
                    </div>
                </div>
            </div>
        );
    else return <>Loading...</>;
}
