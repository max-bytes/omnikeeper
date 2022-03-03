"use strict";

Object.defineProperty(exports, "__esModule", {
  value: true
});
exports["default"] = OKPluginGenericJSONIngest;
exports.description = exports.version = exports.title = exports.name = void 0;

var _react = _interopRequireDefault(require("react"));

var _antd = require("antd");

var _reactFontawesome = require("@fortawesome/react-fontawesome");

var _freeSolidSvgIcons = require("@fortawesome/free-solid-svg-icons");

require("antd/dist/antd.min.css");

var _PrivateRoute = require("components/PrivateRoute");

var _reactRouterDom = require("react-router-dom");

var _package = require("./package.json");

var _AddNewContext = _interopRequireDefault(require("./AddNewContext"));

var _Explorer = _interopRequireDefault(require("./Explorer"));

var _useSwaggerClient2 = _interopRequireDefault(require("utils/useSwaggerClient"));

function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { "default": obj }; }

var pluginTitle = "Generic JSON Ingest";
var apiVersion = 1;

function OKPluginGenericJSONIngest(props) {
  var _useRouteMatch = (0, _reactRouterDom.useRouteMatch)(),
      path = _useRouteMatch.path,
      url = _useRouteMatch.url;

  var _useLocation = (0, _reactRouterDom.useLocation)(),
      pathname = _useLocation.pathname;

  var _useSwaggerClient = (0, _useSwaggerClient2["default"])(),
      swaggerClient = _useSwaggerClient.data,
      loading = _useSwaggerClient.loading,
      error = _useSwaggerClient.error;

  if (error) return "Error:" + error;
  if (loading) return "Loading...";

  var ManageComponent = function ManageComponent() {
    return /*#__PURE__*/_react["default"].createElement("div", {
      style: {
        display: 'flex',
        flexDirection: 'column',
        height: '100%'
      }
    }, /*#__PURE__*/_react["default"].createElement(_reactRouterDom.Route, {
      render: function render(_ref) {
        var location = _ref.location;
        var locPath = location.pathname.split("/");
        var locPathLast = locPath[locPath.length - 1];
        var selectedKey = locPathLast === "create-context" ? "create-context" : "explorer";
        return /*#__PURE__*/_react["default"].createElement(_antd.Menu, {
          mode: "horizontal",
          selectedKeys: selectedKey,
          style: {
            display: 'flex',
            justifyContent: 'center',
            margin: "auto"
          }
        }, /*#__PURE__*/_react["default"].createElement(_antd.Menu.Item, {
          key: "explorer"
        }, /*#__PURE__*/_react["default"].createElement(_reactRouterDom.Link, {
          to: "".concat(url, "/").concat(_package.name, "/explorer")
        }, /*#__PURE__*/_react["default"].createElement(_reactFontawesome.FontAwesomeIcon, {
          icon: _freeSolidSvgIcons.faSearch,
          style: {
            marginRight: "10px"
          }
        }), "Contexts")), /*#__PURE__*/_react["default"].createElement(_antd.Menu.Item, {
          key: "create-context"
        }, /*#__PURE__*/_react["default"].createElement(_reactRouterDom.Link, {
          to: "".concat(url, "/").concat(_package.name, "/create-context")
        }, /*#__PURE__*/_react["default"].createElement(_reactFontawesome.FontAwesomeIcon, {
          icon: _freeSolidSvgIcons.faPlus,
          style: {
            marginRight: "10px"
          }
        }), "Create New Context")));
      }
    }), /*#__PURE__*/_react["default"].createElement(_reactRouterDom.Switch, null, /*#__PURE__*/_react["default"].createElement(_reactRouterDom.Redirect, {
      from: "/:url*(/+)",
      to: pathname.slice(0, -1)
    }), " ", /*#__PURE__*/_react["default"].createElement(_PrivateRoute.PrivateRoute, {
      path: "".concat(path, "/").concat(_package.name, "/edit-context/:contextID")
    }, /*#__PURE__*/_react["default"].createElement(_AddNewContext["default"], {
      swaggerClient: swaggerClient,
      apiVersion: apiVersion,
      editMode: true
    })), /*#__PURE__*/_react["default"].createElement(_PrivateRoute.PrivateRoute, {
      path: "".concat(path, "/").concat(_package.name, "/create-context")
    }, /*#__PURE__*/_react["default"].createElement(_AddNewContext["default"], {
      swaggerClient: swaggerClient,
      apiVersion: apiVersion
    })), /*#__PURE__*/_react["default"].createElement(_PrivateRoute.PrivateRoute, {
      path: "".concat(path, "/").concat(_package.name, "/explorer")
    }, /*#__PURE__*/_react["default"].createElement(_Explorer["default"], {
      swaggerClient: swaggerClient,
      apiVersion: apiVersion
    })), /*#__PURE__*/_react["default"].createElement(_PrivateRoute.PrivateRoute, {
      path: path
    }, /*#__PURE__*/_react["default"].createElement(_reactRouterDom.Redirect, {
      to: "".concat(path, "/").concat(_package.name, "/explorer")
    }))));
  };

  return {
    manageComponent: ManageComponent
  };
}

var name = _package.name;
exports.name = name;
var title = pluginTitle;
exports.title = title;
var version = _package.version;
exports.version = version;
var description = _package.description;
exports.description = description;