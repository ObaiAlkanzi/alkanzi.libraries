(function () {
    "use strict";

    var app = angular.module("searchApp", []);

    var TYPE_META = {
        vendor:    { label: "Vendor",         color: "#0b8043", icon: "🏬" },
        customer:  { label: "Customer",       color: "#c5221f", icon: "👤" },
        inventory: { label: "Purchase Order", color: "#1a73e8", icon: "📄" },
        call:      { label: "Call",           color: "#a142f4", icon: "📞" }
    };

    var TABS = [
        { key: "",          label: "All" },
        { key: "inventory", label: "Purchase Orders" },
        { key: "call",      label: "Calls" },
        { key: "vendor",    label: "Vendors" },
        { key: "customer",  label: "Customers" }
    ];

    var PAGE_SIZE = 10;

    app.constant("apiBase", (window.__API_BASE__ || "").replace(/\/+$/, ""));

    app.factory("api", ["$http", "apiBase", function ($http, apiBase) {
        return {
            search: function (term, type, skip, take) {
                return $http.get(apiBase + "/api/search", {
                    params: { term: term, types: type || null, skip: skip, take: take }
                }).then(function (r) { return r.data; });
            }
        };
    }]);

    app.controller("searchCtrl", ["$scope", "$timeout", "api", function ($scope, $timeout, api) {
        var vm = $scope;

        vm.tabs = TABS;
        vm.typeMeta = function (t) { return TYPE_META[t] || { label: t, color: "#5f6368", icon: "•" }; };

        // ----- state -----
        vm.term = "";
        vm.activeType = "";
        vm.hasSearched = false;
        vm.loading = false;
        vm.results = [];
        vm.page = 1;
        vm.hasNext = false;
        vm.total = 0;
        vm.elapsed = "0.00";

        // ----- read the query string so /Workspace/Search?q=…&type=…&page= lands pre-searched -----
        function readUrl() {
            var p = new URLSearchParams(window.location.search);
            vm.term = p.get("q") || "";
            vm.activeType = p.get("type") || "";
            vm.page = Math.max(1, parseInt(p.get("page"), 10) || 1);
        }

        function writeUrl() {
            var p = new URLSearchParams();
            if (vm.term) p.set("q", vm.term);
            if (vm.activeType) p.set("type", vm.activeType);
            if (vm.page > 1) p.set("page", vm.page);
            var qs = p.toString();
            window.history.replaceState(null, "", qs ? ("?" + qs) : window.location.pathname);
        }

        // ----- search -----
        vm.submit = function () {
            var q = (vm.term || "").trim();
            if (!q) return;
            vm.page = 1;
            run();
        };

        vm.setType = function (key) {
            if (vm.activeType === key) return;
            vm.activeType = key;
            vm.page = 1;
            run();
        };

        function run() {
            var q = (vm.term || "").trim();
            if (!q) { vm.hasSearched = false; return; }
            vm.hasSearched = true;
            vm.loading = true;
            writeUrl();

            var skip = (vm.page - 1) * PAGE_SIZE;
            var started = window.performance ? performance.now() : 0;

            api.search(q, vm.activeType, skip, PAGE_SIZE).then(function (d) {
                vm.loading = false;
                vm.results = d.hits || [];
                vm.total = d.total || 0;
                // With candidate-pool paging we can't know the true total, so "has next page"
                // is inferred from a full page coming back.
                vm.hasNext = vm.results.length === PAGE_SIZE;
                vm.elapsed = window.performance ? ((performance.now() - started) / 1000).toFixed(2) : "0.00";
                window.scrollTo(0, 0);
            }, function () {
                vm.loading = false; vm.results = []; vm.hasNext = false; vm.total = 0;
            });
        }

        // ----- paging -----
        vm.goto = function (p) {
            if (p < 1 || p === vm.page) return;
            vm.page = p; run();
        };
        vm.prev = function () { if (vm.page > 1) vm.goto(vm.page - 1); };
        vm.next = function () { if (vm.hasNext) vm.goto(vm.page + 1); };

        // Rolling window of page numbers (true total is unknown, so we show up to one page ahead).
        vm.pageNumbers = function () {
            var end = vm.page + (vm.hasNext ? 1 : 0);
            var start = Math.max(1, end - 9);
            var arr = [];
            for (var i = start; i <= end; i++) arr.push(i);
            return arr;
        };

        vm.rangeLabel = function () {
            if (!vm.results.length) return "";
            var from = (vm.page - 1) * PAGE_SIZE + 1;
            return "Results " + from + "–" + (from + vm.results.length - 1);
        };

        vm.clear = function () { vm.term = ""; };

        // ----- init -----
        readUrl();
        if ((vm.term || "").trim()) run();
    }]);
})();
