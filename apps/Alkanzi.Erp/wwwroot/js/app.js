/* ============================================================
   Alkanzi.Erp — the AngularJS module and the layout controller.

   The module is created HERE, once, with the 'dx' dependency that
   DevExtreme registers. Never call angular.module('app', ['dx'])
   again from a page controller: that recreates the module and
   discards every service and controller already registered on it.
   Page scripts must use `app.controller(...)` instead.

   Script order is fixed by _Layout and matters:
     jQuery -> AngularJS -> dx.all.js -> app.js -> page controller
   ============================================================ */
var app = angular.module("app", ["dx"]);

app.controller("_layoutCtrl", ["$scope", "$http", "WsSearchTerminal", function ($scope, $http, WsSearchTerminal) {
    "use strict";

    var DRAWER_KEY = "erp-drawer-opened";
    var THEME_KEY = "erp-theme";

    // ---------- responsive drawer ----------
    // Large screens shrink the content beside the menu; smaller ones overlay
    // it, so the menu never eats the working area on a laptop or tablet.
    var breakpoints = {
        xSmall: window.matchMedia("(max-width: 599.99px)"),
        large: window.matchMedia("(min-width: 1280px)")
    };

    function drawer() { return $scope.mainDrawer.instance; }

    // One source of truth for the responsive options, used both for the initial
    // config and for every later re-application. It must also be part of the
    // initial config, not applied only imperatively: switching the DevExtreme
    // theme re-creates widgets from their ORIGINAL options, so anything set
    // purely through .option() afterwards is silently dropped — which lost the
    // collapsed icon rail (minSize) on the first theme toggle.
    function drawerOptions() {
        var isXSmall = breakpoints.xSmall.matches;
        var isLarge = breakpoints.large.matches;
        return {
            openedStateMode: isLarge ? "shrink" : "overlap",
            revealMode: isXSmall ? "slide" : "expand",
            minSize: isXSmall ? 0 : 60,
            shading: !isLarge
        };
    }

    function updateDrawer() {
        var d = drawer();
        if (d) d.option(drawerOptions());
    }

    function restoreDrawerOpened() {
        if (!breakpoints.large.matches) return false;
        try {
            var saved = sessionStorage.getItem(DRAWER_KEY);
            return saved === null ? true : saved === "true";
        } catch (e) { return true; }
    }

    function saveDrawerOpened() {
        try { sessionStorage.setItem(DRAWER_KEY, drawer().option("opened")); } catch (e) { }
    }

    $scope.mainDrawer = angular.extend({
        opened: restoreDrawerOpened(),
        position: "left",
        template: "navigation-menu",
        closeOnOutsideClick: function () { return !breakpoints.large.matches; },
        onInitialized: function (e) { $scope.mainDrawer.instance = e.component; },
        onOptionChanged: function (e) { if (e.name === "opened") saveDrawerOpened(); }
    }, drawerOptions());

    $scope.toggleDrawer = function () {
        var d = drawer();
        if (d) d.toggle();
    };

    // ---------- theme ----------
    // data-theme goes on <html>, not on a container: DevExtreme renders popups
    // and overlays at <body> level, so a container-scoped attribute would leave
    // every popup on the light theme. DevExtreme itself needs a stylesheet swap
    // rather than CSS variables, because dx.light.css hardcodes its colours —
    // both link tags ship and we flip `.disabled`.
    $scope.theme = {
        current: (function () {
            try { return localStorage.getItem(THEME_KEY) || "light"; } catch (e) { return "light"; }
        })(),
        apply: function (mode) {
            mode = mode === "dark" ? "dark" : "light";
            $scope.theme.current = mode;
            document.documentElement.setAttribute("data-theme", mode);

            var dark = document.getElementById("dxDarkTheme");
            if (dark) dark.disabled = mode !== "dark";

            try { DevExpress.ui.themes.current(mode === "dark" ? "generic.dark" : "generic.light"); } catch (e) { }
            try { localStorage.setItem(THEME_KEY, mode); } catch (e) { }

            // The theme swap rebuilds the widgets, and that rebuild lands on its
            // own schedule — after this call. Re-assert the responsive drawer
            // options across that window instead of once, or the rail comes back
            // at zero width.
            var tries = 0;
            (function reassert() {
                updateDrawer();
                if (++tries < 6) setTimeout(reassert, 200);
            })();
        },
        toggle: function () { $scope.theme.apply($scope.theme.current === "dark" ? "light" : "dark"); }
    };

    // ---------- navigation ----------
    // One list drives both the drawer tree and the breadcrumb title. `path` is
    // what a menu item navigates to; parents carry no path.
    $scope.menu = [
        { id: "home", text: "Dashboard", icon: "home", path: "/" },
        {
            id: "procurement", text: "Procurement", icon: "cart", expanded: true, items: [
                { id: "lpo", text: "Purchase Orders", icon: "file", path: "/Procurement/PurchaseOrders" },
                { id: "req", text: "Requisitions", icon: "orderedlist", path: "/Procurement/Requisitions" },
                { id: "vendors", text: "Vendors", icon: "group", path: "/Procurement/Vendors" }
            ]
        },
        {
            id: "sales", text: "Sales", icon: "money", items: [
                { id: "customers", text: "Customers", icon: "user", path: "/Sales/Customers" },
                { id: "invoices", text: "Invoices", icon: "doc", path: "/Sales/Invoices" }
            ]
        },
        {
            id: "it", text: "IT", icon: "preferences", expanded: true, items: [
                { id: "structure", text: "Organization Structure", icon: "hierarchy", path: "/It/Workspace" }
            ]
        }
    ];

    $scope.mainTree = {
        items: $scope.menu,
        keyExpr: "id",
        displayExpr: "text",
        selectionMode: "single",
        selectByClick: true,
        focusStateEnabled: false,
        expandEvent: "click",
        width: "100%",
        onInitialized: function (e) { $scope.mainTree.instance = e.component; },
        onItemClick: function (e) {
            var item = e.itemData;
            if (!item || !item.path) return;   // a parent node just expands

            // On overlay-mode screens the drawer covers the content, so close it
            // before navigating or the new page opens behind the menu.
            if (!breakpoints.large.matches) {
                var d = drawer();
                if (d) d.hide();
            }
            window.location.href = item.path;
        }
    };

    // ---------- toolbar ----------
    $scope.mainToolbar = {
        elementAttr: { class: "erp-toolbar", id: "erpMainToolbar" },
        items: [
            {
                location: "before", locateInMenu: "never", widget: "dxButton",
                options: { icon: "menu", stylingMode: "text", hint: "Menu", onClick: function () { $scope.toggleDrawer(); } }
            },
            {
                location: "before", locateInMenu: "never",
                template: function () {
                    return $("<div>").addClass("erp-brand").text($scope.appTitle || "Alkanzi ERP");
                }
            },
            {
                location: "after", locateInMenu: "never", widget: "dxTextBox",
                options: {
                    width: 280, mode: "search", placeholder: "Search…", showClearButton: true,
                    elementAttr: { id: "erpSearchBox", class: "erp-search" },
                    onEnterKey: function (e) { $scope.search(e.component.option("value")); }
                }
            },
            {
                location: "after", locateInMenu: "never",
                template: function () {
                    var $btn = $("<button>").attr({ type: "button", id: "erpThemeBtn", title: "Toggle theme" }).addClass("erp-icon-btn");
                    var $i = $("<i>").addClass("fa-solid").appendTo($btn);
                    function paint(mode) { $i.toggleClass("fa-sun", mode === "dark").toggleClass("fa-moon", mode !== "dark"); }
                    paint($scope.theme.current);
                    $btn.on("click", function () { $scope.$apply(function () { $scope.theme.toggle(); }); });
                    $scope.$watch("theme.current", paint);
                    return $btn;
                }
            },
            {
                location: "after", locateInMenu: "never",
                template: function () {
                    var $u = $("<div>").addClass("erp-user").attr("id", "erpUserBtn");
                    $("<span>").addClass("erp-avatar").append($("<i>").addClass("fa-solid fa-user")).appendTo($u);
                    $("<span>").addClass("erp-user-name").text($scope.userName || "Signed in").appendTo($u);
                    return $u;
                }
            }
        ]
    };

    // The shared search terminal, instantiated once for every workspace. A workspace adds
    // its own routing through cfg rather than building a second search.
    $scope.terminal = WsSearchTerminal({
        onOpen: function (hit) {
            // No workspace-specific handler claimed this hit type. Saying so beats a click
            // that appears to do nothing.
            DevExpress.ui.notify("Nothing is wired up yet to open a " + (hit.label || hit.entityType) + ".", "info", 3000);
        }
    });

    $scope.search = function (term) {
        $scope.terminal.show();
        if (term) $scope.terminal.run(term);
    };

    // ---------- startup ----------
    $scope.init = function () {
        $scope.theme.apply($scope.theme.current);
        updateDrawer();

        // Re-evaluate the drawer mode when the viewport crosses a breakpoint.
        $.each(breakpoints, function (_, mq) {
            var handler = function (e) { if (e.matches) updateDrawer(); };
            if (mq.addEventListener) mq.addEventListener("change", handler);
            else mq.addListener(handler);   // Safari < 14
        });

        // Highlight the menu entry for the page actually being shown.
        var here = window.location.pathname.toLowerCase();
        var match = null;
        (function walk(items) {
            (items || []).forEach(function (i) {
                if (i.path && here === i.path.toLowerCase()) match = i;
                walk(i.items);
            });
        })($scope.menu);
        if (match && $scope.mainTree.instance) $scope.mainTree.instance.selectItem(match.id);
    };
}]);

/* A page with no controller of its own still needs a registered name for the shell's
   ng-controller, because an empty or unknown one throws and blanks the page. */
app.controller("emptyCtrl", [function () { }]);
