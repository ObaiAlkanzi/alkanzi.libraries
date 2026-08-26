/* ============================================================
   Demo reproduction of the ERP front-end shims the Procurement
   Workspace relies on: the listPopup + alkanziFormPopup shells,
   showAlert, and a couple of formatting helpers. Option defaults
   and wrapper classes mirror the real PublicFrame.js / helpers.js
   so the copied CSS renders identically.
   ============================================================ */
(function (w) {
    "use strict";

    function numberFormat(v, dec) {
        return DevExpress.localization.formatNumber(Number(v) || 0, { type: "fixedPoint", precision: dec == null ? 2 : dec });
    }

    // Grid status-cell colour classes (see pw-supplement.css / custom-class.css).
    w.cellColors = { customSuccessBtn: "custom-success-btn", warning: "custom-warning-btn", danger: "custom-danger-btn" };
    w.DevExpressNumberFormat = numberFormat;

    // Top-toolbar minimise + fullscreen buttons shared by both popup shells.
    function mgmtToolbarItems(getPopup) {
        return [
            {
                toolbar: "top", location: "after", widget: "dxButton",
                options: {
                    icon: "minus", elementAttr: { class: "management-toolbar-btn" }, hint: "Minimise",
                    onClick: function () { var p = getPopup(); if (p) p.hide(); }
                }
            },
            {
                toolbar: "top", location: "after", widget: "dxButton",
                options: {
                    icon: "fullscreen", elementAttr: { class: "management-toolbar-btn" }, hint: "Full screen",
                    onClick: function () { var p = getPopup(); if (p) p.option("fullScreen", !p.option("fullScreen")); }
                }
            }
        ];
    }

    /* -------- listPopup: a popup wrapping a dxList (kz-list-popup shell). -------- */
    function listPopup(ngName) {
        var self = this;
        self.data = [];
        self.listInit = null;
        self.popupInit = null;

        self.list = {
            keyExpr: "ID",
            height: "100%",
            scrollingEnabled: true,
            selectionMode: "none",
            activeStateEnabled: false,
            focusStateEnabled: false,
            hoverStateEnabled: true,
            searchEnabled: false,
            pageLoadMode: "scrollBottom",
            noDataText: "No records found.",
            dataSource: [],
            onInitialized: function (e) { self.listInit = e.component; }
        };

        self.popup = {
            showCloseButton: true,
            focusStateEnabled: true,
            hideOnOutsideClick: false,
            deferRendering: false,
            position: "center",
            dragOutsideBoundary: true,
            resizeEnabled: true,
            restorePosition: true,
            shading: false,
            shadingColor: "rgba(0,0,0,0.5)",
            showTitle: true,
            title: "Title",
            visible: false,
            wrapperAttr: { class: "kz-list-popup" },
            width: 560,
            height: "80%",
            toolbarItems: mgmtToolbarItems(function () { return self.popupInit; }),
            onInitialized: function (e) { self.popupInit = e.component; }
        };

        self.setDataSource = function (data) {
            self.data = data || [];
            if (self.listInit) self.listInit.option("dataSource", self.data);
        };
        self.popupTitle = function (t) { if (self.popupInit) self.popupInit.option("title", t); };
        self.showPopup = function (show) { if (self.popupInit) self.popupInit.option("visible", show !== false); };
        self.hidePopup = function () { self.showPopup(false); };
        self.open = function (title) { if (title) self.popupTitle(title); self.showPopup(true); };
    }

    /* -------- alkanziFormPopup: a popup wrapping a dxForm (form-tab-panel-popup shell). -------- */
    function alkanziFormPopup(config) {
        config = config || {};
        var self = this;
        self.formInit = null;
        self.popupInit = null;

        self.form = {
            keyExpr: "ID",
            colCount: config.colCount || 1,
            focusStateEnabled: true,
            scrollingEnabled: true,
            showRequiredMark: true,
            showColonAfterLabel: false,
            formData: {},
            labelMode: "floating",
            labelLocation: "top",
            items: [],
            onInitialized: function (e) { self.formInit = e.component; }
        };

        var wrapperClass = "form-tab-panel-popup alkanzi-custom-popup" + (config.toolbarColor ? " " + config.toolbarColor : "");
        self.popup = {
            showCloseButton: false,
            focusStateEnabled: true,
            hideOnOutsideClick: false,
            deferRendering: false,
            position: "center",
            dragOutsideBoundary: true,
            resizeEnabled: true,
            restorePosition: true,
            shading: false,
            shadingColor: "rgba(0,0,0,0.5)",
            title: "",
            visible: false,
            width: 780,
            height: "85%",
            wrapperAttr: { class: wrapperClass },
            toolbarItems: mgmtToolbarItems(function () { return self.popupInit; }).concat(
                config.showSubmit ? [{
                    toolbar: "bottom", location: "center", widget: "dxButton",
                    options: {
                        text: config.saveBtnText || "Save", icon: "save",
                        elementAttr: { class: "custom-success-btn" },
                        onClick: function () { if (typeof self.submit === "function") self.submit(); }
                    }
                }] : []),
            onInitialized: function (e) { self.popupInit = e.component; }
        };

        self.getData = function () { return self.formInit ? self.formInit.option("formData") : self.form.formData; };
        self.popupTitle = function (t) { if (self.popupInit) self.popupInit.option("title", t); };
        self.showPopup = function (show) { if (self.popupInit) self.popupInit.option("visible", show !== false); };
        self.hidePopup = function () { self.showPopup(false); };
        self.set = function (title, data) {
            self.form.formData = data || {};
            if (self.formInit) self.formInit.option("formData", self.form.formData);
            self.popupTitle(title);
            self.showPopup(true);
        };
    }

    /* -------- showAlert: the global centred info dialog (#showAlertPopup). -------- */
    var _alert = null;
    w._initAlertPopup = function () {
        var el = document.getElementById("showAlertPopup");
        if (!el || _alert) return;
        _alert = new DevExpress.ui.dxPopup(el, {
            width: 420, height: "auto", visible: false, showTitle: true, title: "Notice",
            dragEnabled: false, hideOnOutsideClick: true, shading: true,
            wrapperAttr: { class: "pw-alert-popup" },
            contentTemplate: function (content) {
                $("<div>").addClass("pw-alert-msg").css({ padding: "6px 4px 14px", fontSize: "14px" })
                    .attr("id", "pwAlertMsg").appendTo(content);
            },
            toolbarItems: [{
                toolbar: "bottom", location: "center", widget: "dxButton",
                options: { text: "Got it", type: "default", stylingMode: "contained", onClick: function () { _alert.hide(); } }
            }]
        });
    };
    w.showAlert = function (msg) {
        if (!_alert) w._initAlertPopup();
        if (!_alert) { alert(msg); return; }
        $("#pwAlertMsg").text(msg || "");
        _alert.show();
    };

    w.listPopup = listPopup;
    w.alkanziFormPopup = alkanziFormPopup;
})(window);
