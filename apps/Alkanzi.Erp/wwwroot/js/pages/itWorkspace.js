/* ============================================================
   IT workspace — the organization structure tree.

   Organization -> Companies -> Branches, loaded a level at a time
   as nodes expand, with add / edit / delete at every level.

   Registers on the `app` module created in app.js. Every call goes
   through erpApi to Alkanzi.Erp.Api; nothing here talks to the MVC
   app's own controllers.
   ============================================================ */
app.controller("itWorkspaceCtrl", ["$scope", "erpApi", function ($scope, erpApi) {
    "use strict";

    // The three levels, described once. Everything below — icons, labels, endpoints, the
    // shape of a new row — reads from here, so adding a level later is a table entry rather
    // than a fourth branch through every function.
    var LEVELS = {
        organization: {
            label: "Organization",
            icon: "fa-sitemap",
            token: "--erp-type-org",
            endpoint: "/api/organizations",
            childLevel: "company",
            blank: function () { return { code: "", name: "", isActive: true }; }
        },
        company: {
            label: "Company",
            icon: "fa-building",
            token: "--erp-type-company",
            endpoint: "/api/companies",
            childLevel: "branch",
            parentKey: "organizationId",
            blank: function (parentId) { return { organizationId: parentId, code: "", name: "", currency: "AED", isActive: true }; }
        },
        branch: {
            label: "Branch",
            icon: "fa-code-branch",
            token: "--erp-type-branch",
            endpoint: "/api/branches",
            childLevel: null,
            parentKey: "companyId",
            blank: function (parentId) { return { companyId: parentId, code: "", name: "", isActive: true }; }
        }
    };

    $scope.selected = null;
    $scope.busy = false;

    // ---------- tree ----------
    // Nodes carry a composite key ("company-3"), because ids only repeat across levels —
    // organization 1 and company 1 both exist, and a plain id would collide in the tree.
    function nodeKey(level, id) { return level + "-" + id; }

    function toNode(level, row, parentKey) {
        var cfg = LEVELS[level];
        return {
            id: nodeKey(level, row.id),
            parentId: parentKey,
            level: level,
            entityId: row.id,
            row: row,
            text: row.code + " — " + row.name,
            icon: "fa-solid " + cfg.icon,
            // A node is expandable when its level has children. Organizations and companies
            // report their child counts, so a childless one shows no expander rather than an
            // arrow that opens onto nothing.
            hasItems: cfg.childLevel !== null &&
                (row.companyCount === undefined && row.branchCount === undefined
                    ? true
                    : (row.companyCount || row.branchCount || 0) > 0),
            expanded: false
        };
    }

    $scope.tree = {
        dataStructure: "plain",
        keyExpr: "id",
        parentIdExpr: "parentId",
        displayExpr: "text",
        selectionMode: "single",
        selectByClick: true,
        virtualModeEnabled: true,
        focusStateEnabled: false,
        width: "100%",
        noDataText: "No organizations yet.",
        elementAttr: { class: "it-tree" },
        onInitialized: function (e) { $scope.tree.instance = e.component; },

        // Lazy per level: expanding is what fetches the children, so opening the page costs
        // one request rather than walking the whole structure up front.
        createChildren: function (parentNode) {
            if (!parentNode) {
                return erpApi.get(LEVELS.organization.endpoint)
                    .then(function (rows) { return rows.map(function (r) { return toNode("organization", r, null); }); });
            }

            var data = parentNode.itemData;
            var cfg = LEVELS[data.level];
            if (!cfg.childLevel) return [];

            var childCfg = LEVELS[cfg.childLevel];
            var params = {};
            params[childCfg.parentKey] = data.entityId;

            return erpApi.get(childCfg.endpoint, params)
                .then(function (rows) {
                    return rows.map(function (r) { return toNode(cfg.childLevel, r, data.id); });
                });
        },

        onItemClick: function (e) {
            $scope.$applyAsync(function () { $scope.select(e.itemData); });
        }
    };

    $scope.select = function (node) {
        $scope.selected = node || null;
    };

    // ---------- editor ----------
    // Built from the ERP's own alkanziFormPopup rather than a hand-rolled dxPopup + dxForm, so
    // this dialog is the same shell — and the same form-tab-panel-popup styling from
    // listPopup.css — as every other form in the system. The class owns the popup chrome, the
    // Save button and set(title, data); this controller supplies the fields and the handler.
    $scope.editor = { mode: "add", level: "organization", parentId: null, title: "" };

    $scope.editorPopup = new alkanziFormPopup({
        colCount: 1,
        showSubmit: true,
        saveBtnText: "Save",
        // Ties the dialog's title bar to the level being edited, the way the ERP pairs a
        // form's colour with the list it was opened from.
        toolbarColor: "primaryPopup"
    });

    $scope.editorPopup.popup.width = 460;
    $scope.editorPopup.popup.height = "auto";

    // The class exposes `submit` as an assignable hook rather than a callback in its config.
    $scope.editorPopup.submit = function () { $scope.saveEditor(); };

    function itemsFor(level) {
        var base = [
            { dataField: "code", label: { text: "Code" }, isRequired: true, editorOptions: { maxLength: 20 } },
            { dataField: "name", label: { text: "Name" }, isRequired: true, editorOptions: { maxLength: 200 } }
        ];
        if (level === "company") {
            base.push({ dataField: "currency", label: { text: "Currency" }, isRequired: true, editorOptions: { maxLength: 3 } });
        }
        base.push({ dataField: "isActive", label: { text: "Active" }, editorType: "dxCheckBox" });
        return base;
    }

    /// Opens the editor for a new child of the selected node, or a new organization.
    $scope.add = function (level, parentNode) {
        var cfg = LEVELS[level];
        $scope.editor.mode = "add";
        $scope.editor.level = level;
        $scope.editor.parentId = parentNode ? parentNode.entityId : null;
        $scope.editor.title = "New " + cfg.label + (parentNode ? " in " + parentNode.row.name : "");
        openEditor(cfg.blank(parentNode ? parentNode.entityId : null));
    };

    $scope.edit = function (node) {
        if (!node) return;
        var cfg = LEVELS[node.level];
        $scope.editor.mode = "edit";
        $scope.editor.level = node.level;
        $scope.editor.parentId = node.row[cfg.parentKey] || null;
        $scope.editor.title = "Edit " + cfg.label + " — " + node.row.name;
        openEditor(angular.copy(node.row));
    };

    function openEditor(data) {
        var popup = $scope.editorPopup;
        var items = itemsFor($scope.editor.level);

        // The dxForm inside the popup does not exist until the popup has rendered once, and
        // the class's set() goes straight to formInit.option(...) — so calling set() on a
        // never-opened dialog throws. Seed the config, open, then apply once the widget is up.
        popup.form.items = items;

        if (!popup.formInit) {
            popup.popupTitle($scope.editor.title);
            popup.showPopup();
            whenFormReady(function () {
                popup.formInit.option("items", items);
                popup.formInit.option("formData", data);
            });
            return;
        }

        popup.formInit.option("items", items);
        popup.set($scope.editor.title, data);   // assigns formData, sets the title and shows
    }

    /// Waits for the popup's form widget to come up. Bounded, so a dialog that never renders
    /// fails quietly rather than spinning.
    function whenFormReady(done) {
        var tries = 0;
        (function probe() {
            if ($scope.editorPopup.formInit) { done(); return; }
            if (++tries > 40) return;
            setTimeout(probe, 25);
        })();
    }

    $scope.saveEditor = function () {
        var form = $scope.editorPopup.formInit;
        if (form && !form.validate().isValid) return;

        var cfg = LEVELS[$scope.editor.level];
        var data = form ? form.option("formData") : {};

        // Re-attach the parent key: the form does not show it, and an edit must not silently
        // reparent the row.
        if (cfg.parentKey) data[cfg.parentKey] = $scope.editor.parentId;

        $scope.busy = true;

        var request = $scope.editor.mode === "add"
            ? erpApi.post(cfg.endpoint, data)
            : erpApi.put(cfg.endpoint + "/" + data.id, data);

        request
            .then(function () {
                DevExpress.ui.notify(cfg.label + " saved.", "success", 2500);
                $scope.editorPopup.hidePopup();
                $scope.selected = null;
                $scope.refresh();
            })
            .finally(function () { $scope.busy = false; });
    };

    $scope.remove = function (node) {
        if (!node) return;
        var cfg = LEVELS[node.level];

        var confirmed = DevExpress.ui.dialog.confirm(
            "Delete " + cfg.label.toLowerCase() + " <b>" + node.row.name + "</b>?<br/>" +
            "<span style='color:var(--erp-muted)'>It is hidden rather than erased, and can be restored in the database.</span>",
            "Delete " + cfg.label);

        confirmed.done(function (ok) {
            if (!ok) return;
            $scope.busy = true;
            erpApi.del(cfg.endpoint + "/" + node.entityId)
                .then(function () {
                    DevExpress.ui.notify(cfg.label + " deleted.", "success", 2500);
                    $scope.$applyAsync(function () {
                        $scope.selected = null;
                        $scope.refresh();
                    });
                })
                // A refusal ("this company still has branches") is already surfaced by erpApi,
                // so nothing to add here beyond clearing the busy state.
                .finally(function () { $scope.busy = false; });
        });
    };

    // ---------- refresh ----------
    $scope.refresh = function () {
        var instance = $scope.tree.instance;
        if (!instance) return;

        // Re-assigning createChildren is what clears the widget's cached children; repaint
        // alone redraws the same stale nodes.
        instance.option("createChildren", function (parent) {
            return $scope.tree.createChildren(parent);
        });
    };

    // ---------- toolbar for the detail pane ----------
    $scope.canAddChild = function () {
        return $scope.selected && LEVELS[$scope.selected.level].childLevel !== null;
    };

    $scope.childLabel = function () {
        if (!$scope.selected) return "";
        var child = LEVELS[$scope.selected.level].childLevel;
        return child ? LEVELS[child].label : "";
    };

    $scope.levelLabel = function (level) { return LEVELS[level] ? LEVELS[level].label : level; };
    $scope.levelIcon = function (level) { return LEVELS[level] ? "fa-solid " + LEVELS[level].icon : "fa-solid fa-circle"; };
    $scope.levelInk = function (level) { return LEVELS[level] ? "var(" + LEVELS[level].token + ")" : "var(--erp-muted)"; };
}]);
