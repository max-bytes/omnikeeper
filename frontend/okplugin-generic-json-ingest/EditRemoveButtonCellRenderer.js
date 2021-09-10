"use strict";

Object.defineProperty(exports, "__esModule", {
  value: true
});
exports["default"] = void 0;

var _react = _interopRequireDefault(require("react"));

var _antd = require("antd");

function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { "default": obj }; }

var _default = function _default(props) {
  if (props.operation === "edit") return /*#__PURE__*/_react["default"].createElement("span", null, /*#__PURE__*/_react["default"].createElement(_antd.Button, {
    size: "small",
    style: {
      width: "60px"
    },
    type: "primary",
    onClick: function onClick() {
      return props.history.push("edit-context/".concat(props.data.id));
    }
  }, "Edit"));
  if (props.operation === "remove") return /*#__PURE__*/_react["default"].createElement("span", null, /*#__PURE__*/_react["default"].createElement(_antd.Popconfirm, {
    title: "Are you sure to delete ".concat(props.data.id, "?"),
    onConfirm: function onConfirm() {
      return props.removeContext(props.data.id);
    },
    okText: "Yes",
    okButtonProps: {
      type: "danger"
    },
    cancelText: "No",
    cancelButtonProps: {
      size: "normal"
    },
    placement: "topRight"
  }, /*#__PURE__*/_react["default"].createElement(_antd.Button, {
    size: "small",
    htmlType: "submit",
    style: {
      width: "80px"
    },
    type: "danger"
  }, "Remove")));
};

exports["default"] = _default;