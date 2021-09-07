"use strict";

function _typeof(obj) { "@babel/helpers - typeof"; if (typeof Symbol === "function" && typeof Symbol.iterator === "symbol") { _typeof = function _typeof(obj) { return typeof obj; }; } else { _typeof = function _typeof(obj) { return obj && typeof Symbol === "function" && obj.constructor === Symbol && obj !== Symbol.prototype ? "symbol" : typeof obj; }; } return _typeof(obj); }

Object.defineProperty(exports, "__esModule", {
  value: true
});
exports.Explorer = Explorer;
exports["default"] = void 0;

var _react = _interopRequireWildcard(require("react"));

var _agGridReact = require("ag-grid-react");

require("ag-grid-community/dist/styles/ag-grid.css");

require("ag-grid-community/dist/styles/ag-theme-balham.css");

var _antd = require("antd");

var _FeedbackMsg = _interopRequireDefault(require("components/FeedbackMsg.js"));

var _EditRemoveButtonCellRenderer = _interopRequireDefault(require("./EditRemoveButtonCellRenderer.js"));

var _reactRouterDom = require("react-router-dom");

function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { "default": obj }; }

function _getRequireWildcardCache() { if (typeof WeakMap !== "function") return null; var cache = new WeakMap(); _getRequireWildcardCache = function _getRequireWildcardCache() { return cache; }; return cache; }

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } if (obj === null || _typeof(obj) !== "object" && typeof obj !== "function") { return { "default": obj }; } var cache = _getRequireWildcardCache(); if (cache && cache.has(obj)) { return cache.get(obj); } var newObj = {}; var hasPropertyDescriptor = Object.defineProperty && Object.getOwnPropertyDescriptor; for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) { var desc = hasPropertyDescriptor ? Object.getOwnPropertyDescriptor(obj, key) : null; if (desc && (desc.get || desc.set)) { Object.defineProperty(newObj, key, desc); } else { newObj[key] = obj[key]; } } } newObj["default"] = obj; if (cache) { cache.set(obj, newObj); } return newObj; }

function asyncGeneratorStep(gen, resolve, reject, _next, _throw, key, arg) { try { var info = gen[key](arg); var value = info.value; } catch (error) { reject(error); return; } if (info.done) { resolve(value); } else { Promise.resolve(value).then(_next, _throw); } }

function _asyncToGenerator(fn) { return function () { var self = this, args = arguments; return new Promise(function (resolve, reject) { var gen = fn.apply(self, args); function _next(value) { asyncGeneratorStep(gen, resolve, reject, _next, _throw, "next", value); } function _throw(err) { asyncGeneratorStep(gen, resolve, reject, _next, _throw, "throw", err); } _next(undefined); }); }; }

function _slicedToArray(arr, i) { return _arrayWithHoles(arr) || _iterableToArrayLimit(arr, i) || _unsupportedIterableToArray(arr, i) || _nonIterableRest(); }

function _nonIterableRest() { throw new TypeError("Invalid attempt to destructure non-iterable instance.\nIn order to be iterable, non-array objects must have a [Symbol.iterator]() method."); }

function _unsupportedIterableToArray(o, minLen) { if (!o) return; if (typeof o === "string") return _arrayLikeToArray(o, minLen); var n = Object.prototype.toString.call(o).slice(8, -1); if (n === "Object" && o.constructor) n = o.constructor.name; if (n === "Map" || n === "Set") return Array.from(o); if (n === "Arguments" || /^(?:Ui|I)nt(?:8|16|32)(?:Clamped)?Array$/.test(n)) return _arrayLikeToArray(o, minLen); }

function _arrayLikeToArray(arr, len) { if (len == null || len > arr.length) len = arr.length; for (var i = 0, arr2 = new Array(len); i < len; i++) { arr2[i] = arr[i]; } return arr2; }

function _iterableToArrayLimit(arr, i) { if (typeof Symbol === "undefined" || !(Symbol.iterator in Object(arr))) return; var _arr = []; var _n = true; var _d = false; var _e = undefined; try { for (var _i = arr[Symbol.iterator](), _s; !(_n = (_s = _i.next()).done); _n = true) { _arr.push(_s.value); if (i && _arr.length === i) break; } } catch (err) { _d = true; _e = err; } finally { try { if (!_n && _i["return"] != null) _i["return"](); } finally { if (_d) throw _e; } } return _arr; }

function _arrayWithHoles(arr) { if (Array.isArray(arr)) return arr; }

var AgGridCopyCutPaste = _agGridReact.AgGridReact; // const AgGridCopyCutPaste = AgGridCopyCutPasteHOC(
//     AgGridReact, // React-AgGrid component
//     { className: "ag-theme-balham" }, // hocProps
//     true // logging
// );  

var Header = _antd.Layout.Header,
    Content = _antd.Layout.Content;

function Explorer(props) {
  var swaggerClient = props.swaggerClient;

  var _useState = (0, _react.useState)(null),
      _useState2 = _slicedToArray(_useState, 2),
      gridApi = _useState2[0],
      setGridApi = _useState2[1];

  var _useState3 = (0, _react.useState)(null),
      _useState4 = _slicedToArray(_useState3, 2),
      gridColumnApi = _useState4[0],
      setGridColumnApi = _useState4[1];

  var _useState5 = (0, _react.useState)(null),
      _useState6 = _slicedToArray(_useState5, 2),
      rowData = _useState6[0],
      setRowData = _useState6[1];

  var defaultColDef = initDefaultColDef(); // Init defaultColDef

  var _useState7 = (0, _react.useState)(""),
      _useState8 = _slicedToArray(_useState7, 2),
      swaggerMsg = _useState8[0],
      setSwaggerMsg = _useState8[1];

  var _useState9 = (0, _react.useState)(false),
      _useState10 = _slicedToArray(_useState9, 2),
      swaggerErrorJson = _useState10[0],
      setSwaggerErrorJson = _useState10[1];

  var columnDefs = [{
    headerName: "ID",
    field: "id",
    width: 1000
  }, {
    headerName: "",
    field: "edit",
    // set width = minWidth = maxWith, so fitting is suppressed in every possible way
    width: 84,
    minWidth: 84,
    maxWidth: 84,
    resizable: false,
    pinned: "right",
    // pinn to the right
    suppressSizeToFit: true,
    // suppress sizeToFit
    sortable: false,
    filter: false,
    cellRenderer: "editRemoveButtonCellRenderer",
    cellRendererParams: {
      operation: "edit",
      history: props.history
    }
  }, {
    headerName: "",
    field: "remove",
    // set width = minWidth = maxWith, so fitting is suppressed in every possible way
    width: 104,
    minWidth: 104,
    maxWidth: 104,
    resizable: false,
    pinned: "right",
    // pinn to the right
    suppressSizeToFit: true,
    // suppress sizeToFit
    sortable: false,
    filter: false,
    cellRenderer: "editRemoveButtonCellRenderer",
    cellRendererParams: {
      operation: "remove",
      removeContext: removeContext
    }
  }];
  return /*#__PURE__*/_react["default"].createElement(_antd.Layout, {
    style: {
      height: "100%",
      maxHeight: "100%",
      width: "100%",
      maxWidth: "100%",
      padding: "10px",
      backgroundColor: "white"
    }
  }, /*#__PURE__*/_react["default"].createElement(Header, {
    style: {
      paddingLeft: "0px",
      background: "none",
      height: "auto",
      padding: "unset"
    }
  }, swaggerMsg && /*#__PURE__*/_react["default"].createElement(_FeedbackMsg["default"], {
    alertProps: {
      message: swaggerMsg,
      type: swaggerErrorJson ? "error" : "success",
      showIcon: true,
      banner: true
    },
    swaggerErrorJson: swaggerErrorJson
  }), /*#__PURE__*/_react["default"].createElement("div", {
    style: {
      display: "flex",
      justifyContent: "flex-end",
      marginTop: "10px",
      marginBottom: "10px"
    }
  }, /*#__PURE__*/_react["default"].createElement("div", {
    style: {
      display: "flex"
    }
  }, /*#__PURE__*/_react["default"].createElement(_antd.Button, {
    style: {
      marginRight: "10px"
    },
    onClick: autoSizeAll
  }, "Fit")), /*#__PURE__*/_react["default"].createElement("div", {
    style: {
      display: "flex"
    }
  }, /*#__PURE__*/_react["default"].createElement(_antd.Button, {
    onClick: function onClick() {
      return refreshData();
    }
  }, "Refresh")))), /*#__PURE__*/_react["default"].createElement(Content, null, /*#__PURE__*/_react["default"].createElement(AgGridCopyCutPaste, {
    stopEditingWhenGridLosesFocus: true,
    onGridReady: onGridReady,
    rowData: rowData,
    columnDefs: columnDefs,
    defaultColDef: defaultColDef,
    frameworkComponents: {
      editRemoveButtonCellRenderer: _EditRemoveButtonCellRenderer["default"]
    },
    animateRows: true,
    rowSelection: "multiple",
    getRowNodeId: function getRowNodeId(data) {
      return data.id;
    },
    overlayLoadingTemplate: '<span class="ag-overlay-loading-center">Loading...</span>',
    overlayNoRowsTemplate: '<span class="ag-overlay-loading-center">No data.</span>'
  }))); // ######################################## INIT FUNCTIONS ########################################
  // grid ready

  function onGridReady(params) {
    setGridApi(params.api);
    setGridColumnApi(params.columnApi);
    refreshData();
  } // Init defaultColDef


  function initDefaultColDef() {
    return {
      sortable: true,
      filter: true,
      editable: false,
      resizable: true,
      width: 500,
      cellStyle: {
        fontStyle: "italic"
      }
    };
  } // ######################################## CRUD OPERATIONS ########################################
  // READ / refresh context


  function refreshData() {
    return _refreshData.apply(this, arguments);
  }

  function _refreshData() {
    _refreshData = _asyncToGenerator( /*#__PURE__*/regeneratorRuntime.mark(function _callee() {
      var contexts;
      return regeneratorRuntime.wrap(function _callee$(_context) {
        while (1) {
          switch (_context.prev = _context.next) {
            case 0:
              // important to re-create FeedbackMsg, after it has been closed!
              setSwaggerMsg("");
              setSwaggerErrorJson(""); // Tell AgGrid to reset rowData // important!

              if (gridApi) {
                gridApi.setRowData(null);
                gridApi.showLoadingOverlay(); // trigger "Loading"-state (otherwise would be in "No Rows"-state instead)
              }

              _context.prev = 3;
              _context.next = 6;
              return swaggerClient.apis.OKPluginGenericJSONIngest.GetAllContexts({
                version: props.apiVersion
              }).then(function (result) {
                return result.body;
              });

            case 6:
              contexts = _context.sent;
              setRowData(contexts); // set rowData
              // Tell AgGrid to set rowData

              if (gridApi) {
                gridApi.setRowData(contexts);
              } // INFO: don't show message on basic load


              _context.next = 15;
              break;

            case 11:
              _context.prev = 11;
              _context.t0 = _context["catch"](3);
              setSwaggerErrorJson(JSON.stringify(_context.t0.response, null, 2));
              setSwaggerMsg(_context.t0.toString());

            case 15:
            case "end":
              return _context.stop();
          }
        }
      }, _callee, null, [[3, 11]]);
    }));
    return _refreshData.apply(this, arguments);
  }

  function removeContext(_x) {
    return _removeContext.apply(this, arguments);
  } // ######################################## AG GRID FORMATTING ########################################
  // resize table and fit to column sizes


  function _removeContext() {
    _removeContext = _asyncToGenerator( /*#__PURE__*/regeneratorRuntime.mark(function _callee2(contextID) {
      return regeneratorRuntime.wrap(function _callee2$(_context2) {
        while (1) {
          switch (_context2.prev = _context2.next) {
            case 0:
              _context2.prev = 0;
              _context2.next = 3;
              return swaggerClient.apis.OKPluginGenericJSONIngest.RemoveContext({
                version: props.apiVersion,
                id: contextID
              }).then(function (result) {
                return result.body;
              });

            case 3:
              setSwaggerErrorJson(false);
              setSwaggerMsg("'" + contextID + "' has been removed.");
              refreshData(); // reload

              _context2.next = 12;
              break;

            case 8:
              _context2.prev = 8;
              _context2.t0 = _context2["catch"](0);
              setSwaggerErrorJson(JSON.stringify(_context2.t0.response, null, 2));
              setSwaggerMsg(_context2.t0.toString());

            case 12:
            case "end":
              return _context2.stop();
          }
        }
      }, _callee2, null, [[0, 8]]);
    }));
    return _removeContext.apply(this, arguments);
  }

  function autoSizeAll() {
    gridColumnApi.autoSizeAllColumns();
  }
}

var _default = (0, _reactRouterDom.withRouter)(Explorer);

exports["default"] = _default;