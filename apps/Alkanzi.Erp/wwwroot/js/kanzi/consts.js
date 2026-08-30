let x = sessionStorage.getItem('OrganizationId');
test = true;
const _breakpoints = {
    xSmallMedia: window.matchMedia("(max-width: 599.99px)"),
    smallMedia: window.matchMedia("(min-width: 600px) and (max-width: 959.99px)"),
    mediumMedia: window.matchMedia("(min-width: 960px) and (max-width: 1279.99px)"),
    largeMedia: window.matchMedia("(min-width: 1280px)")
};
const _isLarge = _breakpoints.largeMedia.matches;
const _isXSmall = _breakpoints.xSmallMedia.matches;
//const _yesterday = new Date();
if (x == null || x == undefined) {
    _OrgId = 0;
    _CompId = 0;
    _BranchId = 0;
    _sgId = 0;
    _UserId = 0;
} else {
    _OrgId = parseInt(sessionStorage.getItem('OrganizationId'));
    _CompId = parseInt(sessionStorage.getItem('UnitId'));
    _BranchId = parseInt(sessionStorage.getItem('BranchId'));
    _sgId = parseInt(sessionStorage.getItem("securityGroup"));
     _UserId = parseInt(sessionStorage.getItem("UserId"));
    //_UserId = 21;
    _tabName = sessionStorage.getItem('tabName');
    _stockBranch = parseInt(sessionStorage.getItem("StockBranch"));
    if (_CompId == 25)
    {
        //let date = new Date();
        //_yesterday = date.setFullYear(date.getFullYear() - 1);
        _yesterday = null;
    } else {
        _yesterday = new Date((new Date()).valueOf() - (1000 * 60 * 60 * 24) * 3);
    };
}

function getStockParam() {
    return {
        stockBranch: parseInt(sessionStorage.getItem("StockBranch"))
    }
}

const request = { insert: 'insert', delete: 'delete', update: 'update', submit: 'submit', cancel:'cancel', post:'post', generate:'generate' };
const icons = {
    check: 'dx-icon dx-icon-check',
    close: 'dx-icon dx-icon-close',
    cancel: 'fa-solid fa-ban',
}
const cellColors = {
    grayDegree2: 'gray-degree-2-cell',
    orange: 'custom-orange-cell',
    white: 'white-cell',
    brownCell: 'brown-cell',
    lemonNormal: 'lemon-normal-cell',
    normal: 'normal-cell',
    darkNormal: 'dark-normal',
    lightSuccess: 'light-success-cell',
    customSuccessBtn: 'custom-success-btn',
    customWarningBtn: 'custom-warning-btn',
    customBrownBtn: 'custom-brown-btn',
    cusomtBrownCloseBtn: `custom-brown-close-btn`,
    success: 'success-cell',
    solid: 'solid-cell',
    default: 'default-cell',
    organge: 'orange-cell',
    orangeDashboard: 'orange-dashboard-cell',
    yellowDark1: 'yellow-dark1-cell',
    warning: 'warning-cell',
    lemonYellow: 'leamon-yellow-cell',
    lemonBlack: 'leamon-black-cell',
    danger: 'danger-cell',
    renew: 'renew-cell',
    lightRed: 'light-red-cell',
    darkGray: 'dark-gray-cell',
    noramlNoBg: 'normal-btn-no-bg',
    noramlBtnBg: 'normal-btn-bg',
    lightBrown: 'light-brown-cell',
    lightGreen: 'light-green-cell',
    greenDegree1: 'green-degree1-cell',
    warningField: 'warning-field',
    activeGradient: `active-gradient`,
    alertGradient: `alert-gradient`,
    orangeGradient: `orange-gradient`,
    silverGreen: `silver-green-cell`,
    greenBorderBtn: `custom-close-btn`,
    cusomtSuccessCell: `custom-success-cell`,
    gold: `custom-gold`,
    customSuccessCellDeg2: `custom-success-cell-2`,
    customBrownCell: `custom-brown-cell`,
    cusomtSuccessIcon: `custom-success-icon`,
    cusomtNormalIcon: `custom-normal-icon`,
    cusomtDangerIcon: `custom-danger-icon`,
    cusomtYellowIcon: `custom-yellow-icon`,
    customGrayField: `custom-gray-field`,
    customSuccessField: `custom-success-field`,
    grayDegree3: `gray-degree3-cell`,
    pLightBlue: `p-light-blue-cell`,
    pLightGreen: `p-light-green-cell`,
    redFont: 'red-font',
    darkGrayFont: 'darkgray-font',
};
const alkanziColors = {
    softGray: 'soft-gray',
    darkNormal: 'dark-normal',
    darkNormalWhite: 'dark-normal-white',
    softGeen: 'soft-green',
    softWarning: 'soft-warning',
    bgSemiGray: 'semi-gray',
    semiWarning: 'semi-warning',
    semiSuccess: 'semi-success',
    semiPrimary: 'semi-primary',
    semiYellow: 'semi-yellow',
    semiBrown: 'semi-brown',
    semiDanger: 'semi-danger',
    orangeFont: 'orange-font',
    redFont: 'red-font',
    successFont: 'success-font',
    popupToolbtn: 'alkanzi-popup-toolbtn',
    alkanziPrimaryCell: 'alkanzi-primary-cell',
    assetStyle: 'asset-style',
};

  const  _loadApprovalProgress = (docType, id) => {
    let $container = $('#approvalProgressSection');
    $container.empty();
      if (id > 0) {
        let row = { docType: docType, transId: id };
        $.get(`${apis.approvalProgress}docProgress`, row).done((res) => {
            if (res && res.length > 0) {
                $.each(res, (i, data) => {
                    let stepHtml = $(`<div class='step ${data.STATUS} ${data.IS_FINAL}'>`).html(
                        `<div class="circle">${data.STATUS_ID}</div>
                             <div class="content">
                                <div class="label">${data.LABEL}</div>
                                <div class="desc usr">${data.USER_NAME}</div>
                                <div class="desc">${data.POST_DATE}</div>
                             </div>`
                    );
                    $container.append(stepHtml);
                });
                $container.show();
            } else {
                $container.hide();
            }
        });
    } else {
        $container.hide();
    }
};

const DevExpressNumberFormat = function (val, points = 3) {
    return DevExpress.localization.formatNumber(val, { type: 'fixedPoint', precision: points })
};
const DevExpressDecimalFormat = function (val, points = 2) {
    return DevExpress.localization.formatNumber(val, { type: 'fixedPoint', precision: points })
};
const defaultGrid = function () {
    return {
        autoNavigateToFocusedRow: true,
        activeStateEnabled: true,
        focusStateEnabled: true,
        hoverStateEnabled: true,
        focusedRowEnabled: true,
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
        onCellPrepared(e) {
            if (e.rowType === 'data') {
                let field = e.column.dataField;
                if (field === 'ID' || field === 'DOC_NO') {
                    e.cellElement.addClass(cellColors.normal);
                } else if (field === 'AMOUNT') {
                    e.cellElement.addClass(cellColors.default);
                }
            }
        },
        summary: {
            recalculateWhileEditing: true,
            totalItems: [{
                column: "AMOUNT",
                summaryType: "sum",
                valueFormat: _decFormat,
                displayFormat: "{0}",
                precision: '2',
            }, {
                column: "SCHEDULE_AMT",
                summaryType: "sum",
                valueFormat: 'fixedPoint',
                displayFormat: "{0}",
                precision: '2',
            },]
        },
        export: { fileName: 'Excel Report', enabled: true },
    }
};
const _diffDays = (date, otherDate) => Math.ceil(Math.abs(date - otherDate) / (1000 * 60 * 60 * 24));
const _openWfStack = function (transDoc,transId)
{
    $("#wfStackPopup").dxPopup('option', 'visible', true);
}
const _openKzApproval = function (transDoc, transId)
{
    $("#kzApprovalPopup").dxPopup('option', 'visible', true);
};
const _defaultParam = function (model,isUpdate) { 
    if (isUpdate)
        model.updatedBy = _UserId;
    else model.createdBy = _UserId;
    model.orgId = _OrgId;
    model.compId = _CompId;
    model.branchId = _BranchId;
    return model;
};


