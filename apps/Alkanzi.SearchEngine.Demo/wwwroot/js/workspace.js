/* ============================================================
   Procurement Workspace — DEMO controller.
   AngularJS + DevExtreme 22.2.3 (angular.module('demo', ['dx'])),
   reproducing the ERP Procurement Workspace look & components
   (mainToolbar, KPI strip, Top Vendors chart, Explorer grid,
   omni-search popover, listPopup, alkanziFormPopup) wired to the
   demo API (search / kpis / explorer / top-vendors).
   ============================================================ */
var DemoApp = angular.module("demo", ["dx"]);

DemoApp.controller("procurementWorkspaceCtrl", ["$scope", "$http", "$timeout", function ($scope, $http, $timeout) {
    "use strict";

    var API = (window.__API_BASE__ || "").replace(/\/+$/, "");
    var _dateFormat = "dd MMM yyyy";

    function apiGet(path, params) {
        return $http.get(API + path, { params: params || {} }).then(function (r) { return r.data; });
    }

    // Visual identity per hit / entity type (matches the ERP globalSearch _meta).
    var _meta = {
        inventory: { label: "Purchase Order", icon: "fa-cart-flatbed", color: "#1D5A8A", bg: "rgba(29,90,138,.12)" },
        call: { label: "Call", icon: "fa-headset", color: "#6d28d9", bg: "rgba(124,58,237,.12)" },
        vendor: { label: "Vendor", icon: "fa-truck-field", color: "#475569", bg: "rgba(71,85,105,.12)" },
        customer: { label: "Customer", icon: "fa-user-tie", color: "#c5221f", bg: "rgba(197,34,31,.12)" }
    };

    // ===================== KPI strip =====================
    $scope.kpi = { purchaseOrders: 0, pending: 0, calls: 0, vendors: 0, pendingPct: 0 };
    $scope._kpiRefreshing = false;

    $scope.refreshKpis = function () {
        $scope._kpiRefreshing = true;
        apiGet("/api/procurement/kpis").then(function (tiles) {
            var by = {};
            (tiles || []).forEach(function (t) { by[t.key] = t.value; });
            $scope.kpi.purchaseOrders = by.lpos || 0;
            $scope.kpi.pending = by.pending || 0;
            $scope.kpi.calls = by.calls || 0;
            $scope.kpi.vendors = by.vendors || 0;
            $scope.kpi.pendingPct = $scope.kpi.purchaseOrders
                ? Math.round(($scope.kpi.pending / $scope.kpi.purchaseOrders) * 100) : 0;
        })["finally"](function () { $scope._kpiRefreshing = false; });
    };

    // ===================== Main toolbar =====================
    $scope.mainToolbar = {
        elementAttr: { class: "ap-hero--erp ap-erp-toolbar", id: "pwMainToolbar" },
        items: [
            {
                location: "before", locateInMenu: "never",
                template: function () {
                    var $b = $("<div>").addClass("ap-hero-brand").css({ display: "flex", alignItems: "center", gap: "10px", fontWeight: 700, color: "#fff" });
                    $("<span>").addClass("ap-hero-logo").css({ width: "30px", height: "30px", borderRadius: "8px", background: "rgba(255,255,255,.16)", display: "grid", placeItems: "center" })
                        .append($("<i>").addClass("fa-solid fa-cubes")).appendTo($b);
                    $("<span>").text("Procurement Workspace").appendTo($b);
                    return $b;
                }
            },
            {
                location: "before", locateInMenu: "never", widget: "dxTextBox", cssClass: "ap-tb-search",
                options: {
                    width: 440, mode: "search", elementAttr: { id: "pwGlobalSearchBox" },
                    placeholder: "Search id across LPOs, calls, vendors…",
                    showClearButton: true, valueChangeEvent: "keyup",
                    onInitialized: function (e) { $scope._globalSearchBox = e.component; },
                    onValueChanged: function (e) { $scope._globalSearchDebounced(e.value); },
                    onEnterKey: function () { $scope._globalSearch($scope._globalSearchBox.option("value")); },
                    buttons: [{
                        name: "go", location: "after",
                        options: { icon: "search", stylingMode: "text", onClick: function () { $scope._globalSearch($scope._globalSearchBox && $scope._globalSearchBox.option("value")); } }
                    }]
                }
            },
            {
                location: "after", locateInMenu: "never",
                template: function () {
                    var $btn = $("<button>").attr({ type: "button", id: "pwKpiRefreshBtn", title: "Refresh KPIs" }).addClass("ap-hero-icon");
                    var $i = $("<i>").addClass("fa-solid fa-rotate").appendTo($btn);
                    $btn.on("click", function () { if ($scope._kpiRefreshing) return; $scope.refreshKpis(); });
                    $scope.$watch("_kpiRefreshing", function (v) { $i.toggleClass("fa-spin", !!v); });
                    return $btn;
                }
            },
            {
                location: "after", locateInMenu: "never",
                template: function () {
                    var $u = $("<div>").attr({ id: "pwUserBtn", title: "Profile" }).addClass("ap-hero-user");
                    var $av = $("<div>").addClass("ap-hero-avatar").append($("<i>").addClass("fa-solid fa-user")).appendTo($u);
                    var $m = $("<div>").addClass("ap-hero-user-meta").appendTo($u);
                    $("<div>").addClass("ap-hero-user-hi").text("Signed in").appendTo($m);
                    $("<div>").addClass("ap-hero-user-name").text("Demo User").appendTo($m);
                    return $u;
                }
            }
        ]
    };

    // ===================== Global omni-search =====================
    $scope.globalSearch = {};
    $scope.globalSearch.popover = {
        target: "#pwGlobalSearchBox",
        position: { my: "top left", at: "bottom left", of: "#pwGlobalSearchBox", offset: "0 6" },
        width: 460, maxWidth: "92vw", height: "auto",
        shading: false, showTitle: false, hideOnOutsideClick: true,
        wrapperAttr: { class: "pw-search-popover" }, visible: false,
        onInitialized: function (e) { $scope.globalSearch.popoverInit = e.component; }
    };
    $scope.globalSearch.list = {
        dataSource: [], keyExpr: "_KEY", height: 300,
        elementAttr: { class: "asset-picker-list shortlist-list pw-search-list" },
        noDataText: "No matches.",
        onInitialized: function (e) { $scope.globalSearch.listInit = e.component; },
        itemTemplate: function (data) {
            var m = _meta[data.TYPE] || { label: data.TYPE, icon: "fa-file", color: "#475569", bg: "rgba(71,85,105,.12)" };
            var $row = $("<div>").addClass("shortlist-row");
            $("<div>").addClass("shortlist-avatar").css("background", m.color)
                .append($("<i>").addClass("fa-solid " + m.icon)).appendTo($row);
            var $info = $("<div>").addClass("shortlist-info").appendTo($row);
            var $title = $("<div>").addClass("shortlist-title").appendTo($info);
            $("<span>").css({ padding: "1px 8px", borderRadius: "7px", fontSize: "11px", fontWeight: 700, background: m.bg, color: m.color, marginRight: "8px" }).text(m.label).appendTo($title);
            $("<span>").text(data.LABEL).appendTo($title);
            if (data.SUB) $("<div>").addClass("shortlist-sub").text(data.SUB).appendTo($info);
            return $row;
        },
        onItemClick: function (e) { $scope._openSearchResult(e.itemData); }
    };

    var _searchTimer = null;
    $scope._globalSearchDebounced = function (term) {
        if (_searchTimer) $timeout.cancel(_searchTimer);
        _searchTimer = $timeout(function () { $scope._globalSearch(term); }, 220);
    };
    $scope._globalSearch = function (term) {
        term = (term || "").trim();
        if (!term) { if ($scope.globalSearch.popoverInit) $scope.globalSearch.popoverInit.hide(); return; }
        apiGet("/api/search", { term: term, take: 25 }).then(function (res) {
            var rows = (res.hits || []).map(function (h, i) {
                return { TYPE: h.entityType, ID: h.id, LABEL: h.title, SUB: h.subtitle, _KEY: h.entityType + "-" + h.id + "-" + i };
            });
            if ($scope.globalSearch.listInit) {
                var h = Math.min(Math.max(rows.length, 1) * 62 + 8, Math.round(window.innerHeight * 0.8));
                $scope.globalSearch.listInit.option("height", h);
                $scope.globalSearch.listInit.option("dataSource", rows);
            }
            if ($scope.globalSearch.popoverInit) $scope.globalSearch.popoverInit.show();
        });
    };
    $scope._openSearchResult = function (item) {
        if ($scope.globalSearch.popoverInit) $scope.globalSearch.popoverInit.hide();
        $scope.docForm.set(_meta[item.TYPE] ? _meta[item.TYPE].label + " · " + item.LABEL : item.LABEL, {
            TYPE_LABEL: (_meta[item.TYPE] || {}).label || item.TYPE, ID: item.ID, TITLE: item.LABEL, SUBTITLE: item.SUB || ""
        });
    };

    // ===================== Top Vendors chart =====================
    $scope.topVendors = { data: [] };
    $scope.topVendors.chart = {
        onInitialized: function (e) { $scope.topVendors.chartInit = e.component; },
        dataSource: [], rotated: true, palette: "Soft Blue", size: { height: 360 },
        commonSeriesSettings: { argumentField: "ACCOUNT_NAME", type: "bar" },
        series: [{ valueField: "ORDERS", name: "Orders", color: "#1D5A8A", label: { visible: true, backgroundColor: "transparent", font: { weight: 600 } } }],
        legend: { visible: false },
        argumentAxis: { label: { overlappingBehavior: "none" } },
        valueAxis: [{ title: { text: "Orders" }, allowDecimals: false }],
        tooltip: { enabled: true, customizeTooltip: function (a) { return { text: a.argumentText + ": " + a.valueText + " orders" }; } },
        onPointClick: function (e) { var d = e.target && e.target.data; if (d) $scope.topVendors.openVendorLpos(d.ACCOUNT_NAME); }
    };
    $scope.topVendors.load = function () {
        apiGet("/api/procurement/top-vendors", { top: 15 }).then(function (rows) {
            $scope.topVendors.data = (rows || []).map(function (v) { return { ACCOUNT_NAME: v.vendor, ORDERS: v.orders }; });
            if ($scope.topVendors.chartInit) $scope.topVendors.chartInit.option("dataSource", $scope.topVendors.data);
        });
    };

    // Vendor LPOs drill-down — a listPopup (kz-list-popup shell).
    $scope.topVendors.vendorLpos = new listPopup("vendorLpos");
    $scope.topVendors.vendorLpos.popup.title = "Vendor LPOs";
    $scope.topVendors.vendorLpos.popup.width = 640;
    Object.assign($scope.topVendors.vendorLpos.list, {
        elementAttr: { class: "asset-picker-list shortlist-list" }, searchEnabled: true, searchExpr: ["title", "docNum"],
        noDataText: "No LPOs for this vendor.",
        itemTemplate: function (data) {
            var $row = $("<div>").addClass("shortlist-row");
            $("<div>").addClass("shortlist-avatar").css("background", "#1D5A8A")
                .append($("<i>").addClass("fa-solid fa-cart-flatbed")).appendTo($row);
            var $info = $("<div>").addClass("shortlist-info").appendTo($row);
            $("<div>").addClass("shortlist-title").append($("<span>").addClass("shortlist-id").text("LPO #" + (data.docNum || data.id))).appendTo($info);
            var date = data.date ? DevExpress.localization.formatDate(new Date(data.date), _dateFormat) : "";
            $("<div>").addClass("shortlist-sub").text([date, data.title].filter(Boolean).join("  ·  ")).appendTo($info);
            var $right = $("<div>").addClass("shortlist-right").appendTo($row);
            $("<div>").addClass("shortlist-value").text("Branch " + data.branchId).appendTo($right);
            return $row;
        }
    });
    $scope.topVendors.openVendorLpos = function (vendorName) {
        apiGet("/api/procurement/explorer", { tab: "lpo", term: vendorName, take: 100 }).then(function (page) {
            $scope.topVendors.vendorLpos.setDataSource(page.rows || []);
            $scope.topVendors.vendorLpos.open(vendorName + " · " + (page.total || 0) + " LPOs");
        });
    };

    // ===================== Explorer grid + view tabs =====================
    $scope.workspaceTabs = [
        { key: "lpo", label: "Purchase Orders", icon: "fa-solid fa-cart-flatbed" },
        { key: "call", label: "Calls", icon: "fa-solid fa-headset" },
        { key: "vendor", label: "Vendors", icon: "fa-solid fa-truck-field" }
    ];
    $scope.workspaceView = "lpo";

    $scope.explorerGrid = { dataGrid: null };
    $scope.explorerGrid.dataGrid = {
        dataSource: [], keyExpr: "id", height: 560, remoteOperations: false,
        showBorders: false, rowAlternationEnabled: true, columnAutoWidth: true, wordWrapEnabled: false,
        searchPanel: { visible: true, width: 280, placeholder: "Search doc #, name…" },
        filterRow: { visible: true }, headerFilter: { visible: true },
        paging: { pageSize: 25 },
        pager: { visible: true, allowedPageSizes: [10, 25, 50, "all"], showPageSizeSelector: true, showInfo: true, showNavigationButtons: true },
        export: { enabled: true, fileName: "Documents" },
        onInitialized: function (e) { $scope.explorerGrid.gridInit = e.component; },
        columns: [
            { dataField: "docNum", caption: "Doc #", width: 120, fixed: true, fixedPosition: "left", cellTemplate: function (c, o) { $("<span>").addClass("pw-doc").css({ fontWeight: 600, color: "#1D5A8A" }).text((o.data && (o.data.docNum || o.data.id))).appendTo(c); } },
            { dataField: "title", caption: "Name" },
            { dataField: "date", caption: "Date", dataType: "date", format: _dateFormat, width: 130 },
            { dataField: "branchId", caption: "Branch", width: 100, alignment: "center", cellTemplate: function (c, o) { $("<span>").addClass("pw-chip").text("Branch " + (o.data && o.data.branchId)).appendTo(c); } },
            {
                type: "buttons", caption: "", width: 70, fixed: true, fixedPosition: "right",
                buttons: [{
                    hint: "Open",
                    template: function (container, options) {
                        var row = options.data;
                        $("<span>").addClass("fa-solid fa-eye custom-cell-icon").css({ cursor: "pointer", color: "#1D5A8A" })
                            .attr("title", "Open document")
                            .on("click", function (ev) { ev.stopPropagation(); $scope._openExplorerDoc(row); }).appendTo(container);
                    }
                }]
            }
        ],
        onToolbarPreparing: function (e) {
            e.toolbarOptions.items.unshift({
                location: "before", widget: "dxButton",
                options: { icon: "refresh", hint: "Reload", onClick: function () { $scope._loadExplorer($scope.workspaceView); } }
            });
        }
    };

    $scope.setWorkspaceView = function (key) {
        if ($scope.workspaceView === key) return;
        $scope.workspaceView = key;
        $scope._loadExplorer(key);
    };
    $scope._loadExplorer = function (view) {
        apiGet("/api/procurement/explorer", { tab: view, take: 100 }).then(function (page) {
            var nameCol = view === "call" ? "Client" : view === "vendor" ? "Vendor" : "Supplier";
            if ($scope.explorerGrid.gridInit) {
                $scope.explorerGrid.gridInit.columnOption("title", "caption", nameCol);
                $scope.explorerGrid.gridInit.columnOption("date", "visible", view !== "vendor");
                $scope.explorerGrid.gridInit.columnOption("docNum", "caption", view === "vendor" ? "Id" : "Doc #");
                $scope.explorerGrid.gridInit.option("dataSource", page.rows || []);
            }
        });
    };
    $scope._openExplorerDoc = function (row) {
        var m = _meta[$scope.workspaceView === "lpo" ? "inventory" : $scope.workspaceView] || {};
        $scope.docForm.set((m.label || "Document") + " · " + (row.title || ("#" + row.id)), {
            TYPE_LABEL: m.label || "Document", ID: row.id,
            DOC_NUM: row.docNum || "—", TITLE: row.title || "",
            DATE: row.date ? DevExpress.localization.formatDate(new Date(row.date), _dateFormat) : "—",
            BRANCH: "Branch " + row.branchId
        });
    };

    // ===================== Detail form (alkanziFormPopup) =====================
    $scope.docForm = new alkanziFormPopup({ colCount: 2, showSubmit: false, toolbarColor: "primaryPopup" });
    $scope.docForm.popup.width = 620;
    $scope.docForm.popup.height = "auto";
    $scope.docForm.form.items = [
        { itemType: "group", caption: "Document", colSpan: 2, colCount: 2, items: [
            { dataField: "TYPE_LABEL", label: { text: "Type" }, editorOptions: { readOnly: true } },
            { dataField: "ID", label: { text: "Id" }, editorOptions: { readOnly: true } },
            { dataField: "DOC_NUM", label: { text: "Document #" }, editorOptions: { readOnly: true } },
            { dataField: "DATE", label: { text: "Date" }, editorOptions: { readOnly: true } },
            { dataField: "TITLE", label: { text: "Name" }, colSpan: 2, editorOptions: { readOnly: true } },
            { dataField: "BRANCH", label: { text: "Branch" }, editorOptions: { readOnly: true } },
            { dataField: "SUBTITLE", label: { text: "Detail" }, colSpan: 2, editorOptions: { readOnly: true } }
        ] }
    ];

    // ===================== startup =====================
    $scope.startup = function () {
        $timeout(window._initAlertPopup, 0);
        $scope.refreshKpis();
        $scope.topVendors.load();
        $scope._loadExplorer($scope.workspaceView);
    };
}]);
