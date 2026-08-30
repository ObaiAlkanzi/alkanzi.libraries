/* ============================================================
   Dashboard page controller.

   Registers on the `app` module created in app.js — it does NOT
   create one. Calling angular.module('app', ['dx']) here would
   replace the module and drop the layout controller with it.
   ============================================================ */
app.controller("dashboardCtrl", ["$scope", "erpApi", function ($scope, erpApi) {
    "use strict";

    $scope.kpis = [];
    $scope.loading = false;

    // ---------- widgets ----------
    $scope.vendorChart = {
        dataSource: [],
        rotated: true,
        size: { height: 300 },
        commonSeriesSettings: { argumentField: "vendor", type: "bar" },
        series: [{ valueField: "amount", name: "Value" }],
        legend: { visible: false },
        valueAxis: [{ title: { text: "AED" } }],
        argumentAxis: { label: { overlappingBehavior: "none" } },
        tooltip: {
            enabled: true,
            customizeTooltip: function (a) {
                return { text: a.argumentText + ": " + DevExpress.localization.formatNumber(a.value, "#,##0") + " AED" };
            }
        },
        onInitialized: function (e) { $scope.vendorChart.instance = e.component; }
    };

    $scope.ordersGrid = {
        dataSource: [],
        keyExpr: "id",
        showBorders: false,
        columnAutoWidth: true,
        rowAlternationEnabled: true,
        hoverStateEnabled: true,
        searchPanel: { visible: true, width: 240, placeholder: "Search…" },
        filterRow: { visible: true },
        headerFilter: { visible: true },
        paging: { pageSize: 10 },
        pager: { visible: true, showInfo: true, showNavigationButtons: true },
        noDataText: "No purchase orders.",
        onInitialized: function (e) { $scope.ordersGrid.instance = e.component; },
        columns: [
            { dataField: "id", caption: "Doc #", width: 100, alignment: "center" },
            { dataField: "vendor", caption: "Vendor" },
            { dataField: "date", caption: "Date", dataType: "date", format: "dd MMM yyyy", width: 130 },
            { dataField: "amount", caption: "Amount", dataType: "number", format: "#,##0.00", width: 140 },
            {
                dataField: "status", caption: "Status", width: 120, alignment: "center",
                cellTemplate: function (container, options) {
                    // Tone is derived from the status rather than stored with it, so a new
                    // status shows up in a neutral chip instead of an unstyled one.
                    var tones = { Approved: "success", Pending: "warning", Rejected: "danger", Draft: "muted" };
                    $("<span>")
                        .addClass("erp-chip erp-chip--" + (tones[options.value] || "muted"))
                        .text(options.value)
                        .appendTo(container);
                }
            }
        ]
    };

    $scope.reloadBtn = {
        icon: "refresh",
        hint: "Reload",
        stylingMode: "contained",
        onClick: function () { $scope.load(); }
    };

    // ---------- data ----------
    $scope.load = function () {
        $scope.loading = true;
        return erpApi.get("/api/dashboard")
            .then(function (d) {
                d = d || {};
                $scope.kpis = d.kpis || [];
                if ($scope.ordersGrid.instance) $scope.ordersGrid.instance.option("dataSource", d.orders || []);
                if ($scope.vendorChart.instance) $scope.vendorChart.instance.option("dataSource", d.byVendor || []);
            })
            .finally(function () { $scope.loading = false; });
    };

    $scope.load();
}]);
