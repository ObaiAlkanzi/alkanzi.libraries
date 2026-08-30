/* ============================================================
   WsSearchTerminal — the shared omni-search service.

   One factory owns the whole search surface: running the query,
   styling the results, and routing a hit to whatever opens it.
   Every workspace gets it from the shell for free and supplies
   only what is specific to it:

     create({
       handlers: { vendor: function (hit) { ... } },  // extra/override routing
       onOpen:   function (hit) { ... },              // fallback opener
       take: 25
     })

   Factories never touch $scope — the host runs the digest — and
   the markup lives in WorkspaceForms/_SearchTerminal.cshtml.
   ============================================================ */
app.factory("WsSearchTerminal", ["erpApi", "$timeout", function (erpApi, $timeout) {
    "use strict";

    // Visual identity per hit type. The label itself comes from the index, so this is the
    // icon and the colour token only — plus a label fallback for a type indexed before it
    // had one. Colours are token names, never literals, so the terminal follows the theme.
    var META = {
        vendor:         { label: "Vendor",         icon: "fa-truck-field",   token: "--erp-type-vendor" },
        purchase_order: { label: "Purchase Order", icon: "fa-cart-flatbed",  token: "--erp-type-lpo" },
        company:        { label: "Company",        icon: "fa-building",      token: "--erp-type-company" },
        branch:         { label: "Branch",         icon: "fa-code-branch",   token: "--erp-type-branch" }
    };

    var FACETS = [
        { key: "", label: "All", icon: "fa-solid fa-layer-group" },
        { key: "purchase_order", label: "Purchase Orders", icon: "fa-solid fa-cart-flatbed" },
        { key: "vendor", label: "Vendors", icon: "fa-solid fa-truck-field" }
    ];

    function metaFor(type) {
        var m = META[type] || { label: type, icon: "fa-file", token: "--erp-muted" };
        return {
            label: m.label,
            icon: m.icon,
            ink: "var(" + m.token + ")",
            wash: "var(" + m.token + "-tint)"
        };
    }

    return function create(ctx) {
        ctx = ctx || {};

        var take = ctx.take || 25;
        var onOpen = ctx.onOpen || function () { };
        var handlers = ctx.handlers || {};

        var self = { term: "", type: "", status: "", facets: FACETS, metaFor: metaFor };

        // ---- result rendering ----
        function itemTemplate(hit) {
            var m = metaFor(hit.entityType);

            var $row = $("<div>").addClass("ws-hit");

            $("<div>").addClass("ws-hit-icon")
                .css({ background: m.wash, color: m.ink })
                .append($("<i>").addClass("fa-solid " + m.icon))
                .appendTo($row);

            var $body = $("<div>").addClass("ws-hit-body").appendTo($row);

            var $title = $("<div>").addClass("ws-hit-title").appendTo($body);
            $("<span>").addClass("ws-hit-chip")
                .css({ background: m.wash, color: m.ink })
                .text(hit.label || m.label)
                .appendTo($title);
            $("<span>").text(hit.title).appendTo($title);

            if (hit.subtitle) $("<div>").addClass("ws-hit-sub").text(hit.subtitle).appendTo($body);

            var $meta = $("<div>").addClass("ws-hit-meta").appendTo($row);
            if (hit.docNum) $("<span>").addClass("ws-hit-doc").text("#" + hit.docNum).appendTo($meta);
            $("<span>").addClass("ws-hit-branch").text("Branch " + hit.branchId).appendTo($meta);

            return $row;
        }

        self.list = {
            dataSource: [],
            keyExpr: "_key",
            height: 380,
            noDataText: "",
            elementAttr: { class: "ws-hit-list" },
            onInitialized: function (e) { self.listInit = e.component; },
            itemTemplate: itemTemplate,
            onItemClick: function (e) { self.open(e.itemData); }
        };

        self.box = {
            placeholder: "Search across the ERP…",
            mode: "search",
            showClearButton: true,
            valueChangeEvent: "keyup",
            elementAttr: { id: "wsTerminalBox" },
            onInitialized: function (e) { self.boxInit = e.component; },
            onValueChanged: function (e) { self.runDebounced(e.value); },
            onEnterKey: function () { self.run(self.boxInit.option("value")); }
        };

        self.popup = {
            width: 720,
            maxWidth: "94vw",
            height: "auto",
            maxHeight: "84vh",
            showTitle: false,
            shading: true,
            shadingColor: "var(--erp-shading)",
            hideOnOutsideClick: true,
            deferRendering: false,
            visible: false,
            wrapperAttr: { class: "ws-terminal-wrap" },
            position: { my: "top center", at: "top center", offset: "0 90" },
            onInitialized: function (e) { self.popupInit = e.component; },
            onShown: function () { if (self.boxInit) self.boxInit.focus(); }
        };

        // ---- querying ----
        var timer = null;

        self.runDebounced = function (term) {
            if (timer) $timeout.cancel(timer);
            // Long enough that typing a word is one request rather than six, short enough that
            // the list still feels live.
            timer = $timeout(function () { self.run(term); }, 220);
        };

        self.run = function (term) {
            self.term = (term || "").trim();

            if (self.term.length < 2) {
                self.status = self.term.length ? "Keep typing…" : "";
                self.apply([]);
                return;
            }

            self.status = "Searching…";

            return erpApi.get("/api/search", { term: self.term, type: self.type || null, take: take })
                .then(function (result) {
                    var hits = (result.hits || []).map(function (h, i) {
                        h._key = h.entityType + "-" + h.id + "-" + i;
                        return h;
                    });
                    self.status = hits.length
                        ? result.total + (result.total === 1 ? " result" : " results")
                        : "No matches for “" + self.term + "”.";
                    self.apply(hits);
                })
                .catch(function () { self.status = "Search failed."; self.apply([]); });
        };

        self.apply = function (hits) {
            if (self.listInit) self.listInit.option("dataSource", hits);
        };

        self.setType = function (type) {
            if (self.type === type) return;
            self.type = type || "";
            if (self.term.length >= 2) self.run(self.term);
        };

        // ---- routing ----
        // A workspace-specific handler wins; otherwise the workspace's generic opener runs.
        // Matched case-insensitively so a caller need not know the index's exact casing.
        self.open = function (hit) {
            self.hide();

            var key = Object.keys(handlers).filter(function (k) {
                return k.toLowerCase() === String(hit.entityType || "").toLowerCase();
            })[0];

            if (key) return handlers[key](hit);
            return onOpen(hit);
        };

        // ---- visibility ----
        self.show = function () {
            if (self.popupInit) self.popupInit.show();
        };

        self.hide = function () {
            if (self.popupInit) self.popupInit.hide();
        };

        self.toggle = function () {
            if (self.popupInit) self.popupInit.option("visible", !self.popupInit.option("visible"));
        };

        return self;
    };
}]);
