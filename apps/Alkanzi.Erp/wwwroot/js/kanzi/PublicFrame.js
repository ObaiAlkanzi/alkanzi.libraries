
class frame {
    apiUr = '';
    data = [];
    UserDetail = {
        OrganizationId: sessionStorage.getItem("OrganizationId"),
        CompanyId: sessionStorage.getItem("UnitId"),
        UserId: sessionStorage.getItem("UserId"),
        EnableUserAuthorization: true,
    };
    labelMode = 'outside';
    dataGrid = {
        activeStateEnabled: true,
        focusStateEnabled: true,
        hoverStateEnabled: true,
        keyExpr: 'Id',
        showBorders: true,
        paging: {
            pageSize: 10,
        },
        pager: {
            visible: true,
            allowedPageSizes: [10, 30, 50, 'all'],
            showPageSizeSelector: true,
            showInfo: true,
            showNavigationButtons: true,
        },
        filterRow: { visible: true },
        headerFilter: { visible: true },
        allowColumnReordering: true,
        allowColumnResizing: true,
        searchPanel: {
            visible: true,
            width: 240,
            placeholder: 'Search...',
        },
        showBorders: true,
        showColumnLines: true,
        showRowLines: true,
        columnMinWidth: 50,
        columnAutoWidth: true,
        columnHidingEnabled: false,
        remoteOperations: true,
        filterRow: {
            visible: true,
        },
        editing: {
            mode: 'row',
            allowUpdating: false,
            allowAdding: false,
            allowDeleting: true,
            useIcons: true,
        },
    };
    serverErrorHandler(errorRes, header) {
        try {
            var msg = header + ' : ' + HttpServerRequestStatus.find(item => item.status == errorRes).message;
            this.showIndicator(msg, 'warning');
        } catch (e) {
            this.showIndicator('connection refused', 'warning');
        }
    };
    loadingVisible = false;
    loadOptions = {
        position: "center",
        shading: true,
        shadingColor: 'rgba(0,0,0,0.5)',
    }
    showIndicator(msg, status) {
        DevExpress.ui.notify({ message: msg, position: { my: 'center top', at: 'center top', }, }, status, 3000);
    };
    showPopup = false;
    popup = {};
    validation = {};
    flag = null;
    $http = null;
    constructor(detail, $http) {
        this.$http = $http;
        this.apiUrl = detail.apiUrl;
        this.flag = detail.title;
        this.popup = {
            minHeight: 500,
            height: 400,
            //elementAttr: true,
            hideOnOutsideClick: false,
            deferRendering: false,
            position: {
                at: 'center',
                my: 'center',
            },
            dragOutsideBoundary: true,
            resizeEnabled: true,
            restorePosition: true,
            shading: true,
            shadingColor: 'rgba(0,0,0,0.5)',
            showTitle: true,
            title: detail.title,
        }
        this.validation = {
            validationGroup: detail.validationGroup,
            validationRules: [{
                type: 'required',
                message: 'Is required',
            }],
        }
    }
    getData() {
        notification.LoadingMsg();
        var ef = this;
        this.$http.get(this.apiUrl).then((res) => {
            notification.RemoveNotification();
            ef.data = res.data;
        }, function (errorRes) {
            notification.RemoveNotification();
            ef.serverErrorHandler(errorRes.status, 'fetch data');
        });
    }
    IsValide(validationGroup) {
        var x = DevExpress.validationEngine.validateGroup(validationGroup);
        if (x.isValid) {
            return true;
        } else {
            this.showIndicator('Validation error', 'error');
            return false;
        }
    }
}
const _indicatorIcon = "/svg/rolling.svg";
const isValidate = function (vgName) {
    var x = DevExpress.validationEngine.validateGroup(vgName);
    if (x.isValid) {
        return true;
    } else {
        showIndicator('Validation error', 'error');
        return false;
    }
}
const editConfirm = function (title, msg, btnType, type = 'dialog')
{
    if (type === 'alert')
    {
        return DevExpress.ui.dialog.custom({
            title: title, width: '60%', messageHtml: "<b class='h4'>" + msg + "</b>",
            buttons: [{ text: "Ok",elementAttr: { class: cellColors.customSuccessBtn }, onClick: function (e) { return true; } }]
        });
    }
    return DevExpress.ui.dialog.custom({
        title: title, messageHtml: "<b class='h4'>" + msg + "</b>",
        buttons: [{ text: "Yes", type: btnType, onClick: function (e) { return true; } }, { text: "No", type: 'normal', onClick: function (e) { return false;}},]
    });
}
const showIndicator = function (msg, status ='success') {
    let top = window.innerHeight - 20;
    let lastOffset = $(".dx-toast-content").last().offset();
    if (lastOffset != null) {
        top = lastOffset.top;
    }
    if (top <= 0)
        top = 20;
    else {
        top = window.innerHeight - top;
        top += 5;
    }
    DevExpress.ui.notify({
        height: "auto",
        minWidth: 344,
        width: "auto",
        type: status,
        message: msg,
        position: {
            my: 'bottom right',
            at: 'bottom right',
            offset: '-10 -' + top,
        },
        displayTime: 1000,
    });
}
const _gridheight = 'auto';
const serverErrorHandler = function (errorRes, header) {
    try {
        var msg = header + ' : ' + HttpServerRequestStatus.find(item => item.status == errorRes).message;
        showIndicator(msg, 'warning');
    } catch (e) {
        showIndicator('connection refused', 'warning');
    }
};
class basicContainer {
    data = [];
    dataGrid = {
        //autoNavigateToFocusedRow: true,
        activeStateEnabled: true,
        focusStateEnabled: true,
        hoverStateEnabled: false,
        focusedRowEnabled: true,
        columnFixing: { enabled: true, },
        keyExpr: 'ID',
        showBorders: true,
        paging: {
            pageSize: 10,
        },
        pager: {
            visible: true,
            allowedPageSizes: [10, 30, 50, 'all'],
            showPageSizeSelector: true,
            showInfo: true,
            showNavigationButtons: true,
        },
        cacheEnabled: true,
        filterRow: { visible: true },
        headerFilter: { visible: true },
        allowColumnReordering: true,
        allowColumnResizing: true,
        searchPanel: {
            visible: true,
            width: 240,
            placeholder: 'Search...',
        },
        showColumnLines: true,
        showRowLines: true,
        selection: { mode: 'single', enabled: true },
        scrolling: {
            columnRenderingMode: "standard",
            mode: "standard",
            preloadEnabled: true,
            renderAsync: true,
            rowRenderingMode: "standard",
            scrollByContent: true,
            scrollByThumb: true,
            showScrollbar: "always",
            useNative: "auto"
        },
        columnMinWidth: 50,
        columnAutoWidth: true,
        columnHidingEnabled: false,
        //width: '100%',
        height: _gridheight,
        remoteOperations: false,
        editing: {
            mode: 'row',
            allowUpdating: false,
            allowAdding: false,
            allowDeleting: false,
            useIcons: true,
        },
        columnChooser: {
            enabled: true,
            mode: 'select',
        },
        onCellPrepared(e) {
            if (e.rowType === 'data') {
                let field = e.column.dataField;
                if (field === 'ID' || field === 'DOC_NUM') {
                    e.cellElement.addClass(cellColors.default);
                }
                else if (field === 'STATUS') {
                    statusPrepared(e);
                }
            }
        },
    };
    popup = {
        showCloseButton: true,
        focusStateEnabled: true,
        ////elementAttr: true,
        hideOnOutsideClick: false,
        deferRendering: false,
        position: 'center',
        dragOutsideBoundary: true,
        resizeEnabled: true,
        restorePosition: true,
        shading: false,
        shadingColor: 'rgba(0,0,0,0.5)',
        showTitle: true,
        title: 'Title',
        wrapperAttr: true,
        //copyRootClassesToWrapper: true,
        //width()
        //{
        //    if (_isLarge)
        //    {
        //        return 500;
        //    }
        //},
    }
    validation = {};
    submit() {

    };
    async clearDataSource() {
        return true;
    }
    constructor(ngName, saveBtn = false, saveBtnText = 'Save', btnClass = 'custom-close-btn') {
        if (ngName != null || ngName != undefined) {
            this.validation = {
                validationGroup: ngName,
                validationRules: [{
                    type: 'required',
                    message: 'Is required',
                }],
            }
        }
        var x = this.popup;
        var g = this.dataGrid;
        let th = this;
        //console.log(saveBtn,'saveBtn')
        x.onInitialized = function (e) {
            let compo = e.component;
            th.hidePopup = function () {
                compo.option('visible', false);
            }
            th.showPopup = function () {
                compo.option('visible', true);
            }
            th.popupTitle = function (title) {
                compo.option('title', title);
            };
            x.addToolbarItem = function (item) {
                let items = compo.option('toolbarItems');
                let last = items.length;
                compo.option(`toolbarItems[${last}]`, item);
            };
            compo.option('toolbarItems',
                [
                    {
                        location: "after",
                        toolbar: "top",
                        widget: 'dxButton',
                        options: {
                            icon: 'minus',
                            elementAttr: { class: "management-toolbar-btn" },
                            onClick(e) {
                                compo.hide();
                            }
                        }
                    },
                    {
                        location: "after",
                        toolbar: "top",
                        widget: 'dxButton',
                        options: {
                            //icon: 'fa-solid fa-expand',
                            icon: 'fullscreen',
                            elementAttr: { class: "management-toolbar-btn" },
                            onClick(e) {
                                compo.option('fullScreen', !compo.option('fullScreen'));
                            }
                        }
                    },
                    {
                        location: "after",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            icon: 'save',
                            text: saveBtnText,
                            //text: 'Save',
                            visible: saveBtn,
                            elementAttr: { class: 'custom-success-btn' },
                            //stylingMode: "outlined",
                            onInitialized(e) {
                                th._submitBtnInit = e.component;
                            },
                            onClick() {
                                th.submit();
                            }
                        }
                    },
                    {
                        location: "after",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            //visible: saveBtn,
                            text: 'Exit',
                            elementAttr: { class: btnClass },
                            onClick() {
                                compo.hide();
                            }
                        }
                    },
                ]);
        };

        //x.onShown = function (e) {
        //    setTimeout(() => {
        //        th.gridInit.focus();
        //    });
        //};
        g.onInitialized = function (e) {
            g.initializer = e.component;
            th.gridInit = e.component;
            let gridCompo = e.component;
            th.open = async function (data,title) {
                try {
                    //gridCompo.clearFilter();
                    //gridCompo.clearSelection();
                    gridCompo.option('dataSource', data);
                    if (title) {
                        th.popupTitle(title);
                    }
                    th.showPopup();
                    setTimeout(() => {
                        gridCompo.focus();
                        gridCompo.option(`focusedRowIndex`, 0);
                    },900);
                    return true;
                } catch (e) {
                    return false;
                }
            };
            th.clearDataSource = async function () {
                try {
                    gridCompo.clearFilter();
                    gridCompo.clearSelection();
                    gridCompo.option('dataSource', []);
                    return true;
                } catch (e) {
                    return false;
                }
            };
            th.setDataSource = function (data) {
                gridCompo.option('dataSource', data);
                //setTimeout(() => {
                //    gridCompo.focus();
                //    //gridCompo.option(`focusedRowIndex`, 0);
                //});
            };
            th.getDataSource = function () {
                return gridCompo.option('dataSource');
            }
            th.customAddRow = function (row) {
                var tmpStore = e.component.option('dataSource');
                tmpStore.push(row);
                gridCompo.option('dataSource', tmpStore);
            };
            th.customRepaintRow = function (rowKey, newRow) {
                var index = gridCompo.getRowIndexByKey(rowKey);
                var row = gridCompo.getVisibleRows()[index].data;
                Object.assign(row, newRow);
                gridCompo.repaintRows([index]);
                gridCompo.refresh().done(() => {
                    //console.log('refreshed')
                    //gridCompo.cancelEditData();
                });
            };
            th.getRow = async function (rowKey) {
                let index = gridCompo.getRowIndexByKey(rowKey);
                let row = gridCompo.getVisibleRows()[index].data;
                return row;
            };
            th.customDeleteRow = function (row) {
                gridCompo.option('dataSource', gridCompo.getDataSource().items().filter(item => item.ID !== row.ID));
            };
        };
        g.onSelectionChanged = function (e) {
            let row = e.selectedRowsData[0];
            th.selectedRow = row;
        };
    }
};

/* ============================================================
   listPopup — a popup that hosts a dxList (instead of basicContainer's
   dxGrid). Same popup chrome/toolbar (minimize / fullscreen / save / exit)
   and the same showPopup/hidePopup/popupTitle helpers, plus list helpers
   (setDataSource / getDataSource / reload / repaint / open).

   Usage (mirrors basicContainer):
       $scope.myVendors = new listPopup('myVendors');
       $scope.myVendors.popup.title = 'My Vendors';
       $scope.myVendors.list.dataSource = ...;        // override list options
       $scope.myVendors.list.itemTemplate = ...;
       // markup: <div dx-popup="myVendors.popup"><div dx-list="myVendors.list"></div></div>
       $scope.myVendors.open();                        // reload + repaint + show
   ============================================================ */
class listPopup {
    data = [];
    list = {
        keyExpr: 'ID',
        height: '100%',
        scrollingEnabled: true,
        selectionMode: 'none',
        activeStateEnabled: false,
        focusStateEnabled: false,
        hoverStateEnabled: true,
        searchEnabled: false,
        pageLoadMode: 'scrollBottom',
        noDataText: 'No records found.',
    };
    popup = {
        showCloseButton: true,
        focusStateEnabled: true,
        hideOnOutsideClick: false,
        deferRendering: false,
        position: 'center',
        dragOutsideBoundary: true,
        resizeEnabled: true,
        restorePosition: true,
        shading: false,
        shadingColor: 'rgba(0,0,0,0.5)',
        showTitle: true,
        title: 'Title',
        // Popup renders its markup into an overlay on <body>, so elementAttr only
        // tags the original hidden element — DevExtreme deprecated it for overlays
        // in 21.2. wrapperAttr is what lands on the visible .dx-overlay-wrapper.
        wrapperAttr: { class: 'kz-list-popup' },
        width: 560,
    };
    validation = {};
    submit() { };
    constructor(ngName, saveBtn = false, saveBtnText = 'Save', btnClass = alkanziColors.softGray) {
        if (ngName != null || ngName != undefined) {
            this.validation = {
                validationGroup: ngName,
                validationRules: [{
                    type: 'required',
                    message: 'Is required',
                }],
            }
        }
        var x = this.popup;
        var l = this.list;
        let th = this;

        x.onInitialized = function (e) {
            let compo = e.component;
            th.hidePopup = function () {
                compo.option('visible', false);
            };
            th.showPopup = function () {
                compo.option('visible', true);
            };
            th.popupTitle = function (title) {
                compo.option('title', title);
            };
            x.addToolbarItem = function (item) {
                let items = compo.option('toolbarItems');
                let last = items.length;
                compo.option(`toolbarItems[${last}]`, item);
            };
            compo.option('toolbarItems',
                [
                    {
                        location: "after",
                        toolbar: "top",
                        widget: 'dxButton',
                        options: {
                            icon: 'minus',
                            elementAttr: { class: "management-toolbar-btn" },
                            onClick(e) {
                                compo.hide();
                            }
                        }
                    },
                    {
                        location: "after",
                        toolbar: "top",
                        widget: 'dxButton',
                        options: {
                            icon: 'fullscreen',
                            elementAttr: { class: "management-toolbar-btn" },
                            onClick(e) {
                                compo.option('fullScreen', !compo.option('fullScreen'));
                            }
                        }
                    },
                    {
                        location: "after",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            icon: 'save',
                            text: saveBtnText,
                            visible: saveBtn,
                            elementAttr: { class: 'custom-success-btn' },
                            onInitialized(e) {
                                th._submitBtnInit = e.component;
                            },
                            onClick() {
                                th.submit();
                            }
                        }
                    },
                    // {
                    //     location: "after",
                    //     toolbar: "bottom",
                    //     widget: 'dxButton',
                    //     options: {
                    //         text: 'Exit',
                    //         elementAttr: { class: btnClass },
                    //         onClick() {
                    //             compo.hide();
                    //         }
                    //     }
                    // },
                ]);
        };

        l.onInitialized = function (e) {
            l.initializer = e.component;
            th.listInit = e.component;
            let listCompo = e.component;
            // Persist a background highlight on the last-clicked item. mousedown is
            // used (not click) so it still fires when an in-row button stops the
            // click, and the highlight survives while a popup opens over the list.
            let $listEl = $(e.element);
            $listEl.on('mousedown', '.dx-list-item', function () {
                $listEl.find('.dx-list-item.pw-active-item').removeClass('pw-active-item');
                $(this).addClass('pw-active-item');
            });
            th.setDataSource = function (data) {
                listCompo.option('dataSource', data);
            };
            th.getDataSource = function () {
                return listCompo.getDataSource();
            };
            // Re-fetch from the underlying store (no-op for plain-array sources).
            th.reload = function () {
                var ds = listCompo.getDataSource && listCompo.getDataSource();
                if (ds && ds.reload) ds.reload();
            };
            th.repaint = function () {
                listCompo.repaint();
            };
            // Reload + repaint + show — rows re-animate on every open.
            th.open = function (title) {
                th.reload();
                listCompo.repaint();
                if (title) th.popupTitle(title);
                th.showPopup();
            };
        };
    }
};

class management {
    treeListInit = null;
    popupInit = 1;
    showPopup = false;
    constructor() {
        var ei = this;
        this.popup.onInitialized = function (e) {
            ei.popupInit = e.component;
        }
    };
    popup = {
        showCloseButton: true,
        //elementAttr: true,
        hideOnOutsideClick: false,
        deferRendering: false,
        position: 'center',
        dragOutsideBoundary: true,
        resizeEnabled: true,
        restorePosition: true,
        fullScreen: true,
        shading: true,
        shadingColor: 'rgba(0,0,0,0.5)',
        showTitle: true,
        toolbarItems: [{
            location: 'before',
            template(e) {
                if (e.text != undefined || e.text != null) {
                    return '<span class="dx-dashboard-ellipsis">' + e.text + '</span>';
                } else {
                    return '';
                }

            },
        },
        ]
    };
};
class submitForm {
    docName = null;
    userId = null;
    popupInit = 1;
    fromLevelInit = 0;
    toLevelInit = 0;
    statusBoxInit = 0;
    popup = {
        showCloseButton: true,
        //elementAttr: true,
        hideOnOutsideClick: false,
        deferRendering: false,
        position: 'center',
        dragOutsideBoundary: true,
        resizeEnabled: true,
        restorePosition: true,
        fullScreen: true,
        shading: false,
        shadingColor: 'rgba(0,0,0,0.5)',
        showTitle: true,
        toolbarItems: [{
            location: 'before',
            template(e) {
                return '<span class="dx-dashboard-ellipsis">' + e.text + '</span>';
            },
        },
        ]
    };
    validation = {};
    constructor(ngName, user, docName) {
        this.docName = docName;
        this.userId = user;
        if (ngName != null || ngName != undefined) {
            this.validation = {
                validationGroup: ngName,
                validationRules: [{
                    type: 'required',
                    message: 'Is required',
                }],
            }
        }
        var ei = this;
        this.popup.onInitialized = function (e) {
            ei.popupInit = e.component;
        };
        this.fromLevel.onInitialized = function (e) {
            ei.fromLevelInit = e.component;
        };
        this.toLevel.onInitialized = function (e) {
            ei.toLevelInit = e.component;
        };
        this.statusBox.onInitialized = function (e) {
            ei.statusBoxInit = e.component;
        };
        this.commentBox.onInitialized = function (e) {
            ei.commentBoxInit = e.component;
        };
    }
    fromLevel = {
        readOnly: true,
    };
    toLevel = {
        displayExpr: 'NAME',
        valueExpr: 'ID',
    };
    statusBox = {
        displayExpr: 'NAME',
        valueExpr: 'ID',
        itemTemplate(data, index, element) {
            return "<span>" + data.NAME + " <span class='" + data.icon + "'></span></span>";
        },
    };
    commentBox = {
        height: 80,
        showClearButton: true,
    };
    commentDxList = {
        allowItemDeleting: false,
        activeStateEnabled: false,
        focusStateEnabled: false,
        selectionMode: "single",
        itemDeleteMode: 'swipe',
        menuMode: "context",
        searchMode: "contains",
        scrollByContent: true,
        scrollByThumb: true,
        scrollingEnabled: true,
        nextButtonText: "More",
        height: 400,
    };
    setStatus(id) {
        var tmp = [];
        if (id == 1) {
            tmp.push({ ID: '1', NAME: "Submit", icon: 'fa fa-check' });
        } else if (id > 1) {
            tmp.push(
                { ID: '1', NAME: "Submit", icon: 'fa fa-check' },
                { ID: '2', NAME: "Rework", icon: 'fa fa-refresh' },
                { ID: '3', NAME: "Reject", icon: 'fa fa-times' },
            );
        }
        return tmp;
    }
    setLevels(data, status) {
        var id = data.LEVEL_ID;
        var nexLevel = parseInt(id + 1);
        var tmp = [];
        switch (parseInt(status)) {
            case 1:
                if (data.LEVEL_ID < data.LAST_LEVEL) {
                    tmp.push({ ID: nexLevel, NAME: "Next Level ", icon: 'fa fa-check' });
                } else if (data.LEVEL_ID == data.LAST_LEVEL) {
                    tmp.push({ ID: nexLevel - 1, NAME: "Approve Level", icon: 'fa fa-check' });
                }
                break;
            case 2:

                for (var i = id - 1; i > 0; i--) {
                    tmp.push({ ID: i - 1, NAME: "Level " + i, icon: 'fa fa-chevron-right' });
                }
                break;
            case 3:
                tmp.push({ ID: 0, NAME: "No Level", icon: 'fa fa-check' });
                break;
        }
        return tmp;
    };
    processRoom(transLv, useLv) {
        //console.log(transLv, useLv);
        this.docData.LEVEL_ID = transLv;
        if (transLv < useLv) {
            if (transLv == 0) {
                transLv = transLv + 1;
            } else {
                transLv = useLv;
            }
            this.docData.LEVEL_ID = transLv;
            this.statusList = this.setStatus(transLv);
            return transLv;
        }
        return 0;
    }
};
class comment {
    popupInit = 1;
    commentBoxInit = 1;
    popup = {
        showCloseButton: true,
        //elementAttr: true,
        hideOnOutsideClick: false,
        deferRendering: false,
        position: 'center',
        dragOutsideBoundary: true,
        resizeEnabled: true,
        restorePosition: true,
        fullScreen: true,
        shading: true,
        shadingColor: 'rgba(0,0,0,0.5)',
        showTitle: true,
        toolbarItems: [{
            location: 'before',
            template(e) {
                return '<span class="dx-dashboard-ellipsis">' + e.text + '</span>';
            },
        },
        ]
    };
    validation = {};
    constructor(ngName) {
        if (ngName != null || ngName != undefined) {
            this.validation = {
                validationGroup: ngName,
                validationRules: [{
                    type: 'required',
                    message: 'Is required',
                }],
            }
        }
        var ei = this;
        this.popup.onInitialized = function (e) {
            ei.popupInit = e.component;
        };
        this.commentBox.onInitialized = function (e) {
            ei.commentBoxInit = e.component;
        };
    };
    commentBox = {
        height: 80,
        showClearButton: true,
    };
    commentDxList = {
        allowItemDeleting: false,
        activeStateEnabled: false,
        focusStateEnabled: false,
        itemDeleteMode: 'swipe',
        menuMode: "context",
        selectionMode: "single",
        searchMode: "contains",
        scrollByContent: true,
        scrollByThumb: true,
        scrollingEnabled: true,
        nextButtonText: "More",
        height: 'auto',
        noDataText: "No Comments",
    };
    transCommentList = {
        allowItemDeleting: false,
        activeStateEnabled: false,
        focusStateEnabled: false,
        itemDeleteMode: 'swipe',
        menuMode: "context",
        selectionMode: "single",
        searchMode: "contains",
        scrollByContent: true,
        scrollByThumb: true,
        scrollingEnabled: true,
        nextButtonText: "More",
        height: 'auto',
        noDataText: "No Remark",
    };
    approvalCommentList = {
        allowItemDeleting: false,
        activeStateEnabled: false,
        focusStateEnabled: false,
        itemDeleteMode: 'swipe',
        menuMode: "context",
        selectionMode: "single",
        searchMode: "contains",
        scrollByContent: true,
        scrollByThumb: true,
        scrollingEnabled: true,
        nextButtonText: "More",
        height: 'auto',
        noDataText: "No Approval Comments",
    };
};
class fetchDocument {
    popupInit = 1;
    commentBoxInit = 0;
    fileLoaderInit = 0;
    validation = {};
    constructor(ngName) {
        if (ngName != null || ngName != undefined) {
            this.validation = {
                validationGroup: ngName,
                validationRules: [{
                    type: 'required',
                    message: 'Is required',
                }],
            }
        }
        var ei = this;
        this.popup.onInitialized = function (e) {
            ei.popupInit = e.component;
        };
        this.commentBox.onInitialized = function (e) {
            ei.commentBoxInit = e.component;
        };
        this.fileLoader.onInitialized = function (e) {
            ei.fileLoaderInit = e.component;
        };
        this.docType.onInitialized = function (e) {
            ei.docTypeInit = e.component;
        };
    };
    docType = {
        displayExpr: 'NAME',
        valueExpr: 'ID',
        placeholder: 'Document Type',
    }
    popup = {
        showCloseButton: true,
        //elementAttr: true,
        hideOnOutsideClick: false,
        deferRendering: false,
        position: 'center',
        dragOutsideBoundary: true,
        resizeEnabled: true,
        restorePosition: true,
        fullScreen: true,
        shading: true,
        shadingColor: 'rgba(0,0,0,0.5)',
        showTitle: true,
        toolbarItems: [{
            location: 'before',
            template(e) {
                return '<span class="dx-dashboard-ellipsis">' + e.text + '</span>';
            },
        },
        ]
    };
    commentBox = {
        height: 60,
        showClearButton: true,
    };
    fileLoader = {
        hoverStateEnabled: true,
        focusStateEnabled: true,
        activeStateEnabled: true,
        selectButtonText: 'Select Document',
        labelText: '',
        accept: '*',
        uploadMode: 'useForm',
        maxFileSize: 15728640,
        minFileSize: 1,
    }
    dataGrid = {
        height: 400,
        activeStateEnabled: true,
        focusStateEnabled: true,
        hoverStateEnabled: true,
        keyExpr: 'ID',
        showBorders: true,
        paging: {
            pageSize: 10,
        },
        pager: {
            visible: true,
            allowedPageSizes: [10, 30, 50, 'all'],
            showPageSizeSelector: true,
            showInfo: true,
            showNavigationButtons: true,
        },
        filterRow: { visible: true },
        headerFilter: { visible: true },
        allowColumnReordering: true,
        allowColumnResizing: true,
        searchPanel: {
            visible: true,
            width: 240,
            placeholder: 'Search...',
        },
        showColumnLines: false,
        showRowLines: true,
        columnMinWidth: 50,
        columnAutoWidth: true,
        columnHidingEnabled: false,
        remoteOperations: true,
        filterRow: {
            visible: true,
        },
        editing: {
            mode: 'row',
            allowUpdating: false,
            allowAdding: false,
            allowDeleting: false,
            useIcons: true,
        },
        columnChooser: {
            enabled: true,
            mode: 'select',
        },
        columns: [
            {
                dataField: 'ID',
                dataType: 'number',
                allowEditing: false,
                width: 80,
                alignment: 'center',
                visible: false,
            },
            {
                dataField: 'SNUMBER',
                caption: 'S.N',
                allowEditing: false,
                sortOrder: "desc",
                width: 100,
                alignment: 'center',
                //visible: false,
            },
            {
                dataField: 'REMARKS',
            },
            {
                dataField: 'DOC_TYPE_NAME',
                caption: 'DOC TYPE',
            },
            {
                dataField: 'UNIQUE_NAME',
                caption: 'Download',
                width: 120,
                cellTemplate(container, options) {
                    $(container).append('<a href="/TrnDocuments/' + options.row.data.UNIQUE_NAME + '"  target="_blank" download><span class="glyphicon glyphicon-download-alt"></span></a>');
                },
                alignment: 'center',
            },
            {
                dataField: 'UNIQUE_NAME',
                caption: 'Open',
                width: 120,
                cellTemplate(container, options) {
                    $(container).append('<a href="/TrnDocuments/' + options.row.data.UNIQUE_NAME + '"  target="_blank" ><span class="dx-icon-folder"></span></a>');
                },
                alignment: 'center',
            },
            {
                dataField: 'CREATED_AT',
                caption: 'Date',
                dataType: 'date',
                format: 'dd-MM-yyyy',
                alignment: 'center',
                width: 200,
            },
        ],
    };
};
class report {
    docName = null;
    userId = null;
    data = [];
    constructor(docName, user) {
        this.docName = docName;
        this.userId = user;
    }
    filter(filter, key, value) {
        var pointer = 0;
        var tail = 0;
        while (pointer > - 1) {
            pointer = filter.indexOf('%' + key + '%', tail);
            if (pointer > -1) {
                filter = filter.replace('%' + key + '%', value);
            }
            tail = pointer + 1;
        }
        return filter;
    }
};
class ownerFetchDocument {
    data = [];
    popupInit = 1;
    commentBoxInit = 0;
    fileLoaderInit = 0;
    validation = {};
    constructor(ngName) {
        if (ngName != null || ngName != undefined) {
            this.validation = {
                validationGroup: ngName,
                validationRules: [{
                    type: 'required',
                    message: 'Is required',
                }],
            }
        }
        var ei = this;
        this.dataGrid.onInitialized = function (e) {
            ei.dataGridInit = e.component;
        };
        this.popup.onInitialized = function (e) {
            ei.popupInit = e.component;
        };
        this.commentBox.onInitialized = function (e) {
            ei.commentBoxInit = e.component;
        };
        this.fileLoader.onInitialized = function (e) {
            ei.fileLoaderInit = e.component;
        };
    };
    popup = {
        showCloseButton: true,
        //elementAttr: true,
        hideOnOutsideClick: false,
        deferRendering: false,
        position: 'center',
        dragOutsideBoundary: true,
        resizeEnabled: true,
        restorePosition: true,
        fullScreen: true,
        shading: true,
        shadingColor: 'rgba(0,0,0,0.5)',
        showTitle: true,
        toolbarItems: [{
            location: 'before',
            template(e) {
                return '<span class="dx-dashboard-ellipsis">' + e.text + '</span>';
            },
        },
        ]
    };
    commentBox = {
        height: 60,
        showClearButton: true,
    };
    fileLoader = {
        hoverStateEnabled: true,
        focusStateEnabled: true,
        activeStateEnabled: true,
        selectButtonText: 'Select Document',
        labelText: '',
        accept: '*',
        uploadMode: 'useForm',
        maxFileSize: 15728640,
        minFileSize: 1,
    }
    dataGrid = {
        height: 400,
        activeStateEnabled: true,
        focusStateEnabled: true,
        hoverStateEnabled: true,
        keyExpr: 'ID',
        showBorders: true,
        paging: {
            pageSize: 10,
        },
        pager: {
            visible: true,
            allowedPageSizes: [10, 30, 50, 'all'],
            showPageSizeSelector: true,
            showInfo: true,
            showNavigationButtons: true,
        },
        filterRow: { visible: true },
        headerFilter: { visible: true },
        allowColumnReordering: true,
        allowColumnResizing: true,
        searchPanel: {
            visible: true,
            width: 240,
            placeholder: 'Search...',
        },
        showColumnLines: false,
        showRowLines: true,
        columnMinWidth: 50,
        columnAutoWidth: true,
        columnHidingEnabled: false,
        remoteOperations: true,
        filterRow: {
            visible: true,
        },
        editing: {
            mode: 'row',
            allowUpdating: true,
            allowAdding: false,
            allowDeleting: false,
            useIcons: true,
        },
        columnChooser: {
            enabled: true,
            mode: 'select',
        },
        columns: [
            {
                dataField: 'ID',
                dataType: 'number',
                allowEditing: false,
                sortOrder: "desc",
                width: 80,
                alignment: 'center',
                visible: false,
            },
            {
                dataField: 'REMARKS',
                allowEditing: false,
            },
            {
                dataField: 'FILE_NAME',
                caption: 'Download',
                width: 120,
                cellTemplate(container, options) {
                    $(container).append('<a href="/upload/owners_docs/' + options.row.data.FILE_NAME + '"  target="_blank" download><span class="glyphicon glyphicon-download-alt"></span></a>');
                },
                alignment: 'center',
                allowEditing: false,
            },
            {
                dataField: 'FILE_NAME',
                caption: 'Open',
                width: 120,
                cellTemplate(container, options) {
                    $(container).append('<a href="/upload/owners_docs/' + options.row.data.FILE_NAME + '"  target="_blank" ><span class="dx-icon-folder"></span></a>');
                },
                alignment: 'center',
                allowEditing: false,
            },
            {
                dataField: 'IS_ACTIVE',
                dataType: 'boolean',
                alignment: 'center',
            },
            {
                dataField: 'EXPIRE_DATE',
                caption: 'Date',
                dataType: 'date',
                format: _dateFormat,
                alignment: 'center',
                width: 200,
                allowEditing: false,
            },
            {
                dataField: 'CREATED_AT',
                caption: 'Date',
                dataType: 'date',
                format: _dateFormat,
                alignment: 'center',
                width: 200,
                allowEditing: false,
            },
        ],
    };
};
class singleGrid {
    dataGrid = {
        dataSource: [],
        focusedRowEnabled: true,/* DO NOT REMOVE */
        keyExpr: 'ID',
        showBorders: true,
        paging: {
            pageSize: 10,
        },
        pager: {
            visible: true,
            allowedPageSizes: [10, 30, 50, 'all'],
            showPageSizeSelector: true,
            showInfo: true,
            showNavigationButtons: true,
        },
        filterRow: { visible: true },
        headerFilter: { visible: true },
        allowColumnReordering: true,
        allowColumnResizing: true,
        paging: {
            pageSize: 10,
        },
        pager: {
            visible: true,
            allowedPageSizes: [10, 30, 50, 'all'],
            showPageSizeSelector: true,
            showInfo: true,
            showNavigationButtons: true,
        },
        cacheEnabled: true,
        searchPanel: {
            visible: true,
            width: 240,
            placeholder: 'Search...',
        },
        selection: { mode: 'single', enabled: true },
        showColumnLines: true,
        showRowLines: true,
        scrolling: {
            columnRenderingMode: "standard",
            mode: "standard",
            preloadEnabled: true,
            renderAsync: true,
            rowRenderingMode: "standard",
            scrollByContent: true,
            scrollByThumb: true,
            showScrollbar: "always",
            useNative: "auto"
        },
        columnMinWidth: 50,
        columnAutoWidth: true,
        columnHidingEnabled: false,
        columnResizingMode : "widget",
        width: '100%',
        height: _gridheight,
        //remoteOperations: false,
        editing: {
            mode: 'row',
            allowUpdating: false,
            allowAdding: false,
            allowDeleting: false,
            useIcons: true,
        },
        columnChooser: {
            enabled: true,
            mode: 'select',
        },
        //cacheEnabled: true,
        onCellPrepared(e) {
            if (e.rowType === 'data')
            {
                let column = e.column;
                let field = column.dataField;
                if (field === 'ID') {
                    e.cellElement.addClass(cellColors.default);
                }
                if (column.type === 'buttons')
                {
                    e.cellElement.addClass(alkanziColors.darkNormal);
                }
            }
        },
    };
    async clearDataSource() {
        return true;
    };
    getDataSource() {
        return [];
    }
    constructor() {
        var x = this;
        let grid = this.dataGrid;
        grid.onInitialized = function (e) {
            x.gridInit = e.component;
            let gridCompo = e.component;
            x.loading = function (status) {

            }
            x.clearDataSource = async function () {
                try {
                    gridCompo.clearFilter();
                    gridCompo.clearSelection();
                    gridCompo.option('dataSource', []);
                    return true;
                } catch (e) {
                    return false;
                }
            };
            x.setDataSource = function (data) {
                gridCompo.option('dataSource', data);
                //setTimeout(() => {
                //    gridCompo.focus();
                //    //gridCompo.option(`focusedRowIndex`, 0);
                //},500);
            }
            x.getDataSource = function () {
                return gridCompo.option('dataSource');
            }
            x.customAddRow = function (row) {
                var tmpStore = gridCompo.option('dataSource');
                tmpStore.push(row);
                gridCompo.option('dataSource', tmpStore);
            };
            x.customRepaintRow = function (rowKey, newRow) {
                var index = gridCompo.getRowIndexByKey(rowKey);
                var row = gridCompo.getVisibleRows()[index].data;
                Object.assign(row, newRow);
                gridCompo.repaintRows([index]);
                gridCompo.refresh().done(() => {
                    //console.log('refreshed')
                    //gridCompo.cancelEditData();
                });

            };
            x.customDeleteRow = function (row) {
                gridCompo.option('dataSource', e.component.getDataSource().items().filter(item => item.ID !== row.ID));
            };
            x.customStartEdit = function (rowKey) {
                let index = gridCompo.getRowIndexByKey(rowKey);
                gridCompo.editRow(index);
            };
            x.getRow = async function (rowKey) {
                let index = gridCompo.getRowIndexByKey(rowKey);
                let row = gridCompo.getVisibleRows()[index].data;
                return row;
            };
        };
        grid.onSelectionChanged = function (e) {
            let row = e.selectedRowsData[0];
            x.selectedRow = row;
        };
    }
}
class signlePopup {
    popup = {
        showCloseButton: true,
        //elementAttr: true,
        //copyRootClassesToWrapper: true,
        wrapperAttr: true,
        hideOnOutsideClick: false,
        deferRendering: false,
        position: 'center',
        dragOutsideBoundary: true,
        resizeEnabled: true,
        restorePosition: true,
        shading: false,
        shadingColor: 'rgba(0,0,0,0.5)',
        showTitle: true,
        title: 'Title',
        //width: 'auto',
    };
    constructor(saveBtn = false, build = true, submitBtnText = 'Save') {
        var x = this.popup;
        let th = this;
        x.onInitialized = function (e) {
            let compo = e.component;
            th.MaxMinScreen = function ()
            {
                compo.option('fullScreen', !compo.option('fullScreen'));
            };
            th.showPopup = function ()
            {
                compo.show();
            };
            th.hidePopup = function () {
                compo.hide();
            };
            th.popupTitle = function (title) {
                compo.option('title', title);
            };
            th.setTitle = function (title) {
                compo.option('title', title);
            };
            x.show = function () {
                compo.show();
            };
            x.hide = function () {
                compo.hide();
            };
            x.addToolbarItem = function (item, location, position) {
                let items = compo.option('toolbarItems');
            };
            if (build)
                compo.option('toolbarItems', [
                    {
                        location: "after",
                        toolbar: "top",
                        widget: 'dxButton',
                        options: {
                            visible: build,
                            elementAttr: { class: "management-toolbar-btn" },
                            onClick(e) {
                                compo.hide();
                            }
                        }
                    },
                    {
                        location: "after",
                        toolbar: "top",
                        widget: 'dxButton',
                        options: {
                            visible: build,
                            icon: 'fullscreen',
                            elementAttr: { class: "management-toolbar-btn" },
                            onClick(e) {
                                compo.option('fullScreen', !compo.option('fullScreen'));
                            }
                        }
                    },
                    {
                        location: "before",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            icon: 'save',
                            text: submitBtnText,
                            visible: saveBtn,
                            elementAttr: { class: alkanziColors.darkNormal },
                            onClick() {
                                th.submit();
                            }
                        }
                    },
                    {
                        location: "before",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            text: 'Discard',
                            visible: false,
                            //visible: saveBtn,
                            elementAttr: { class: 'custom-close-btn' },
                            onClick() {
                                compo.hide();
                            }
                        }
                    },
                ]);
        }
    }
}
class singleForm {
    form = {
        focusStateEnabled: true,
        scrollingEnabled: true,
        showRequiredMark: true,
        formData: {},
        labelMode: "floating",
        labelLocation: 'top',
    };
    constructor(colCount) {
        this.form.colCount = colCount;
        let th = this;
        th.form.onInitialized = function (e) {
            let comp = e.component;
            th.initializer = comp;
            th.getData = function ()
            {
                return comp.option('formData');
            };
            th.setData = function (data)
            {
                comp.option('formData', data);
            };
            th.optionOf = function (name, option, data = 'onlyShow')
            {
                if (data === 'onlyShow' || data == null)
                {
                    return comp.getEditor(name).option(option);
                } else
                {
                    comp.getEditor(name).option(option, data);
                }
            };
        }
    }
}
class loading {
    indicator = {
        position: "center",
        shading: true,
        shadingColor: 'rgba(0,0,0,0.5)',
        indicatorSrc: _indicatorIcon
    };
    show = function () {
        this.indicator.visible = true;
    };
    constructor() {
        var x = this;
        this.indicator.onInitialized = function (e) {
            x.show = function () {
                e.component.option('visible', true);
            };
            x.hide = function () {
                e.component.option('visible', false);
            };
        };

    }
};
class unitHistory {
    data = [];
    dataGrid = {
        autoNavigateToFocusedRow: true,
        activeStateEnabled: true,
        focusStateEnabled: true,
        hoverStateEnabled: true,
        focusedRowEnabled: true,
        keyExpr: 'ID',
        noDataText: 'No History',
        showBorders: true,
        paging: {
            pageSize: 10,
        },
        pager: {
            visible: true,
            allowedPageSizes: [10, 30, 50, 'all'],
            showPageSizeSelector: true,
            showInfo: true,
            showNavigationButtons: true,
        },
        filterRow: { visible: true },
        headerFilter: { visible: true },
        allowColumnReordering: true,
        allowColumnResizing: true,
        searchPanel: {
            visible: true,
            width: 240,
            placeholder: 'Search...',
        },
        showColumnLines: true,
        showRowLines: true,
        scrolling: {
            columnRenderingMode: "standard",
            mode: "standard",
            preloadEnabled: true,
            renderAsync: true,
            rowRenderingMode: "standard",
            scrollByContent: true,
            scrollByThumb: true,
            showScrollbar: "always",
            useNative: "auto"
        },
        columnMinWidth: 50,
        columnAutoWidth: true,
        columnHidingEnabled: false,
        width: '100%',
        remoteOperations: false,
        editing: {
            mode: 'row',
            allowUpdating: false,
            allowAdding: false,
            allowDeleting: false,
            useIcons: true,
        },
        columnChooser: {
            enabled: false,
            mode: 'select',
        },
        onCellPrepared(e) {
            let field = e.column.dataField;
            if (e.rowType === 'data') {
                if (field === 'ID') {
                    e.cellElement.addClass('default-cell');
                }
                else if (field === 'RESERVATION_ID') {
                    e.cellElement.addClass('normal-cell');
                }
                else if (field === 'STATUS') {
                    //statusPrepared(e);
                } else if (field === 'LEASE_STATUS') {
                    let val = e.data.LEASE_STATUS_ID;
                    switch (val) {
                        case 1:
                            e.cellElement.addClass('normal-cell');
                            break;
                        case 2:
                            e.cellElement.addClass('lemon-normal-cell');
                            break;
                        case 3:
                            e.cellElement.addClass('danger-cell');
                            break;
                        case 4:
                            e.cellElement.addClass('default-cell');
                            break;
                        case 5:
                            e.cellElement.addClass('orange-cell');
                            break;;
                        case 7:
                            e.cellElement.addClass('light-success-cell');
                            break;
                        case 8:
                            e.cellElement.addClass('danger-cell');
                            break;
                        case 9:
                            break;
                        case 10:
                            e.cellElement.addClass('warning-cell');
                            break;
                        default:
                            break;
                    }
                }


            }
        },
        groupPanel: {
            visible: true,
        },
        columns: [
            {
                dataField: 'ID',
                dataType: 'number',
                //visible: false,
                sortOrder: "asc",
                width: 100,
                alignment: 'center',
                fixed: true,
                fixedPosition: "left",
            },
            {
                dataField: 'RESERVATION_ID',
                caption: 'LEASE ID',
                width: 150,
                alignment: 'center',
                groupIndex: 0,
                fixed: true,
                fixedPosition: "left",
            },
            {
                dataField: 'HISTORY_DOC',
                alignment: 'center',
            },
            {
                dataField: 'DOC_TYPE',
                alignment: 'center',
                width: 170,
            },
            {
                dataField: 'DOC_NUM',
                alignment: 'center',
                width: 150,
            },
            {
                dataField: 'CUSTOMER',
            },
            {
                dataField: 'AMOUNT',
                width: 150,
                alignment: 'center',
                format: _decFormat,
            },
            {
                dataField: 'LEASE_STATUS',
                alignment: 'center',
                width: 140,
            },
            {
                dataField: 'STATUS',
                alignment: 'center',
                width: 140,
            },
            {
                dataField: 'START_DATE',
                dataType: 'date',
                format: _dateFormat,
                alignment: 'center',
            },
            {
                dataField: 'END_DATE',
                dataType: 'date',
                format: _dateFormat,
                alignment: 'center',
            },
            {
                dataField: 'CREATED_BY',
            },
            {
                dataField: 'HISTORY_DATE',
                dataType: 'date',
                format: _dateFormat,
                alignment: 'center',
                fixed: true,
                fixedPosition: "right",
            },
            {
                dataField: 'STATUS_REMARKS',
                alignment: 'center',
                width: 140,
                fixed: true,
                fixedPosition: "right",
            },
        ],

    };
    popup = {
        showCloseButton: true,
        focusStateEnabled: true,
        //elementAttr: true,
        hideOnOutsideClick: false,
        deferRendering: false,
        position: 'center',
        dragOutsideBoundary: true,
        resizeEnabled: true,
        restorePosition: true,
        shading: true,
        shadingColor: 'rgba(0,0,0,0.5)',
        showTitle: true,
    }
    validation = {};
    constructor() {
        var x = this.popup;
        var g = this.dataGrid;
        let th = this;
        x.onInitialized = function (e) {
            let compo = e.component;
            th.hidePopup = function () {
                compo.option('visible', false);
            }
            th.showPopup = function () {
                compo.option('visible', true);
            }
            th.popupTitle = function (title) {
                compo.option('title', title);
            };
            compo.option('toolbarItems',
                [
                    {
                        location: "after",
                        toolbar: "top",
                        widget: 'dxButton',
                        options: {
                            icon: 'fa-solid fa-expand',
                            elementAttr: { class: "management-toolbar-btn" },
                            onClick(e) {
                                compo.option('fullScreen', !compo.option('fullScreen'));
                            }
                        }
                    },
                    {
                        location: "after",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            icon: 'close',
                            text: 'Exit',
                            type: 'danger',
                            //stylingMode: "outlined",
                            onClick() {
                                compo.option('visible', false);
                            }
                        }
                    },]);
        };
        g.onInitialized = function (e) {
            g.initializer = e.component;
            th.gridInit = e.component;
            let gridCompo = e.component;
            th.clearDataSource = function () {
                gridCompo.option('dataSource', []);
            };
            th.setDataSource = function (data) {
                gridCompo.option('dataSource', data);
            };
            th.getDataSource = function () {
                return gridCompo.option('dataSource');
            }
            th.customAddRow = function (row) {
                var tmpStore = e.component.option('dataSource');
                tmpStore.push(row);
                gridCompo.option('dataSource', tmpStore);
            };
            th.customRepaintRow = function (rowKey, newRow) {
                var index = gridCompo.getRowIndexByKey(rowKey);
                var row = gridCompo.getVisibleRows()[index].data;
                Object.assign(row, newRow);
                gridCompo.repaintRows([index]);
            };
            th.customDeleteRow = function (row) {
                gridCompo.option('dataSource', gridCompo.getDataSource().items().filter(item => item.ID !== row.ID));
            };
        };

    }
};
//#region Alkanzi Preview Popup
let alkanziPreview;
const _openPreview = (title, pdfUrl) =>
{
    alkanziPreview.option('title', title);
    alkanziPreview.option('visible', true);
    alkanziPreview.focus();
    //var encoded = encodeURIComponent(pdfUrl); 
    //var { pdfjsLib } = globalThis;
    //console.log(pdfjsLib)
    $(`#alkanziPreviewFrame`).attr('src', pdfUrl); 
};
class prviewPopup
{
    popup = {
        showCloseButton: true, 
        //wrapperAttr: true,
        hideOnOutsideClick: true,
        deferRendering: false,
        //position: 'center',
        //position: {
        //    offset: "-400 0"
        //},
        dragOutsideBoundary: true,
        resizeEnabled: true,
        restorePosition: false,
        shading: false,
        shadingColor: 'rgba(0,0,0,0.5)',
        showTitle: true,
        wrapperAttr: { class: `form-tab-panel-popup alkanzi-custom-popup primaryPopup` },
        //fullScreen: true,
        //title: 'PREVIEW',
        width: '48%',
    };
    constructor()
    {
        var x = this.popup;
        let th = this;
        x.onInitialized = function (e)
        { 
            alkanziPreview = e.component;      
            alkanziPreview.option('toolbarItems', [
                {
                    location: "after",
                    toolbar: "top",
                    widget: 'dxButton',
                    options: {
                        icon: 'minus',
                        elementAttr: { class: "management-toolbar-btn" },
                        onClick(e)
                        {
                            alkanziPreview.hide();
                        }
                    }
                },
                {
                    location: "after",
                    toolbar: "top",
                    widget: 'dxButton',
                    options: {
                        icon: 'fullscreen',
                        elementAttr: { class: "management-toolbar-btn" },
                        onClick(e)
                        {
                            alkanziPreview.option('fullScreen', !alkanziPreview.option('fullScreen'));
                        }
                    }
                },
            ]);

        }
    }
};
//#endregion
//#region Gallery Popup
let _galaryPopup;
var _mainGalleryInit;
var _galleryFileUploaderInit;
const _openGalary = (config) =>
{

    _galaryPopup.option('title', (config.title == undefined ? 'Galary' : config.title));
    _galaryPopup.show(); 
    _galaryPopup.focus();
    _mainGalleryInit.option(`tmpUrl`, config.url)
    _mainGalleryInit.option(`dataSource.load`, () =>
    {
        return $.get(config.url);
    }); 
    _galleryFileUploaderInit.option('uploadUrl', config.uploadUrl);
};
class galaryPopup
{
    popup = {
        showCloseButton: true, 
        hideOnOutsideClick: false,
        deferRendering: false, 
        dragOutsideBoundary: true,
        resizeEnabled: true,
        restorePosition: true,
        shading: false,
        shadingColor: 'rgba(0,0,0,0.5)',
        showTitle: true,
        wrapperAttr: { class: `form-tab-panel-popup alkanzi-custom-popup primaryPopup` }, 
        width: '30%',
    }; 
    constructor()
    {
        var x = this.popup;
        let th = this;
        x.onInitialized = function (e)
        { 
            
            _galaryPopup = e.component;
            _galaryPopup.option('toolbarItems', [
                {
                    location: "after",
                    toolbar: "top",
                    widget: 'dxButton',
                    options: {
                        icon: 'minus',
                        elementAttr: { class: "management-toolbar-btn" },
                        onClick(e)
                        {
                            alkanziPreview.hide();
                        }
                    }
                },
                {
                    location: "after",
                    toolbar: "top",
                    widget: 'dxButton',
                    options: {
                        icon: 'fullscreen',
                        elementAttr: { class: "management-toolbar-btn" },
                        onClick(e)
                        {
                            alkanziPreview.option('fullScreen', !alkanziPreview.option('fullScreen')); 
                        }
                    }
                }, 
                {
                    location: "after",
                    toolbar: "bottom",
                    widget: 'dxButton',
                    width:300,
                    options: {
                        icon: 'fa-solid fa-camera-rotate',
                        elementAttr: { class: alkanziColors.popupToolbtn },
                        onClick(e)
                        { 
                            $("#openGallaryUploaderBtn").click();
                        }
                    }
                }, 
                {
                    location: "before",
                    toolbar: "bottom",
                    widget: 'dxFileUploader',
                    visible: false,
                    options: {
                        elementAttr: { id: 'openGallaryUploaderBtn' },
                        onInitialized(e)
                        {
                            _galleryFileUploaderInit = e.component;
                        },
                        hoverStateEnabled: true,
                        focusStateEnabled: true,
                        activeStateEnabled: true,
                        selectButtonText: 'SELECT PDF / IMAGE',
                        labelText: '',
                        accept: '.jpg,.jpeg,.gif,.png,.pdf,.jfif,.msg,.xlsx,xlsx',
                        multiple: false,
                        maxFileSize: 15728640,
                        minFileSize: 1,
                        dialogTrigger: "#openGallaryUploaderBtn",
                        uploadMode: "instantly",
                        name: "myFile",
                        uploadMethod: "POST",
                        multiple: true,
                        //uploadUrl: "/upload"
                        onUploaded(e)
                        {
                            hideLoader();
                            console.log(e)
                            let res = JSON.parse(e.request.response);
                            //let name = e.file.name;
                            if (res != undefined && res != null)
                            {
                                showIndicator(`Process Completed Successfully`);
                                _mainGalleryInit.option(`dataSource.load`, () =>
                                {

                                    return $.get(_mainGalleryInit.option(`tmpUrl`), (res) =>
                                    {
                                        setTimeout(() =>
                                        {
                                            _mainGalleryInit.option('selectedIndex',0);
                                        });
                                    });
                                }); 
                            };
                        }

                    }
                }
             ]);
            _galaryPopup.option(`contentTemplate`, () =>
            {
                return $('<div>').dxGallery( {
                        onInitialized(ee){
                        _mainGalleryInit = ee.component; 
                        },
                        dataSource: new DevExpress.data.CustomStore({
                            key: "ID",
                            loadMode: "raw",
                            load()
                            {
                                return [];
                            },
                            insert(values)
                            {
                                console.log('insert', values)
                                return values;
                            },
                            onPush(changes)
                            {
                                $scope._galleryInit.repaint();
                                setTimeout(() =>
                                {
                                    $scope._galleryInit.option('selectedIndex', 0);
                                });
                            }
                        }),
                        height: 440,
                        width: '100%',
                        //width: 380,
                        loop: false,
                        showNavButtons: true,
                        showIndicator: true,
                        animationEnabled: true,
                        focusStateEnabled: true, 
                        slideshowDelay: 0,
                        noDataText: 'No Images',
                    }
                );
            });
        };  
    }
};

//#endregion
const filterProcess = function (filter, key, value) {
    var pointer = 0;
    var tail = 0;
    while (pointer > - 1) {
        pointer = filter.indexOf('%' + key + '%', tail);
        if (pointer > -1) {
            filter = filter.replace('%' + key + '%', value);
        }
        tail = pointer + 1;
    }
    return filter;
};
const newPopup = function () {
    return {
        wigth: 100,
        height: 400,
        //elementAttr: true,
        hideOnOutsideClick: false,
        deferRendering: false,
        position: 'center',
        dragOutsideBoundary: true,
        resizeEnabled: true,
        restorePosition: true,
        shading: true,
        shadingColor: 'rgba(0,0,0,0.5)',
        showTitle: true,
        title: 'Title',
    }
};
const setInsertDefaultParams = function (model) {
    model.IS_DELETED = false;
    model.IS_UPDATED = false;
    model.DELETED_BY = 0;
    model.UPDATED_BY = 0;
    model.CREATED_BY = parseInt(sessionStorage.getItem("UserId"));
    model.ORG_ID = parseInt(sessionStorage.getItem("OrganizationId"));
    model.COMP_ID = parseInt(sessionStorage.getItem("UnitId"));
    model.BRANCH_ID = parseInt(sessionStorage.getItem("BranchId"));
    return model;
};
const insertParam = async function (model) {
    model.IS_DELETED = false;
    model.IS_UPDATED = false;
    model.DELETED_BY = 0;
    model.UPDATED_BY = 0;
    model.CREATED_BY = parseInt(sessionStorage.getItem("UserId"));
    model.ORG_ID = parseInt(sessionStorage.getItem("OrganizationId"));
    model.COMP_ID = parseInt(sessionStorage.getItem("UnitId"));
    model.BRANCH_ID = parseInt(sessionStorage.getItem("BranchId"));
    return model;
};
const setUpdatetDefaultParams = function (model) {
    model.IS_DELETED = false;
    model.IS_UPDATED = true;
    model.UPDATED_BY = parseInt(sessionStorage.getItem("UserId"));
    model.CREATED_BY = 0;
    model.DELETED_BY = 0;
    model.ORG_ID = parseInt(sessionStorage.getItem("OrganizationId"));
    model.COMP_ID = parseInt(sessionStorage.getItem("UnitId"));
    model.BRANCH_ID = parseInt(sessionStorage.getItem("BranchId"));
    return model;
};
const setDeleteDefaultParams = function (model) {
    model.IS_DELETED = true;
    model.IS_UPDATED = false;
    model.CREATED_BY = 0;
    model.DELETED_BY = 0;
    model.DELETED_BY = parseInt(sessionStorage.getItem("UserId"));
    model.ORG_ID = parseInt(sessionStorage.getItem("OrganizationId"));
    model.COMP_ID = parseInt(sessionStorage.getItem("UnitId"));
    model.BRANCH_ID = parseInt(sessionStorage.getItem("BranchId"));
    return model;
};
const setReportDefaultParams = function (model) {
    model.USER_ID = parseInt(sessionStorage.getItem("UserId"));
    model.ORG_ID = parseInt(sessionStorage.getItem("OrganizationId"));
    model.COMP_ID = parseInt(sessionStorage.getItem("UnitId"));
    return model;
};
const setUserUpdateInfo = function (transLevel, userLevel) {
    if (transLevel > -1 && transLevel < userLevel) {
        return true;
    }
    return false;
};
const gender = [{ id: -1, name: 'company', display: 'COMPANY' }, { id: 1, name: 'male', display: 'MALE' }, { id: 2, name: 'female', display: 'FEMALE' }];
const yesOrNo = [{ Id: 1, Name: 'Yes' }, { Id: 0, Name: 'No' }];
const phoneTypes = [{ id: 1, name: 'Personal' }, { id: 2, name: 'Home' }, { id: 3, name: 'Static' },];
const specialKeys = [
    { keyCode: 38, key: 'ArrowUp' },
    { keyCode: 39, key: 'ArrowRight' },
    { keyCode: 37, key: 'ArrowLeft' },
    { keyCode: 40, key: 'ArrowDown' },
    { keyCode: 9, key: 'Tab' },
];
const allowUpdate = function (approveStatus, approveLevel, userLevel) {
    if (approveStatus === 0 || approveStatus === 2) {
        return true;
    }
    //if (approveStatus === 3 || approveStatus === 4 ) {
    //    return false;
    //} else {
    //    if ( approveLevel <= userLevel) {
    //        return true;
    //    }
    //}
    return false;
}
const allowDelete = function (approveStatus, approveLevel, userLevel) {
    if (approveStatus === 3 || approveStatus === 4) {
        return false;
    } else {
        if (approveLevel <= userLevel) {
            return true;
        }
    }
    return false;
}
const _staticApproveStatus = function (statusId) {
    switch (statusId) {
        case 0:
            return "Suspend";
        case 1:
            return "Submited";
        case 2:
            return "Rework";
        case 3:
            return "Rejected";
        case 4:
            return "Approved";
        default:
            return "";
    }
}
var _mainFullScreenElem = document.documentElement;
function openFullscreen() {
    if (_mainFullScreenElem.requestFullscreen) {
        _mainFullScreenElem.requestFullscreen();
    } else if (_mainFullScreenElem.mozRequestFullScreen) { /* Firefox */
        _mainFullScreenElem.mozRequestFullScreen();
    } else if (_mainFullScreenElem.webkitRequestFullscreen) { /* Chrome, Safari & Opera */
        _mainFullScreenElem.webkitRequestFullscreen();
    } else if (_mainFullScreenElem.msRequestFullscreen) { /* IE/Edge */
        _mainFullScreenElem = window.top.document.body; //To break out of frame in IE
        _mainFullScreenElem.msRequestFullscreen();
    }
}
function closeFullscreen() {
    if (document.exitFullscreen) {
        document.exitFullscreen();
    } else if (document.mozCancelFullScreen) {
        document.mozCancelFullScreen();
    } else if (document.webkitExitFullscreen) {
        document.webkitExitFullscreen();
    } else if (document.msExitFullscreen) {
        window.top.document.msExitFullscreen();
    }
}
function checkSgPermissions(name, dataList) {

    if (dataList == undefined || dataList == null) {
        dataList = [];
    }
    var pointer1 = dataList.find(item => item.NAME.toUpperCase() === 'READ ONLY'.toUpperCase());

    if (pointer1 == undefined || pointer1 == null) {
        var pointer2 = dataList.find(item => item.NAME.toUpperCase() === name.toUpperCase());
        //console.log(pointer2)
        if (pointer2 == undefined || pointer2 == null) {
            return false;
        }
        return true;
    }

    return false;
}
function getUserData() {
    return {
        org: parseInt($.cookie('Organization')),
        comp: parseInt($.cookie('CompanyId')),
        branch: parseInt($.cookie('BranchId')),
        userId: parseInt($.cookie('UserId')),
    };
};
const _approveStatusFilters = [{ id: -1, text: 'All' }, { id: -2, text: 'Pending Approval' }, { id: 0, text: 'Suspend' }, { id: 1, text: 'Submitted' }, { id: 4, text: 'Approved' }, { id: 2, text: 'Rework' }, { id: 3, text: 'Rejected' },];
function showError(data) {
    if (parseInt($.cookie("securityGroup")) === 1) {
        console.log(data)
        //bsOffcanvas.show();
        //$("#ErrorConsole").html("<span>" + data+"</span>");
    }
}
const _decFormat = {
    type: "fixedPoint",
    precision: 3
};
const _decFormat2 = {
    type: "fixedPoint",
    precision: 2
};
const decDefOptions = {
    min: 0,
    showSpinButtons: true,
    format: _decFormat2,
};
const _dateFormat = 'dd-MMM-yyyy';
const statusPrepared = function (e) {
    switch (e.value) {
        case "Suspend":
            e.cellElement.css("background", "#fff");
            break;
        case "Submitted":
            e.cellElement.css("background", "#337ab7");
            e.cellElement.css("color", "white");
            break;
        case "Rework":
            e.cellElement.css("background", "rgb(239 130 18)");
            e.cellElement.css("color", "#fff");
            break;
        case "Rejected":
            e.cellElement.css("background", "#c55e5e");
            e.cellElement.css("color", "white");
            break;
        case "Approved":
            e.cellElement.css("background", "green");
            e.cellElement.css("color", "white");
            break;
        default:
            break;
    }
}

const GetParam = function (doc, id) {
    let filterDate = new Date();
    filterDate.setMonth(filterDate.getMonth() - 13);
    return {
        FLAG: 'all',
        DOC_TYPE: doc,
        NID: id,
        USER_ID: _UserId,
        ACTION_DATE: filterDate,
        APPROVE_STATUS: -1,
        BRANCH: _BranchId,
        COMPANY: _CompId,
        ORG: _OrgId,
        SG: _sgId
    };
}

const getSalesOrderSession = function () {
    return sessionStorage.getItem('salesOrderConnId');
};
const getConfig = function ()
{
    return {
        'UserId': parseInt(sessionStorage.getItem("UserId")),
        'OrgId': parseInt(sessionStorage.getItem('OrganizationId')),
        'CompId': parseInt(sessionStorage.getItem('UnitId')),
        'BranchId': parseInt(sessionStorage.getItem('BranchId')),
        'SecurityGroupId': parseInt(sessionStorage.getItem("securityGroup")),
        'StockBranch': parseInt(sessionStorage.getItem("StockBranch")),
    };
}

// Resolves once the user-profile fetch (setStorage in site.js) has populated sessionStorage.
// Synchronous readers can keep calling getConfig(); async callers that need a guaranteed-
// populated config should call whenConfigReady().then(cfg => ...).
window.__configReady = window.__configReady || (function () {
    let resolveFn;
    const p = new Promise(function (r) { resolveFn = r; });
    p.__resolve = resolveFn;
    return p;
})();

function configIsPopulated(c) {
    return !!(c && c.UserId && c.OrgId && c.CompId && c.BranchId && c.SecurityGroupId)
        && !isNaN(c.UserId) && !isNaN(c.OrgId) && !isNaN(c.CompId) && !isNaN(c.BranchId) && !isNaN(c.SecurityGroupId);
}

window.whenConfigReady = function () {
    const cfg = getConfig();
    if (configIsPopulated(cfg)) return Promise.resolve(cfg);
    return window.__configReady.then(function () { return getConfig(); });
};
const getDocName = function () {
    try {
        return JSON.parse(sessionStorage.getItem("docType")).trim();
    } catch (e) {
        return sessionStorage.getItem("docType");
    }
}
const getPermission = function (doc, divisionId, sgId) {
    doc = getDocName();
    divisionId = parseInt(sessionStorage.getItem('BranchId'));
    sgId = parseInt(sessionStorage.getItem("securityGroup"));
    return $.get(`/api/user/sgPermission/${doc}/${divisionId}/${sgId}`);
}
const userIconstandby = function (e) {
    //$(e).attr('src', `/upload/user.png`);
} 