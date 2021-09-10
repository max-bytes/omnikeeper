"use strict";

function _typeof(obj) { "@babel/helpers - typeof"; if (typeof Symbol === "function" && typeof Symbol.iterator === "symbol") { _typeof = function _typeof(obj) { return typeof obj; }; } else { _typeof = function _typeof(obj) { return obj && typeof Symbol === "function" && obj.constructor === Symbol && obj !== Symbol.prototype ? "symbol" : typeof obj; }; } return _typeof(obj); }

Object.defineProperty(exports, "__esModule", {
  value: true
});
exports["default"] = void 0;

var _react = _interopRequireWildcard(require("react"));

var _antd = require("antd");

var _reactRouterDom = require("react-router-dom");

var _FeedbackMsg = _interopRequireDefault(require("components/FeedbackMsg.js"));

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

function AddNewContext(props) {
  var swaggerClient = props.swaggerClient;
  var apiVersion = props.apiVersion;
  var editMode = props.editMode;

  var _useParams = (0, _reactRouterDom.useParams)(),
      contextID = _useParams.contextID; // get contextID from path


  var _useState = (0, _react.useState)(""),
      _useState2 = _slicedToArray(_useState, 2),
      swaggerMsg = _useState2[0],
      setSwaggerMsg = _useState2[1];

  var _useState3 = (0, _react.useState)(false),
      _useState4 = _slicedToArray(_useState3, 2),
      swaggerErrorJson = _useState4[0],
      setSwaggerErrorJson = _useState4[1];

  var _useState5 = (0, _react.useState)(true),
      _useState6 = _slicedToArray(_useState5, 2),
      loading = _useState6[0],
      setLoading = _useState6[1];

  var _useState7 = (0, _react.useState)(null),
      _useState8 = _slicedToArray(_useState7, 2),
      context = _useState8[0],
      setContext = _useState8[1]; // get context


  var refresh = (0, _react.useCallback)( /*#__PURE__*/_asyncToGenerator( /*#__PURE__*/regeneratorRuntime.mark(function _callee() {
    var contextJson, initialNewContext;
    return regeneratorRuntime.wrap(function _callee$(_context) {
      while (1) {
        switch (_context.prev = _context.next) {
          case 0:
            _context.prev = 0;
            setLoading(true);

            if (!editMode) {
              _context.next = 9;
              break;
            }

            _context.next = 5;
            return swaggerClient.apis.OKPluginGenericJSONIngest.GetContext({
              version: apiVersion,
              id: contextID
            }).then(function (result) {
              return result.body;
            });

          case 5:
            contextJson = _context.sent;
            setContext(contextJson); // set context

            _context.next = 11;
            break;

          case 9:
            initialNewContext = {
              id: "",
              extractConfig: {
                $type: "OKPluginGenericJSONIngest.Extract.ExtractConfigPassiveRESTFiles, OKPluginGenericJSONIngest"
              },
              transformConfig: {
                $type: "OKPluginGenericJSONIngest.Transform.JMESPath.TransformConfigJMESPath, OKPluginGenericJSONIngest",
                expression: ""
              },
              loadConfig: {
                $type: "OKPluginGenericJSONIngest.Load.LoadConfig, OKPluginGenericJSONIngest",
                searchLayerIDs: [],
                writeLayerID: 0
              }
            };
            setContext(initialNewContext); // set context

          case 11:
            _context.next = 17;
            break;

          case 13:
            _context.prev = 13;
            _context.t0 = _context["catch"](0);
            setSwaggerErrorJson(JSON.stringify(_context.t0.response, null, 2));
            setSwaggerMsg(_context.t0.toString());

          case 17:
            _context.prev = 17;
            setLoading(false);
            return _context.finish(17);

          case 20:
          case "end":
            return _context.stop();
        }
      }
    }, _callee, null, [[0, 13, 17, 20]]);
  })), [swaggerClient, apiVersion, contextID, editMode]);
  (0, _react.useEffect)(function () {
    refresh();
  }, [refresh]);
  return /*#__PURE__*/_react["default"].createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'center',
      flexGrow: 1,
      margin: "10px"
    }
  }, context ? /*#__PURE__*/_react["default"].createElement(_antd.Form, {
    labelCol: {
      span: "8"
    },
    style: {
      display: 'flex',
      flexDirection: 'column',
      flexBasis: '1000px',
      margin: '10px 0px'
    },
    onFinish: /*#__PURE__*/function () {
      var _ref2 = _asyncToGenerator( /*#__PURE__*/regeneratorRuntime.mark(function _callee2(e) {
        var newContext, addContext;
        return regeneratorRuntime.wrap(function _callee2$(_context2) {
          while (1) {
            switch (_context2.prev = _context2.next) {
              case 0:
                newContext = context;
                if (!editMode) newContext.id = e.id;
                newContext.transformConfig.expression = e.expression ? e.expression : "";
                ;
                newContext.loadConfig.searchLayerIDs = e.searchLayerIDs.substring(1, e.searchLayerIDs.length - 1).split(","); // convert into array

                newContext.loadConfig.writeLayerID = e.writeLayerID;
                _context2.prev = 6;
                setLoading(true); // 'AddContext' will add a new context, if given context doesn't exist and edit the context, if it does

                _context2.next = 10;
                return swaggerClient.apis.OKPluginGenericJSONIngest.UpsertContext({
                  version: apiVersion
                }, {
                  requestBody: newContext
                }).then(function (result) {
                  return result.body;
                });

              case 10:
                addContext = _context2.sent;
                setSwaggerErrorJson(false);
                if (editMode) setSwaggerMsg("'" + contextID + "' has been changed.");else setSwaggerMsg("'" + addContext.id + "' has been created.");
                _context2.next = 19;
                break;

              case 15:
                _context2.prev = 15;
                _context2.t0 = _context2["catch"](6);
                setSwaggerErrorJson(JSON.stringify(_context2.t0.response, null, 2));
                setSwaggerMsg(_context2.t0.toString());

              case 19:
                _context2.prev = 19;
                setLoading(false);
                return _context2.finish(19);

              case 22:
              case "end":
                return _context2.stop();
            }
          }
        }, _callee2, null, [[6, 15, 19, 22]]);
      }));

      return function (_x) {
        return _ref2.apply(this, arguments);
      };
    }(),
    initialValues: {
      "expression": context.transformConfig.expression,
      // text
      "searchLayerIDs": "[" + context.loadConfig.searchLayerIDs.toString() + "]",
      // array (handled as text)
      "writeLayerID": context.loadConfig.writeLayerID // text

    }
  }, swaggerMsg && /*#__PURE__*/_react["default"].createElement(_FeedbackMsg["default"], {
    alertProps: {
      message: swaggerMsg,
      type: swaggerErrorJson ? "error" : "success",
      showIcon: true,
      banner: true
    },
    swaggerErrorJson: swaggerErrorJson
  }), /*#__PURE__*/_react["default"].createElement("h2", null, editMode ? "Edit" : "Add", " Context"), editMode && /*#__PURE__*/_react["default"].createElement("h4", null, contextID), !editMode && /*#__PURE__*/_react["default"].createElement(_antd.Form.Item, {
    name: "id",
    label: "id",
    style: {
      margin: "0 0 50px 0"
    }
  }, /*#__PURE__*/_react["default"].createElement(_antd.Input, null)), /*#__PURE__*/_react["default"].createElement(_antd.Form.Item, {
    label: "transformConfig",
    style: {
      margin: 0,
      fontStyle: "italic"
    }
  }), /*#__PURE__*/_react["default"].createElement(_antd.Form.Item, {
    name: "expression",
    label: "expression",
    tooltip: "JMESPath",
    style: {
      margin: "0 0 50px 0"
    }
  }, /*#__PURE__*/_react["default"].createElement(_antd.Input.TextArea, null)), /*#__PURE__*/_react["default"].createElement(_antd.Form.Item, {
    label: "loadConfig",
    style: {
      margin: 0,
      fontStyle: "italic"
    }
  }), /*#__PURE__*/_react["default"].createElement(_antd.Form.Item, {
    name: "searchLayerIDs",
    label: "searchLayerIDs",
    tooltip: "Array of layerIDs - e.g. '[layer1,layer2]'",
    rules: [{
      required: true,
      pattern: /\[[0-9a-z_,]*[0-9a-z_]\]/
    }]
  }, /*#__PURE__*/_react["default"].createElement(_antd.Input, null)), /*#__PURE__*/_react["default"].createElement(_antd.Form.Item, {
    name: "writeLayerID",
    label: "writeLayerID",
    rules: [{
      required: true
    }],
    style: {
      margin: "0 0 50px 0"
    }
  }, /*#__PURE__*/_react["default"].createElement(_antd.Input, null)), /*#__PURE__*/_react["default"].createElement("div", {
    style: {
      display: "flex",
      justifyContent: "center"
    }
  }, /*#__PURE__*/_react["default"].createElement(_antd.Button, {
    type: "primary",
    htmlType: "submit",
    disabled: loading,
    style: {
      width: "100%"
    }
  }, editMode ? "Change " : "Create New ", "Context"))) : "Loading...");
}

var _default = (0, _reactRouterDom.withRouter)(AddNewContext);

exports["default"] = _default;