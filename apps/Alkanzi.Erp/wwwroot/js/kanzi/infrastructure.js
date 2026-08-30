class formPopup {
    form = {
        keyExpr: "ID",
        colCount: 1,
        focusStateEnabled: true,
        scrollingEnabled: true,
        showRequiredMark: true,
        showColonAfterLabel: false,
        formData: {},
        labelMode: "floating",
        labelLocation: 'top',
        items: [],
        screenByWidth: function (width) {
            //debugger;
            if (width < 768) return "xs";
            if (width < 992) return "sm";
            if (width < 1200) return "md";
            //return "xs";
            return "lg";
        },
    };
    popup = {
        showCloseButton: false,
        focusStateEnabled: true,
        wrapperAttr: true,
        hideOnOutsideClick: false,
        deferRendering: false,
        position: 'center',
        dragOutsideBoundary: true,
        resizeEnabled: true,
        restorePosition: true,
        shading: false,
        shadingColor: 'rgba(0,0,0,0.5)',
        title: '',
        //width: '60%',
        //height: 'auto',
        /*wrapperAttr: { class: 'form-tab-panel-popup' },*/
    };
    constructor(InitColCount = 1, showSubmit = true, module = 'all', saveBtnText = 'Save', popupClass = '') {
        var pop = this.popup;
        var f = this.form;
        let th = this;
        pop.onInitialized = function (e) {
            let init = e.component;
            th.popupInit = e.component;
            init.option(`wrapperAttr`, { class: `form-tab-panel-popup ${popupClass}` })
            th.hidePopup = function () {
                init.hide();
            };
            th.showPopup = function (show = true) {
                if (show == undefined) {
                    show = true;
                }
                init.option('toolbarItems[1].options.visible', show)
                init.show();
            };
            th.set = function (title, data) {
                let _isMinimized = th._isMinimized;
                //console.log(_isMinimized);
                //th.formInit.resetValues();
                if (_isMinimized == false || _isMinimized == undefined)
                {
                    //th.formInit.resetValues();
                    th.formInit.option('formData', data);
                } else if (_isMinimized)
                {
                    th.formInit.option('formData', data);
                };
                th.popupTitle(title);
                th.showPopup();
            };
            th.popupTitle = function (title) {
                init.option('title', title);
            };
            th.showSubmit = function (val) {
                setTimeout(() => {
                    th._submitBtnInit.option('visible', val);
                }, 100);
            }
            let saveBtnStyle = 'custom-success-btn';
            let closeBtnStyle = 'custom-close-btn';
            switch (module)
            {
                case "call":
                    saveBtnStyle = cellColors.darkNormal;
                    closeBtnStyle = cellColors.darkNormal;
                    break;
                default:
                    saveBtnStyle = 'custom-success-btn';
                    closeBtnStyle = 'custom-close-btn';
                    break;
            }
            init.option('toolbarItems',
                [
                    {
                        location: "after",
                        toolbar: "top",
                        widget: 'dxButton',
                        options: {
                            icon: 'minus',
                            elementAttr: { class: "management-toolbar-btn" },
                            onClick(e) {
                                th._isMinimized = true;
                                init.hide();
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
                                init.option('fullScreen', !init.option('fullScreen'));
                            }
                        }
                    },
                    {
                        location: "after",
                        toolbar: "top",
                        widget: 'dxButton',
                        options: {
                            icon: 'close',
                            elementAttr: { class: "management-toolbar-btn" },
                            onClick(e) {
                                th._isMinimized = false;
                                init.hide();
                            }
                        }
                    },
                    {
                        location: "before",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            icon: 'save',
                            //text: 'Save',
                            text: saveBtnText,
                            elementAttr: { class: saveBtnStyle },
                            visible: showSubmit,
                            //disabled:true,
                            onInitialized(e)
                            {
                                th._submitBtnInit = e.component;
                            },
                            onClick() {
                                //console.log(th.formInit.validationGroup)
                                let validate = th.formInit.validate();
                                if (validate == undefined) {
                                    th.submit();
                                } else {
                                    if (validate.isValid) {
                                        th.submit();
                                    }
                                }

                            },
                        }
                    },
                    {
                        location: "before",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            text: 'Close',
                            elementAttr: { class: closeBtnStyle },
                            onClick() {
                                th._isMinimized = false;
                                init.hide();
                            }
                        }
                    },
                ]
            );
            th.addToolbarItem = function (item)
            {
                let items = init.option('toolbarItems');
                let last = items.length;
                init.option(`toolbarItems[${last}]`, item);
            };
        };
        f.onInitialized = function (e) {
            let comp = e.component;
            th.formInit = comp;
            comp.option('colCount', InitColCount);
            th.getData = function () {
                return comp.option('formData');
            };
            th.setData = function (data) {
                comp.option('formData', data);
            };
            th.getValueOf = function (name) {
                return comp.getEditor(name).option('value');
            };
            th.setValueOf = function (name,val) {
                return comp.getEditor(name).option('value', val);
            };
            th.getSourceOf = function (name,data) {
                return comp.getEditor(name).option('dataSource',data);
            };
            th.setSourceOf = function (name, data) {
                return comp.getEditor(name).option('dataSource', data);
            };
            th.optionOf = function (name, option, data = 'onlyShow') {
                if (data === 'onlyShow' || data == null) {
                    return comp.getEditor(name).option(option);
                } else {
                    comp.getEditor(name).option(option, data);
                }
            };
            th.focusItem = function (name) {
                comp.getEditor(name).focus();
            };                                                                                                                                                          
        };
    }
};
class customGrid {
    dataGrid = {
        autoNavigateToFocusedRow: true,
        activeStateEnabled: true,
        focusStateEnabled: true,
        hoverStateEnabled: true,
        focusedRowEnabled: true,
        showBorders: true,
        keyExpr: 'ID',
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
        showColumnLines: true,
        showRowLines: true,
        columnMinWidth: 50,
        columnAutoWidth: true,
        columnHidingEnabled: false,
        remoteOperations: false,
        editing: {
            mode: 'row',
            allowUpdating: true,
            allowAdding: true,
            allowDeleting: true,
            useIcons: true,
        },
        repaintChangesOnly:true,
        onCellPrepared(e) {
            if (e.rowType === 'data') {
                let field = e.column.dataField;
                if (field === 'ID') {
                    e.cellElement.addClass('normal-cell');
                }
            }
        },
        onInitNewRow(e) {
            e.data.ID = 0;
        },
        onToolbarPreparing(e) {
            let gridInit = e.component;
            e.toolbarOptions.items.unshift(
                {
                    location: "before",
                    widget: "dxButton",
                    options: {
                        icon: "refresh",
                        type: 'normal',
                        onClick: function () {
                            gridInit.refresh();
                        }
                    }
                },
            );
        }
    };
    constructor() {
        let grid = this;
        this.dataGrid.onInitialized = function (e) {
            grid.gridInit = e.component;
            let gridCompo = e.component;
            grid.getRowAsync = async function (rowKey) {
                let index = gridCompo.getRowIndexByKey(rowKey);
                let row = gridCompo.getVisibleRows()[index].data;
                return row;
            };
            grid.getRow = function (rowKey) {
                let index = gridCompo.getRowIndexByKey(rowKey);
                let row = gridCompo.getVisibleRows()[index].data;
                return row;
            };
            grid.getRowAsync = async function (rowKey) {
                let index = gridCompo.getRowIndexByKey(rowKey);
                let row = gridCompo.getVisibleRows()[index].data;
                return row;
            };
            grid.addRow = function () {
                gridCompo.addRow();
            };
            grid.customRepaintRow = function (rowKey, newRow) {
                var index = gridCompo.getRowIndexByKey(rowKey);
                var row = gridCompo.getVisibleRows()[index].data;
                Object.assign(row, newRow);
                gridCompo.repaintRows([index]);
            };
            //grid.customAddRow = function (row)
            //{
            //    var tmpStore = gridCompo.option('dataSource');
            //    tmpStore.push(row);
            //    gridCompo.option('dataSource', tmpStore);
            //};
        }
    }
};
class filterBuilder {
    builder = {
       
    };
    popup = {
        showCloseButton: true,
        focusStateEnabled: true,
        wrapperAttr: true,
        hideOnOutsideClick: false,
        deferRendering: false,
        position: 'center',
        dragOutsideBoundary: true,
        resizeEnabled: true,
        restorePosition: true,
        shading: true,
        shadingColor: 'rgba(0,0,0,0.5)',
        title: 'Filter',
    };
    constructor() {
        let pop = this.popup;
        let builder = this.builder;
        let th = this;
        pop.onInitialized = function (e) {
            let init = e.component;
            th.popupInit = e.component;
            th.hidePopup = function () {
                init.option('visible', false);
            };
            th.showPopup = function (show = true) {
                if (show == undefined) {
                    show = true;
                }
                init.option('toolbarItems[1].options.visible', show)
                init.option('visible', true);
            };
            th.set = function (title, data) {
                //th.formInit.option('formData', data);
                //th.popupTitle(title);
                //th.showPopup();
            };
            th.popupTitle = function (title) {
                init.option('title', title);
            };
            init.option('toolbarItems',
                [
                    {
                        location: "after",
                        toolbar: "top",
                        widget: 'dxButton',
                        options: {
                            icon: 'fa-solid fa-minus',
                            elementAttr: { class: "management-toolbar-btn" },
                            onClick(e) {
                                init.option('visible', false);
                            }
                        }
                    },
                    {
                        location: "after",
                        toolbar: "top",
                        widget: 'dxButton',
                        options: {
                            icon: 'fa-solid fa-expand',
                            elementAttr: { class: "management-toolbar-btn" },
                            onClick(e) {
                                init.option('fullScreen', !init.option('fullScreen'));
                            }
                        }
                    }, 
                    {
                        location: "before",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            icon: 'filter',
                            text: 'Filter',
                            elementAttr: { class: 'custom-success-btn' },
                            onClick() {
                                th.submit();
                            },
                        }
                    },
                    {
                        location: "before",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            text: 'Close',
                            elementAttr: { class: 'custom-close-btn' },
                            onClick() {
                                init.option('visible', false);
                            }
                        }
                    },
                ]
            );
        };
        builder.onInitialized = function (e) {
            let comp = e.component;
            //th.builderInit = comp;
            th.getData = function () {
                return comp.option('value');
            };
            th.setData = function (data) {
                comp.option('value', data);
            };
            th.filterExpression = function () {
                return comp.getFilterExpression();
            };
        };
    }
};
const requestParam = function (model) {
    model.CREATED_BY = parseInt(sessionStorage.getItem('UserId'));
    model.ORG_ID = parseInt($.cookie('Organization'));
    model.COMP_ID = parseInt(sessionStorage.getItem('UnitId'));
    model.BRANCH_ID = parseInt(sessionStorage.getItem('BranchId'));
    model.SG_ID = parseInt(sessionStorage.getItem('securityGroup'));
    return model;
}

class transApproval {
    constructor(division = 0, sgId = 0, doc, result = function () { }) {
        //console.log(result)
        let th = this;
        if (division == 0) {
            division = _BranchId;
        };
        if (sgId == 0) {
            sgId = _sgId;
        };
        if (doc == null || doc == undefined) {
            doc = getDocName();
        };
        th.divisionId = division;
        th.sgId = sgId;
        th.result = result;

        let elementTester = document.getElementById(`${doc}submitPopup`);
        if (elementTester != null || elementTester != undefined) {
            elementTester.remove();
        }
        let newPopup = document.createElement("div");
        newPopup.setAttribute("id", `${doc}submitPopup`);

        let newForm = document.createElement("div");
        newForm.setAttribute("id", `${doc}submitForm`);

        let newAlet = document.createElement("div");
        newAlet.setAttribute("id", `${doc}approvalAlerts`);

        let dxList = document.createElement("div");
        dxList.setAttribute("id", `${doc}submitLogs`);

        let scroll = document.createElement("div");
        scroll.setAttribute("id", `${doc}submitScroll`);
        scroll.appendChild(newForm);
        scroll.appendChild(newAlet);
        scroll.appendChild(dxList);
        newPopup.appendChild(scroll);
        document.body.appendChild(newPopup);
        setTimeout(() => {
            $(`#${doc}submitPopup`).dxPopup({
                onInitialized(e) {
                    let init = e.component;
                    th.popupInit = e.component;
                    th.hidePopup = function () {
                        init.hide();
                    };
                    th.showPopup = function (show = true) {
                        if (show == undefined) {
                            show = true;
                        }
                        init.option('toolbarItems[1].options.visible', show)
                        init.show();
                    };
                    th.popupTitle = function (title) {
                        init.option('title', title);
                    };
                    init.option('toolbarItems',
                        [
                            {
                                location: "after",
                                toolbar: "top",
                                widget: 'dxButton',
                                options: {
                                    icon: 'minus',
                                    elementAttr: { class: "management-toolbar-btn" },
                                    onClick(e) {
                                        init.hide();
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
                                        init.option('fullScreen', !init.option('fullScreen'));
                                    }
                                }
                            },
                            {
                                location: "after",
                                toolbar: "bottom",
                                widget: 'dxButton',
                                options: {
                                    icon: 'close',
                                    text: 'Close',
                                    elementAttr: { class: 'custom-close-btn' },
                                    onClick() {
                                        init.hide();
                                    }
                                }
                            },
                            {
                                location: "after",
                                toolbar: "bottom",
                                widget: 'dxButton',
                                options: {
                                    icon: 'save',
                                    text: 'Save',
                                    type: 'default',
                                    elementAttr: { class: 'custom-success-btn' },
                                    onClick() {
                                        //console.log(th.formInit.validationGroup)
                                        th.submit();
                                    }
                                }
                            },
                        ]
                    );
                },
                //elementAttr: { class:''},
                showCloseButton: true,
                focusStateEnabled: true,
                wrapperAttr: true,
                hideOnOutsideClick: false,
                deferRendering: false,
                position: 'center',
                dragOutsideBoundary: true,
                resizeEnabled: true,
                restorePosition: true,
                shading: true,
                shadingColor: 'rgba(0,0,0,0.5)',
                title: '',
                //width: '60%',
                //copyRootClassesToWrapper: true,
                wrapperAttr: true,
            });
            $(`#${doc}submitScroll`).dxScrollView({ width: '100%', height: '100%' });
            $(`#${doc}submitForm`).dxForm({
                onInitialized(e) {
                    let formInit = e.component;
                    th.formInit = formInit;
                    formInit.option('items[1].editorOptions.onSelectionChanged', (e) => {
                        let item = e.selectedItem;
                        if (item != undefined || item != null) {
                            th.setLevels(item.ID);
                        }
                    });
                    th.startProcess = async function (docType, transId, transLevel, transApprveStatus = 1) {
                        //-------> step 1
                        if (docType == null || docType == undefined) {
                            showIndicator('Invalid Doc Type', 'error')
                            return false;
                        };
                        //console.log(docType)
                        th._actualTransLevel = transLevel;
                        th.transDoc = docType;
                        th._transId = transId;
                        th._transLevel = transLevel;
                        let divisionId = th.divisionId;
                        let sgId = th.sgId;
                        th.getUserLevel(divisionId, sgId, docType, transId, transLevel, transApprveStatus);
                    };
                    th.getHistory = function (docName, id) {
                        //console.log(docName, id)
                        showBasicLoader(true);
                        th.dxListInit.option('dataSource', []);
                        $.get(`/api/approve/${docName}/${id}`).then((res) => {
                            showBasicLoader(false);
                            if (res.length > 0) {
                                let row = res[0];
                                formInit.getEditor(`CREATED_BY`).option('value', row['CREATED_BY']);
                                formInit.getEditor(`CREATED_AT`).option('value', row['CREATED_AT']);
                            }
                            th.dxListInit.option('dataSource', res);
                            setTimeout(() => {
                                th.showPopup();
                            });
                        });
                    };
                    th.getAlertMessages = function (docName, id) {
                        $(`#${doc}approvalAlerts`).html("");
                        $.get(`/api/approve/GetMessages/${docName}/${id}`).then((res) => {
                            if (res.length > 0) {
                                var allertContent = '<div class="alertwarning alertt" style="margin-top: 20px">';
                                allertContent += '<div class="alertcontent">';
                                allertContent += '<div class="alerticon">';
                                allertContent += ' <svg height="50" viewBox="0 0 512 512" width="50" xmlns="http://www.w3.org/2000/svg"><path fill="#fff" d="M449.07,399.08,278.64,82.58c-12.08-22.44-44.26-22.44-56.35,0L51.87,399.08A32,32,0,0,0,80,446.25H420.89A32,32,0,0,0,449.07,399.08Zm-198.6-1.83a20,20,0,1,1,20-20A20,20,0,0,1,250.47,397.25ZM272.19,196.1l-5.74,122a16,16,0,0,1-32,0l-5.74-121.95v0a21.73,21.73,0,0,1,21.5-22.69h.21a21.74,21.74,0,0,1,21.73,22.7Z" /></svg>';
                                allertContent += '</div>';
                                allertContent += '<p>';
                                allertContent += '<strong>Warning!</strong> ';
                                allertContent += '<ul>';
                                $.each(res, function (i, item) {
                                    allertContent += `<li>${item.MESSAGE_TO_SHOW}</li>`;
                                });

                                allertContent += '</ul>';
                                allertContent += '</p>';
                                allertContent += '</div>';
                                allertContent += '</div>';
                                $(`#${doc}approvalAlerts`).html(allertContent);
                            } else {
                                $(`#${doc}approvalAlerts`).html("");
                            }

                        });
                    };
                    th.getUserLevel = function (divisionId, sgId, docType, transId, transLvl, apprveStatus = 1) {
                        //-------> step: 2
                        console.log(`/api/approve/sgApproval/v2/${divisionId}/${sgId}/${docType}/${transLvl}/${transId}`);

                        $.get(`/api/approve/sgApproval/v2/${divisionId}/${sgId}/${docType}/${transLvl}/${transId}`).then((res) => {
                            console.log(res, 'getUserLevel')
                            let _isAuthorized = res.IS_AUTHORIZED;
                            //console.log(_isAuthorized, '_isAuthorized');
                            //debugger;
                            if (_isAuthorized) {
                                let userLevel = res.LEVEL_ID;
                                th._canOverlap = true;
                                th._userLevel = userLevel;
                                th._lastLevel = res.LAST_LEVEL;
                                th._formId = res.FORM_ID;
                                th._docId = res.DOC_ID;
                                th.docData = res;
                                th.overLapType = res.OVERLAP_TYPE;
                                th.workflowLevel = res.WORKFLOW_LEVELS;
                                //step: 3
                                th.getAlertMessages(docType, transId);
                                th.getHistory(docType, transId);
                                if (transLvl >= th._lastLevel) {
                                    th.popupInit.option('toolbarItems[1].options.disabled', true);
                                    th.formInit.option('disabled', true);
                                }
                                else {
                                    th.formInit.option('disabled', false);
                                    //transLvl == 0 ? transLvl = transLvl + 1 : transLvl = userLevel;
                                    th._userLevel == 0 ? transLvl = transLvl + 1 : transLvl = th._actualTransLevel + 1;
                                    transLvl == 0 ? transLvl = transLvl + 1 : transLvl = th._actualTransLevel + 1;
                                    th._transId = transId;
                                    th._transLevel = transLvl;
                                    th.resetApproval(transLvl, apprveStatus);
                                    th.popupInit.option('toolbarItems[1].options.disabled', false);
                                }
                            }
                            else {
                                console.log(res)
                                showAlert(res.Message);
                            }
                        });
                    };
                    th.resetApproval = function (id, apprveStatus = 1) {
                        th.formInit.resetValues();
                        var tmp = [];
                        if (id == 1) {
                            tmp.push(
                                { ID: 1, NAME: "Submit", icon: 'fa fa-check' },
                                { ID: 3, NAME: "Reject", icon: 'fa fa-times' },
                            );
                        }
                        else if (id > 1) {
                            tmp.push(
                                { ID: 1, NAME: "Submit", icon: 'fa fa-check' },
                                { ID: 2, NAME: "Rework", icon: 'fa fa-refresh' },
                                { ID: 3, NAME: "Reject", icon: 'fa fa-times' },
                            );
                        }
                        th.formInit.getEditor('FROM_LEVEL').option('value', id);
                        th.formInit.getEditor('APPROVE_STATUS').option('dataSource', tmp);
                        let currStatus = tmp.find(x => x.ID == apprveStatus);
                        if (!currStatus || currStatus == null || currStatus == undefined) {
                            currStatus = tmp[0];
                        }
                        th.formInit.getEditor('APPROVE_STATUS').option('value', currStatus.ID);
                        th.formInit.getEditor('REMARKS').option('value', "OK");
                    };
                    th.setLevels = function (status) {
                        let transLevel = th._transLevel;
                        let nexLevel = transLevel + 1;
                        let lastLevl = th._lastLevel;
                        //let worflowLevels = th.workflowLevel;
                        let tmp = [];
                        switch (status) {
                            case 1:
                                let levelTitle = "Next Level ";
                                //let levelItem = worflowLevels.find(c => c.LEVEL_ID == nexLevel);
                                //if (levelItem != undefined && levelItem != null)
                                //{
                                //    levelTitle = levelItem.REMARKS;
                                //}
                                if (transLevel < lastLevl) {
                                    tmp.push({ ID: nexLevel, NAME: levelTitle, icon: 'fa fa-check' });
                                } else if (transLevel == lastLevl) {
                                    //nexLevel = nexLevel - 1;
                                    tmp.push({ ID: nexLevel - 1, NAME: "Approve Level", icon: 'fa fa-check' });
                                }
                                //console.log(transLevel)
                                break;
                            case 2:
                                transLevel = th._actualTransLevel;
                                //console.log(transLevel, th._actualTransLevel)
                                for (var i = transLevel - 1; i > -1; i--) {
                                    tmp.push({ ID: i, NAME: `Level ${i + 1}`, icon: 'fa fa-chevron-right' });
                                }
                                break;
                            case 3:
                                tmp.push({ ID: 0, NAME: "Reject", icon: 'fa fa-check' });
                                break;
                        };
                        th.formInit.getEditor('TO_LEVEL').option('dataSource', tmp);
                        setTimeout(() => {
                            th.formInit.getEditor('TO_LEVEL').option('value', tmp[0].ID);
                        });
                    };
                    th.submit = function (e) {
                        let validate = th.formInit.validate();
                        if (validate != undefined || validate != null) {
                            if (!validate.isValid) {
                                return false;
                            }
                        }
                        //th.submit();
                        var status = true;
                        let data = th.formInit.option('formData');
                        data.REMARKS = data.REMARKS.trim();

                        if (data.REMARKS.length <= 0) {
                            showAlert(`NO REMARKS`);
                            return false;
                        }
                        //console.log(data)
                        let result = th.docData;
                        let overlapConditions = [];
                        if (data.APPROVE_STATUS == 1) {
                            var nextLevel = th._actualTransLevel + 1;
                            //check if next level is not the user level
                            if (nextLevel != th._userLevel && th._userLevel > nextLevel && th._actualTransLevel != 0) {
                                //check if not the user level, check if he can overlap in between levels
                                var levelsInfo = th.workflowLevel.filter(x =>
                                    x.LEVEL_ID > nextLevel &&
                                    x.LEVEL_ID < th._userLevel);
                                if (levelsInfo != undefined && levelsInfo.length > 0) {
                                    console.log(levelsInfo);
                                    $.each(levelsInfo, function (i, level) {
                                        if (level.NO_OVERLAP == true && level.NO_OVERLAP_CONDITION == null) {
                                            status = false;
                                            showAlert(`You cann't overlap level ${level.LEVEL_ID}`);
                                        } else if (level.NO_OVERLAP == true && level.NO_OVERLAP_CONDITION != null) {
                                            overlapConditions.push(level.NO_OVERLAP_CONDITION);
                                        }
                                    });
                                }
                            }
                        }

                        if (status == true) {
                            result.TRANSACTION_ID = th._transId;
                            result.FROM_LEVEL = data.FROM_LEVEL;
                            result.APPROVE_STATUS = data.APPROVE_STATUS;
                            result.TO_LEVEL = data.TO_LEVEL;
                            result.REMARKS = data.REMARKS;
                            result.NO_OVERLAP_CONDITIONS = overlapConditions;
                            result.USER_LEVEL = th._userLevel;
                            //console.log(result,'----')
                            //debugger;
                            result = requestParam(result);
                            showBasicLoader(true);
                            $.post(`/api/approve/unique`, result).then((res) => {
                                console.log(res)
                                showBasicLoader(false);
                                if (res.status) {
                                    showIndicator(res.feedback, 'success');
                                    th.hidePopup();
                                    th.result(result.TRANSACTION_ID);
                                } else {
                                    showAlert(res.feedback);
                                }
                            }).fail(function (error) {
                                console.error(error)
                                showBasicLoader(false);
                                showAlert(error.responseText);
                                //showAlert(error.feedback);
                                showIndicator(error.feedback, 'error');
                            });
                        }
                    };
                },
                colCount: 3,
                focusStateEnabled: true,
                scrollingEnabled: true,
                showRequiredMark: false,
                showColonAfterLabel: false,
                scrollingEnabled: true,
                formData: {},
                labelMode: "floating",
                labelLocation: 'top',
                items: [
                    {
                        dataField: 'FROM_LEVEL',
                        label: {
                            text: 'Level'
                        },
                        editorOptions: {
                            readOnly: true,
                        },
                        validationRules: [{ type: 'required' }],
                    },
                    {
                        dataField: 'APPROVE_STATUS',
                        label: {
                            text: 'APPROVE STATUS'
                        },
                        editorType: 'dxSelectBox',
                        editorOptions: {
                            displayExpr: 'NAME',
                            valueExpr: 'ID',
                            itemTemplate(data, index, element) {
                                return "<span>" + data.NAME + " <span class='" + data.icon + "'></span></span>";
                            },
                            buttons: [
                                {
                                    name: 'appStatusBbtn',
                                    options: {
                                        elementAttr: { class: cellColors.customSuccessBtn },
                                        icon: 'search',
                                    },
                                }
                            ],
                        },
                        validationRules: [{ type: 'required' }],

                    },
                    {
                        dataField: 'TO_LEVEL',
                        label: {
                            text: 'TO LEVEL'
                        },
                        editorType: 'dxSelectBox',
                        editorOptions: {
                            displayExpr: 'NAME',
                            valueExpr: 'ID',
                            buttons: [
                                {
                                    name: 'appStatusBbtn',
                                    options: {
                                        elementAttr: { class: cellColors.customSuccessBtn },
                                        icon: 'search',
                                    },
                                }
                            ],
                        },
                        validationRules: [{ type: 'required' }],
                    },
                    {
                        dataField: 'REMARKS',
                        label: {
                            text: 'REMARKS',
                        },
                        colSpan: 3,
                        editorType: 'dxTextArea',
                        editorOptions: {
                            height: 90,
                            showClearButton: true,
                            onKeyDown(e) {
                                let event = e.event;
                                if (event.ctrlKey && event.key === "s") {
                                    event.preventDefault();
                                    th.submit();
                                }
                            },
                        },
                        //validationRules: [{ type: 'required' }],
                    },
                    {
                        dataField: 'CREATED_BY',
                        editorOptions: {
                            readOnly: true,
                        },
                    },
                    {
                        dataField: 'CREATED_AT',
                        editorType: 'dxDateBox',
                        editorOptions: {
                            displayFormat: _dateFormat,
                            width: '100%',
                            readOnly: true,
                        },
                    },
                ],
            });

            //
            $(`#${doc}submitLogs`).dxList({
                onInitialized(e) {
                    th.dxListInit = e.component;
                },
                elementAttr: { class: 'top-5px transaction-box' },
                itemTemplate(data, itemIndex, elelment) {
                    let div = $(`<div>`).addClass(`transaction-submit-content`).addClass(`approve-${data.APPROVE_STATUS}`).append(
                        $(`<img class="dataGrid-row-logo" src="/upload/${data.PROFILE}" onError="userIconstandby(this)" />`),
                        $('<span>').addClass('comment-area').text(`${data.USER_EMAIL}`),
                        $(`<span style='float:right;'>`).append(
                            $('<span>').text(`${data.STATUS}`),
                            /*  $('<span>').dxButton({
                                  visible() {
                                      return _sgId == 1 ? true : false;
                                  },
                                  icon: 'trash',
                                  type: 'danger',
                                  stylingMode:'text',
                                  onClick() {
                                      editConfirm('APPROVAL INFO', `REMOVE COMMIT`, 'default')
                                          .show().done(function (dialogResult) {
                                              if (dialogResult) {
                                                  //console.log(th.transDoc, th._transId, th._transLevel);
                                                  showBasicLoader(true);
                                                  $.post(`${apis.approval}approvalDetail?key=${data.ID}&values=`).done((res) => {
                                                      showBasicLoader(false);
                                                      if (res.status) {
                                                          showIndicator(res.feedback);
                                                          //th.dxListInit.
                                                      }
                                                      console.log(res)
                                                  }).fail();
                                              }
                                          });
                                  }
                              })*/
                        ),

                    );
                    let div2 = $('<div>').addClass(`transaction-submit-footer`).append(
                        $(`<label>${data.CREATED_AT} </label>`),
                        $(`<label style='margin-left:15px;'> ${data.REMARKS} </label>`)
                            .addClass('comment-box-area'),
                    );
                    $(elelment).append(div);
                    $(elelment).append(div2);
                },
                activeStateEnabled: false,
                focusStateEnabled: false,
                selectionMode: "none",
                allowItemDeleting: false,
            });
        });
    }
};
class transComments {
    popup = {
        showCloseButton: true,
        focusStateEnabled: true,
        wrapperAttr: true,
        hideOnOutsideClick: false,
        deferRendering: false,
        position: 'center',
        dragOutsideBoundary: true,
        resizeEnabled: true,
        restorePosition: true,
        shading: true,
        shadingColor: 'rgba(0,0,0,0.5)',
        title: '',
    };
    form = {
        colCount: 1,
        focusStateEnabled: true,
        scrollingEnabled: true,
        showRequiredMark: false,
        showColonAfterLabel: false,
        scrollingEnabled: true,
        formData: {},
        labelMode: "floating",
        labelLocation: 'top',
        items: [
            {
                dataField: 'REMARKS',
                label: {
                    text: 'REMARKS',
                },
                editorType: 'dxTextArea',
                editorOptions: {
                    height: 90,
                    showClearButton: true,
                },
                validationRules: [{ type: 'required' }],
            },
        ],
    };
    transList = {
        elementAttr: { class: 'top-5px transaction-box' },
        itemTemplate(data, itemIndex, elelment) {
            let div = $(`<div>`).addClass(`transaction-submit-content`).append(
                $(`<img class="dataGrid-row-logo" src="/upload/${data.PROFILE}" onError="userIconstandby(this)" />`),
                $('<span>').addClass('comment-area').text(`${data.USER_EMAIL}`),
            );
            let div2 = $('<div>').addClass(`transaction-submit-footer`).append(
                $(`<label><u>Created</u> <b>At</b> ${data.CREATED_AT} </label>`),
                $(`<label style='margin-left:15px;'><b>By</b> ${data.REMARKS} </label>`),
                //$(`<label><u>Updated</u> <b>At</b> ${data.CREATED_AT} </label>`),
                //$(`<label style='margin-left:15px;'><b>By</b> ${data.USER_EMAIL} </label>`),
            );
            $(elelment).append(div);
            $(elelment).append(div2);
        },
        activeStateEnabled: false,
        focusStateEnabled: false,
        selectionMode: "none",
        allowItemDeleting: false,
    };
    approvalList = {
        elementAttr: { class: 'top-5px transaction-box' },
        itemTemplate(data, itemIndex, elelment) {
            let div = $(`<div>`).addClass(`transaction-submit-content`).append(
                $(`<img class="dataGrid-row-logo" src="/upload/${data.PROFILE}" onError="userIconstandby(this)" />`),
                $('<span>').addClass('comment-area').text(`${data.USER_EMAIL}`),
            );
            let div2 = $('<div>').addClass(`transaction-submit-footer`).append(
                $(`<label>${data.CREATED_AT} </label>`),
                $(`<label style='margin-left:15px;'> ${data.REMARKS} </label>`),
            );
            $(elelment).append(div);
            $(elelment).append(div2);
        },
        activeStateEnabled: false,
        focusStateEnabled: false,
        selectionMode: "none",
        allowItemDeleting: false,
    };
    commentsList = {
        elementAttr: { class: 'top-5px transaction-box' },
        itemTemplate(data, itemIndex, elelment) {
            let div = $(`<div>`).addClass(`transaction-submit-content`).append(
                $(`<img class="dataGrid-row-logo" src="/upload/${data.PROFILE}" onError="userIconstandby(this)" />`),
                $('<span>').addClass('comment-area').text(`${data.USER_EMAIL}`),
            );
            let div2 = $('<div>').addClass(`transaction-submit-footer`).append(
                $(`<label>${data.CREATED_AT} </label>`),
                $(`<label style='margin-left:15px;'> ${data.REMARKS} </label>`),
            );
            $(elelment).append(div);
            $(elelment).append(div2);
        },
        activeStateEnabled: false,
        focusStateEnabled: false,
        selectionMode: "none",
        allowItemDeleting: false,
    };
    constructor() {
        let _popup = this.popup;
        let _form = this.form;
        let _transList = this.transList;
        let _approvalList = this.approvalList;
        let _commentsList = this.commentsList;
        let th = this;
        _popup.onInitialized = function (e) {
            let init = e.component;
            th.popupInit = e.component;
            th.hidePopup = function () {
                init.hide();
            };
            th.showPopup = function (show = true) {
                if (show == undefined) {
                    show = true;
                }
                init.option('toolbarItems[1].options.visible', show)
                init.show();
            };
            th.popupTitle = function (title) {
                init.option('title', title);
            };
            init.option('toolbarItems',
                [
                    {
                        location: "after",
                        toolbar: "top",
                        widget: 'dxButton',
                        options: {
                            icon: 'fa-solid fa-expand',
                            elementAttr: { class: "management-toolbar-btn" },
                            onClick(e) {
                                init.option('fullScreen', !init.option('fullScreen'));
                            }
                        }
                    },
                    {
                        location: "after",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            icon: 'save',
                            text: 'Save',
                            type: 'default',
                            stylingMode: "outlined",
                            onClick() {
                                let validate = th.formInit.validate();
                                if (validate.isValid) {
                                    th.submit();
                                }

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
                            stylingMode: "outlined",
                            onClick() {
                                init.hide();
                            }
                        }
                    },
                ]
            );
        };
        _form.onInitialized = function (e) {
            th.formInit = e.component;
            th.startProcess = function (doc, transId) {
                th._transId = transId;
                th._transDoc = doc;
                th.formInit.option('formData', { DOC_NAME: doc, DOC_ID: transId });
                setTimeout(() => {
                    th.getHistory(doc, transId);
                });
            };
            th.submit = function (e) {
                let data = th.formInit.option('formData');
                data = requestParam(data);
                showBasicLoader(true);
                $.post(`/api/comments`, data).then((res) => {
                    showBasicLoader(false);
                    if (res.status) {
                        showIndicator(res.feedback, 'success');
                        th.formInit.option('formData', { REMARKS: null });
                        let data = th.commentsListInit.option('dataSource');
                        let item = res.obj[0];
                        data.unshift(item);
                        setTimeout(() => {
                            th.commentsListInit.option('dataSource', data);
                        });
                    } else {
                        showIndicator(res.feedback, 'error');
                    }
                }).fail(function (error) {
                    showBasicLoader(false);
                    showIndicator(error.feedback, 'error');
                });
            };
            th.getHistory = function (docName, id) {
                showBasicLoader(true);
                //console.log(docName, id)
                $.get(`/api/comments/${docName}/${id}`).then((res) => {
                    showBasicLoader(false);
                    th.transListInit.option('dataSource', res.TRANS_REMARK);
                    th.approvalListInit.option('dataSource', res.APPROVAL_REMARKS);
                    th.commentsListInit.option('dataSource', res.COMMENTS);
                    setTimeout(() => {
                        th.showPopup();
                    });
                });
            };
        };
        _transList.onInitialized = function (e) {
            th.transListInit = e.component;
        };
        _approvalList.onInitialized = function (e) {
            th.approvalListInit = e.component;
        };
        _commentsList.onInitialized = function (e) {
            th.commentsListInit = e.component;
        };
    }
};
class transDocuments {
    popup = {
        showCloseButton: true,
        focusStateEnabled: true,
        wrapperAttr: true,
        hideOnOutsideClick: false,
        deferRendering: false,
        position: 'center',
        dragOutsideBoundary: true,
        resizeEnabled: true,
        restorePosition: true,
        shading: true,
        shadingColor: 'rgba(0,0,0,0.5)',
        title: '',
    };
    fileLoader = {
        selectButtonText: 'Select Document',
        //accept: "image/*",
        accept: "*",
        multiple: false,
        uploadMode: 'useForm',
    }
    form = {
        colCount: 1,
        scrollingEnabled: true,
        showRequiredMark: false,
        showColonAfterLabel: false,
        scrollingEnabled: true,
        formData: {},
        labelMode: "floating",
        labelLocation: 'top',
        items: [
            {
                itemType: 'group',
                colSpan: 1,
                colCount: 2,
                items: [
                    {
                        dataField: 'DOC_TYPE',
                        editorType: 'dxSelectBox',
                        editorOptions: {
                            valueExpr: 'ID',
                            displayExpr: 'NAME',
                        },
                        validationRules: [{ type: 'required' }],
                    },
                    {
                        dataField: 'REMARKS',
                        label: {
                            text: 'REMARKS',
                        },
                        editorType: 'dxTextArea',
                        editorOptions: {
                            height: 35,
                            showClearButton: true,
                        },
                        validationRules: [{ type: 'required' }],
                    },
                ]
            },
            {
                dataField: 'FILE',
                template: 'fileLoaderTemplate',
                //validationRules: [{ type: 'required' }],
            },
        ],
    };
    dataGrid = {
        elementAttr: { class: 'top-5px' },
        noDataText: "No Documents",
        activeStateEnabled: true,
        focusStateEnabled: true,
        focusedRowEnabled: true,
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
            visible: false,
            width: 240,
            placeholder: 'Search...',
        },
        showColumnLines: true,
        showRowLines: true,
        columnMinWidth: 50,
        columnAutoWidth: true,
        columnHidingEnabled: false,
        remoteOperations: true,
        editing: {
            mode: 'row',
            allowUpdating: false,
            allowAdding: false,
            allowDeleting: false,
            useIcons: true,
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
                caption: 'NO',
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
                    let cell = options.row.data;
                    $(container).append('<a href="/TrnDocuments/' + cell.UNIQUE_NAME + '"  target="_blank" download><span class="glyphicon glyphicon-download-alt"></span></a>');
                },
                alignment: 'center',
            },
            {
                dataField: 'UNIQUE_NAME2',
                caption: 'Open',
                width: 120,
                cellTemplate(container, options) {
                    let cell = options.row.data;
                    $(container).append('<a href="/TrnDocuments/' + cell.UNIQUE_NAME + '"  target="_blank" ><span class="dx-icon-folder"></span></a>');
                },
                alignment: 'center',
            },
            {
                dataField: 'CREATED_AT',
                caption: 'UPLOADED AT',
                caption: 'Date',
                dataType: 'date',
                format: _dateFormat,
                alignment: 'center',
                width: 200,
            },
        ],
    };
    constructor() {
        let _popup = this.popup;
        let _form = this.form;
        let _dataGrid = this.dataGrid;
        let _fileLoader = this.fileLoader;
        let th = this;
        _fileLoader.onInitialized = function (e) {
            th.fileInit = e.component;
        };
        _popup.onInitialized = function (e) {
            let init = e.component;
            th.popupInit = e.component;
            th.hidePopup = function () {
                init.hide();
            };
            th.showPopup = function (show = true) {
                if (show == undefined) {
                    show = true;
                }
                init.option('toolbarItems[1].options.visible', show)
                init.show();
            };
            th.popupTitle = function (title) {
                init.option('title', title);
            };
            init.option('toolbarItems',
                [
                    {
                        location: "after",
                        toolbar: "top",
                        widget: 'dxButton',
                        options: {
                            icon: 'fa-solid fa-expand',
                            elementAttr: { class: "management-toolbar-btn" },
                            onClick(e) {
                                init.option('fullScreen', !init.option('fullScreen'));
                            }
                        }
                    },
                    {
                        location: "after",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            icon: 'save',
                            text: 'Save',
                            type: 'default',
                            stylingMode: "outlined",
                            onClick() {
                                let validate = th.formInit.validate();
                                if (validate.isValid) {
                                    th.submit();
                                }

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
                            stylingMode: "outlined",
                            onClick() {
                                init.hide();
                            }
                        }
                    },
                ]
            );
        };
        _form.onInitialized = function (e) {
            th.formInit = e.component;
            th.startProcess = function (doc, transId) {
                th.getTypes(doc);
                th._transId = transId;
                th.transDoc = doc;
                th.formInit.option('formData', { DOC_NAME: doc, DOC_ID: transId });
                setTimeout(() => {
                    th.getHistory(doc, transId);
                });
            };
            th.getTypes = function (docName) {
                $.get(`/api/documents/${docName}/doctypes`).then((res) => {
                    th.formInit.getEditor('DOC_TYPE').option('dataSource', res);
                });
            };
            th.submit = function (e) {
                let fd = new FormData();
                let data = th.formInit.option('formData');
                data.FILE = th.fileInit.option('value');
                data = requestParam(data);
                let files = data.FILE[0];
                fd.append('File', files);
                fd.append('REMARKS', data.REMARKS);
                fd.append('DOC_ID', data.DOC_ID);
                fd.append('DOC_NAME', data.DOC_NAME);
                fd.append('DOC_TYPE', data.DOC_TYPE);
                fd.append('ORG_ID', data.ORG_ID);
                fd.append('COMP_ID', data.COMP_ID);
                fd.append('CREATED_BY', data.CREATED_BY);
                fd.append('UPDATED_BY', 0);
                fd.append('DELETED_BY', 0);
                fd.append('IS_DELETED', 0);
                fd.append('IS_UPDATED', 0);
                showBasicLoader(true);
                $.ajax({
                    url: '/Pm_Transactions/uploadDocument',
                    type: 'post',
                    data: fd,
                    contentType: false,
                    processData: false,
                    success: function (response) {
                        showBasicLoader(false);
                        if (response.status) {
                            showIndicator(response.feedback, 'success');
                            let item = response.obj[0];
                            let data = th.gridInit.option('dataSource');
                            data.unshift(item);
                            setTimeout(() => {
                                th.formInit.getEditor('REMARKS').option('value', null);
                                th.gridInit.option('dataSource', data);
                                th.gridInit.option('focusedRowKey', item.ID);
                            });
                        } else {
                            showIndicator(response.feedback, 'error');
                        }
                    },
                    error: function (request, status, error) {
                        showError(request.data);
                        showBasicLoader(false);
                        serverErrorHandler(status, 'fetch forms');
                    }
                });
            };
            th.getHistory = function (docName, id) {
                showBasicLoader(true);
                debugger
                console.log(docName)
                //th.gridInit.option('dataSource', []);
                $.get(`/api/documents/${docName}/${id}`).done((res) => {
                    showBasicLoader(false);
                    th.gridInit.option('dataSource', res);
                    setTimeout(() => {
                        th.showPopup();
                    });
                }).fail(() =>
                {
                    //'/api/documents/owner/' + docName + "/" + docId
                });
            };
        };
        _dataGrid.onInitialized = function (e) {
            th.gridInit = e.component;
        };
    }
};
class popoverList {
    popover = {
        showEvent: 'click',
        visible: false,
        hideOnOutsideClick: true,
        showCloseButton: true, 
        hoverStateEnabled: false,
        showTitle: false,
        title: 'Notifications',
        width: 500,
        target: null,
        wrapperAttr: {class: 'control-panel-popup chat-popup notification-popover' },
    };
    list = {
        dataSource: {
            store: new DevExpress.data.CustomStore({
                key: "KEY_ID",
                loadMode: "raw",
                load() {
                    let config = getConfig();
                    return $.get(apis.notification, config, (res) =>
                    {
                        notifButton.option(`text`, res.length)
                    }); 
                },
                remove(key)
                {
                    return {};
                }
            })
        }, 
        keyExpr: 'KEY_ID',
        valueExpr: 'MSG',
        searchEnabled: false,
        searchExpr: 'MSG',
        activeStateEnabled: false,
        focusStateEnabled: false,
        selectionMode: "none",
        selectByClick: true,
        allowItemDeleting: true,
        hoverStateEnabled: false,
        indicateLoading: true,
        itemDeleteMode: "swipe",
        showScrollbar: "always",
        noDataText: 'No Notification', 
        height: 600,
        wrapperAttr: { id: 'notification-list' },
    };
    constructor(title = null, target = null,template = null) {
        let th = this; 
        th.popover.onInitialized = function (e) {
            let compo = e.component;
            th.popoverInit = compo;
            if (title != null) {
                compo.option(`title`,title);
            };
            if (target != null) {
                compo.option(`target`, target);
            };
            if (template != null) {
                compo.option(`contentTemplate`, template);
            };
            compo.option(`toolbarItems`, [
                {
                    location: 'after',
                    widget: 'dxButton',
                    options: {
                        icon: 'minus',
                        elementAttr: { class: "management-toolbar-btn" },
                        onClick(e)
                        {
                            compo.hide();
                        },
                    },
                },
            ]);
            compo.option(`onShowing`, () =>
            {
                th.listInit.reload();
            });
        };
        th.list.onInitialized = function (e) {
            th.listInit = e.component;
            th.listInit.option(`itemTemplate`, (data, itemIndex, elelment) =>
            {
                const typeMap = {
                    APPROVE:  { icon: 'fa-circle-check',  badge: '#2a9d3e', cls: 'notif-type-approve' },
                    SUBMIT:   { icon: 'fa-paper-plane',    badge: '#0d5a88', cls: '' },
                    COMMENT:  { icon: 'fa-comment',        badge: '#7a8a90', cls: 'notif-type-comment' },
                    REJECT:   { icon: 'fa-circle-xmark',   badge: '#c83333', cls: 'notif-type-reject' },
                    REWORK:   { icon: 'fa-rotate',         badge: '#e8873a', cls: 'notif-type-rework' },
                };
                const t = typeMap[data.NOTI_TYPE] || { icon: 'fa-bell', badge: '#0d5a88', cls: '' };
                const isUnread = !data.IS_READ;
                const relTime = moment(data.CREATED_AT).fromNow();

                const card = $(`<div>`).addClass(`notif-card${isUnread ? ' notification-unread' : ''}`);

                card.append(
                    $('<div>').addClass(`notif-icon-circle ${t.cls}`).append(
                        $('<i>').addClass(`fa-solid ${t.icon}`)
                    ),
                    $('<div>').addClass('notif-body').append(
                        $('<div>').addClass('notif-title-row').append(
                            $('<span>').addClass('notif-title').text(`${data.TITLE} - ${data.CONTENT}`)
                        ),
                        $('<div>').addClass('notif-sub-row').append(
                            $('<span>').addClass('notif-date').text(relTime + ' \u00B7 ' + data.USER_NAME)
                        )
                    ),
                    $('<span>').addClass('notif-badge').css('color', t.badge).text(data.NOTI_TYPE)
                );

                card.click(() =>
                {
                    kzApproval.open([{ docType: data.DOC_TYPE, transId: data.ID, branchId: data.BRANCH_ID }], null,);
                    th._read(data.KEY_ID);
                }).appendTo(elelment);
            })
        };
        th.open = (elementTarget) =>
        {
            if (elementTarget)
            {
                let _listInit = th.listInit;
                if (_listInit != undefined && _listInit != null)
                {
                    th.listInit.reload();
                };
                th.popoverInit.show(elementTarget);
                
            }
        };
        th._read = (id) =>
        {
            console.log(id)
        }
    }
}
class multiViewContainer {
    multiView = {
        //selectedIndex: 1,
        loop: false,
        animationEnabled: true,
        swipeEnabled: false,
        itemTemplate: 'multi-view-tmplate',
    };
    popup = {
        showCloseButton: true,
        focusStateEnabled: true,
        wrapperAttr: true,
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
        //height: 'auto',
    };
    constructor(levels = 1, showSubmit = false) {
        let x = this.popup;
        let view = this.multiView;
        let th = this;
        th.lastLevel = levels - 1;
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
                            icon: 'fa-solid fa-minus',
                            elementAttr: { class: "management-toolbar-btn" },
                            onClick(e) {
                                compo.option('visible', false);
                            }
                        }
                    },
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
                        location: "before",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            icon: 'fa-solid fa-chevron-left',
                            text: 'Pre',
                            disabled: true,
                            onInitialized(e) {
                                th.preBtnInit = e.component;
                            },
                            onClick() {
                                th.preStep();
                            }
                        }
                    },
                    {
                        location: "before",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            icon: 'fa-solid fa-chevron-right',
                            text: 'Next',
                            type: 'normal',
                            onInitialized(e) {
                                th.nextBtnInit = e.component;
                            },
                            onClick() {
                                th.nextStep();
                            }
                        }
                    },
                    {
                        location: "after",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            icon: 'save',
                            text: 'Save',
                            type: 'default',
                            stylingMode: "outlined",
                            visible: showSubmit,
                            onInitialized(e) {
                                th.submitBtnInit = e.component;
                            },
                            onClick() {
                                let validate = th.formInit.validate();
                                if (validate == undefined) {
                                    th.submit();
                                } else {
                                    if (validate.isValid) {
                                        th.submit();
                                    }
                                };
                            }
                        }
                    },
                    {
                        location: "after",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            icon: 'close',
                            text: 'Close',
                            type: 'default',
                            stylingMode: "outlined",
                            onClick() {
                                compo.option('visible', false);
                            }
                        }
                    },
                ]);
        };
        view.onInitialized = function (e) {
            let viewInit = e.component;
            th.viewInit = viewInit;
            th.disablePrevious = function () {
                th.preBtnInit.option('disabled', true);
            };
            th.enablPrevious = function () {
                th.preBtnInit.option('disabled', false);
            };
            th.resetBtns = async function (direction) {
                let submit = false;
                let selectedIndex = viewInit.option('selectedIndex');
                if (direction === 'next') {
                    selectedIndex += 1;
                    if (selectedIndex == th.lastLevel) {
                        th.nextBtnInit.option('text', 'Submit');
                        th.nextBtnInit.option('icon', 'save');
                    } else if (selectedIndex == th.lastLevel + 1) {
                        submit = true;
                    };
                    th.preBtnInit.option('disabled', false);
                } else {
                    selectedIndex -= 1;
                    if (selectedIndex == 0) {
                        th.preBtnInit.option('disabled', true);
                    };
                    th.nextBtnInit.option('text', 'Next');
                    th.nextBtnInit.option('icon', 'fa-solid fa-chevron-right');
                }
                viewInit.option('selectedIndex', selectedIndex);
                return {
                    status: submit,
                    level: selectedIndex,
                };
            };

            th.showNextBtn = async function (visible = false) {
                th.nextBtnInit.option('visible', visible);
            };
            th.showSubmit = async function (visible = false) {
                th.submitBtnInit.option('visible', visible);
            };
        }
    }
};
class tabPanelPopup {
    tabPanel = {
        swipeEnabled: true,
        animationEnabled: true,
        repaintChangesOnly: true,
        scrollingEnabled: true,
        scrollByContent: true,
        activeStateEnabled: true,
        hoverStateEnabled: true,
        showNavButtons: true,
        focusStateEnabled: false,
        selectedIndex: -1,
    };
    popup = {
        showCloseButton: false,
        focusStateEnabled: true,
        wrapperAttr: true,
        hideOnOutsideClick: false,
        deferRendering: false,
        position: 'center',
        dragOutsideBoundary: true,
        resizeEnabled: true,
        restorePosition: true,
        shading: false,
        shadingColor: 'rgba(0,0,0,0.5)',
        title: '',
        //copyRootClassesToWrapper: true,
        wrapperAttr: true,
        wrapperAttr: { class: 'form-tab-panel-popup' },
    };
    constructor(showSubmit = false) {
        let th = this;
        let panel = this.tabPanel;
        let pop = this.popup;
        panel.onInitialized = function (e) {
            th._init = e.component;
        };
        pop.onInitialized = function (e) {
            let init = e.component;
            th.hidePopup = function () {
                init.option('visible', false);
            };
            th.showPopup = function () {
                init.option('visible', true);
            };
            th.open = function (title) {
                th.popupTitle(title);
                th.showPopup();
            };
            th.popupTitle = function (title) {
                init.option('title', title);
            };
            init.option('toolbarItems',
                [
                    {
                        location: "after",
                        toolbar: "top",
                        widget: 'dxButton',
                        options: {
                            icon: 'fa-solid fa-minus',
                            elementAttr: { class: "management-toolbar-btn" },
                            onClick(e) {
                                th._isMinimized = true;
                                init.option('visible', false);
                            }
                        }
                    },
                    {
                        location: "after",
                        toolbar: "top",
                        widget: 'dxButton',
                        options: {
                            icon: 'fa-solid fa-expand',
                            elementAttr: { class: "management-toolbar-btn" },
                            onClick(e) {
                                init.option('fullScreen', !init.option('fullScreen'));
                            }
                        }
                    },
                    {
                        location: "after",
                        toolbar: "top",
                        widget: 'dxButton',
                        options: {
                            icon: 'dx-icon dx-icon-close',
                            elementAttr: { class: "management-toolbar-btn" },
                            onClick(e) {
                                th._isMinimized = false;
                                init.option('visible', false);
                            }
                        }
                    },
                    {
                        location: "before",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            icon: 'save',
                            text: 'Save',
                            elementAttr: { class: 'custom-success-btn' },
                            visible: showSubmit,
                            onClick() {
                                let validate = th.formInit.validate();
                                if (validate == undefined) {
                                    th.submit();
                                } else {
                                    if (validate.isValid) {
                                        th.submit();
                                    }
                                }

                            },
                        }
                    },
                    {
                        location: "after",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            text: 'Close',
                            elementAttr: { class: 'custom-close-btn' },
                            onClick() {
                                th._isMinimized = false;
                                init.option('visible', false);
                            }
                        }
                    },
                ]
            );
        };
    } 
};
class transApproveFlow {
    constructor(doc) {
        
        let th = this;
        if (doc == null || doc == undefined) {
            doc = getDocName();
        };

        let elementTester = document.getElementById(`${doc}approveFlowPopup`);
        if (elementTester != null || elementTester != undefined) {
            elementTester.remove();
        }
        let newPopup = document.createElement("div");
        newPopup.setAttribute("id", `${doc}approveFlowPopup`);

        let newChart = document.createElement("div");
        newChart.setAttribute("id", `${doc}approveChart`);

        let scroll = document.createElement("div");
        scroll.setAttribute("id", `${doc}approveScroll`);
        scroll.appendChild(newChart);
        newPopup.appendChild(scroll);
        document.body.appendChild(newPopup);
        setTimeout(() => {

            $(`#${doc}approveFlowPopup`).dxPopup({
                onInitialized(e) {
                    let init = e.component;
                    th.popupInit = e.component;
                    th.hidePopup = function () {
                        init.hide();
                    };
                    th.showPopup = function (show = true) {
                        if (show == undefined) {
                            show = true;
                        }
                        init.option('toolbarItems[1].options.visible', show)
                        init.show();
                   
                    };
                    init.option('toolbarItems',
                        [
                            {
                                location: "after",
                                toolbar: "top",
                                widget: 'dxButton',
                                options: {
                                    icon: 'minus',
                                    elementAttr: { class: "management-toolbar-btn" },
                                    onClick(e) {
                                        init.hide();
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
                                        init.option('fullScreen', !init.option('fullScreen'));
                                    }
                                }
                            },
                            {
                                location: "after",
                                toolbar: "bottom",
                                widget: 'dxButton',
                                options: {
                                    icon: 'close',
                                    text: 'Close',
                                    elementAttr: { class: 'custom-close-btn' },
                                    onClick() {
                                        init.hide();
                                    }
                                }
                            },
                        ]
                    );
                },
                showCloseButton: true,
                focusStateEnabled: true,
                wrapperAttr: true,
                hideOnOutsideClick: false,
                deferRendering: false,
                position: 'center',
                dragOutsideBoundary: true,
                resizeEnabled: true,
                restorePosition: true,
                shading: true,
                shadingColor: 'rgba(0,0,0,0.5)',
                title: 'Transaction Approve Workflow',
                wrapperAttr: true,
            });
            $(`#${doc}approveScroll`).dxScrollView({ width: '100%', height: '100%' });
            // Render the status-aware workflow chart + submission history into `target`
            // (a selector or element). Shared by the popup and any inline placement
            // (e.g. the PRF transaction view box). Uses element references (no global ids)
            // so multiple charts can coexist on the page without collisions.
            th.renderFlow = function (target, docType, transId, showHistory) {
                if (showHistory === undefined) showHistory = true;
                const $target = $(target);
                $target.empty();
                return $.get(`/api/approve/flow/${docType}/${transId}`).then((res) => {
                    const LevelsList = res || [];

                    const container = $('<div class="workflow-container-head"></div>');
                    const title = $('<h2 class="workflow-title">Approval Workflow Progress</h2>');
                    const workflowChart = $('<div class="bps-flow-chart"></div>');
                    const submitHistory = $('<div class="bps-flow-history"></div>');
                    container.append(title, workflowChart, submitHistory);
                    $target.append(container);

                    const workflowContainer = $('<div class="workflow-container"></div>');
                    const progressLine = $('<div class="workflow-line"><div class="workflow-progress"></div></div>');
                    const completedCount = LevelsList.filter(l => l.STATE === 'completed').length;

                    LevelsList.forEach((level) => {
                        const stepDiv = $('<div class="workflow-step"></div>');
                        const circle = $('<div>').addClass("step-circle");
                        const dateDiv = $('<div class="step-date"></div>');
                        const label = $('<div class="step-label"></div>');
                        const status = $('<div class="step-status"></div>');

                        circle.text(level.LEVEL_ID);
                        label.text(level.NAME);

                        if (level.SUBMITTED_AT) {
                            dateDiv.html(`${level.SUBMITTED_AT}<br>By: ${level.SUBMIT_BY || '-'}`);
                        } else {
                            dateDiv.html(`-<br>Not submitted`);
                        }

                        if (level.STATE === 'completed') {
                            // Completed levels show the approver's photo (✓ / ✗ fallback)
                            var isRejected = level.APPROVE_STATUS === 3;
                            var fallback = isRejected ? '<span>✗</span>' : '<span>✓</span>';
                            circle.addClass(isRejected ? 'rejected' : 'completed');
                            if (level.SUBMIT_BY) circle.attr('title', level.SUBMIT_BY);
                            if (level.PROFILE) {
                                const avatar = $('<img class="step-avatar" alt="" />')
                                    .attr('src', 'http://hrms.fakhruddin.ae/images/employee/' + level.PROFILE)
                                    .on('error', function () { circle.html(fallback); });
                                circle.empty().append(avatar);
                            } else {
                                circle.html(fallback);
                            }
                            dateDiv.addClass('completed');
                            status.addClass(isRejected ? 'status-rejected' : 'status-completed')
                                  .text(level.STATUS_TEXT || (isRejected ? 'Rejected' : 'Completed'));
                        } else if (level.STATE === 'current') {
                            circle.addClass('current');
                            dateDiv.addClass('current');
                            status.addClass('status-current').text(level.STATUS_TEXT || 'In Progress');
                        } else {
                            circle.addClass('pending');
                            dateDiv.addClass('pending');
                            status.addClass('status-pending').text(level.STATUS_TEXT || 'Pending');
                        }

                        stepDiv.append(circle, label, dateDiv, status);
                        workflowContainer.append(stepDiv);
                    });

                    workflowContainer.append(progressLine);
                    workflowChart.html(workflowContainer);

                    // Submission history (rendered into the local element, not a global id)
                    if (showHistory) {
                        showBasicLoader(true);
                        $.get(`/api/approve/${docType}/${transId}`).then((logs) => {
                            showBasicLoader(false);
                            if (logs && logs.length > 0) {
                                submitHistory.html(th.createSubmitHistorySection(logs));
                            }
                        }).fail(() => showBasicLoader(false));
                    }

                    // Animate progress line (scoped to this chart)
                    setTimeout(() => {
                        let span = LevelsList.length > 1 ? (completedCount / (LevelsList.length - 1)) * 100 : 0;
                        if (span > 100) span = 100;
                        workflowContainer.find('.workflow-progress').css('width', span + '%');
                    }, 500);
                });
            };
            th.previewWorkflow = async function (docType, transId, transLevel, sgId) {
                th.renderFlow(`#${doc}approveChart`, docType, transId).then(() => {
                    th.showPopup();
                });
            };
            th.getHistory = function (docName, id) {
                showBasicLoader(true);
                $.get(`/api/approve/${docName}/${id}`).then((res) => {
                    showBasicLoader(false);
                    if (res.length > 0) {
                        $(`#${doc}-submitHistory`).html(th.createSubmitHistorySection(res));

                    }
                });
            };
            // Inline SVG icons per action so each step is visually scannable at a glance.
            th.historyActionIcons = {
                submitted: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="22" y1="2" x2="11" y2="13"></line><polygon points="22 2 15 22 11 13 2 9 22 2"></polygon></svg>',
                reworked: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="1 4 1 10 7 10"></polyline><path d="M3.51 15a9 9 0 1 0 2.13-9.36L1 10"></path></svg>',
                approved: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>',
                rejected: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>',
                comment: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"></path></svg>'
            };
            // Resolve the action primarily from APPROVE_STATUS (reliable), falling back to STATUS text.
            th.historyActionOf = function (item) {
                switch (Number(item.APPROVE_STATUS)) {
                    case 1: return { key: 'submitted', label: 'Submitted' };
                    case 2: return { key: 'reworked', label: 'Reworked' };
                    case 3: return { key: 'rejected', label: 'Rejected' };
                    case 4: return { key: 'approved', label: 'Approved' };
                }
                const s = (item.STATUS || '').toLowerCase();
                if (s.indexOf('rework') > -1) return { key: 'reworked', label: item.STATUS };
                if (s.indexOf('reject') > -1) return { key: 'rejected', label: item.STATUS };
                if (s.indexOf('approv') > -1) return { key: 'approved', label: item.STATUS };
                if (s.indexOf('submit') > -1) return { key: 'submitted', label: item.STATUS };
                return { key: 'comment', label: item.STATUS || 'Action' };
            };
            // Escape helper for any value that ends up in innerHTML
            th.esc = function (v) { return $('<div>').text(v == null ? '' : v).html(); };
            // Header icons per sequence type
            th.seqIcons = {
                initial: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polygon points="5 3 19 12 5 21 5 3"></polygon></svg>',
                rework: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 10 4 15 9 20"></polyline><path d="M20 4v7a4 4 0 0 1-4 4H4"></path></svg>',
                current: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>'
            };
            th.createSubmitHistorySection = function (submitHistoryList) {
                const historySection = $('<div class="submit-history"></div>');
                const historyTitle = $('<h3 class="history-title">Submission History</h3>')
                    .append($('<span class="history-count"></span>').text(submitHistoryList.length));
                const board = $('<div class="flow-board"></div>');
                const connector = '<div class="flow-connector"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12"></line><polyline points="12 5 19 12 12 19"></polyline></svg></div>';

                // The API returns logs newest-first; reverse to chronological order (step 1 = first).
                const ordered = (submitHistoryList || []).slice().reverse();

                function nameOf(item) {
                    const n = item.USER_NAME || item.NAME || item.SUBMIT_BY;
                    if (n && String(n).trim()) return n;
                    const e = item.USER_EMAIL || '';
                    return e ? e.split('@')[0] : '-';
                }
                function levelOf(item) {
                    const l = item.FROM_LEVEL;
                    return (l === 0 || l) ? Number(l) : null;
                }

                function buildNode(item) {
                    const action = th.historyActionOf(item);
                    const icon = th.historyActionIcons[action.key] || th.historyActionIcons.comment;
                    // Profile pictures are hosted on the HRMS image server (PROFILE holds the file name)
                    const avatarSrc = item.PROFILE ? 'http://hrms.fakhruddin.ae/images/employee/' + item.PROFILE : '';
                    const name = nameOf(item);
                    const email = item.USER_EMAIL || '';
                    const hasRemarks = item.REMARKS && String(item.REMARKS).trim() !== '';
                    const remarks = hasRemarks ? `“${th.esc(item.REMARKS)}”` : '—';
                    const node = $('<div class="flow-node"></div>').addClass(action.key);
                    node.html(`
                        <div class="flow-node-head">
                            <img class="flow-node-avatar" src="${avatarSrc}" onerror="userIconstandby(this)" />
                            <span class="flow-node-id">
                                <span class="flow-node-name" title="${th.esc(email)}">${th.esc(name)}</span>
                                <span class="flow-node-email">${th.esc(email)}</span>
                            </span>
                        </div>
                        <div class="flow-node-band">
                            <span class="flow-node-status">${icon}<span>${th.esc(action.label)}</span></span>
                            <span class="flow-node-date">${th.esc(item.CREATED_AT || '')}</span>
                        </div>
                        <div class="flow-node-remarks${hasRemarks ? '' : ' empty'}">${remarks}</div>
                    `);
                    return node;
                }

                // Split the chronological list into sequences: a rework ends a sequence and the
                // next action starts a new one.
                const seqs = [];
                let cur = { items: [], prevRework: null };
                ordered.forEach((item, i) => {
                    const action = th.historyActionOf(item);
                    cur.items.push({ item, step: i + 1 });
                    if (action.key === 'reworked' && i < ordered.length - 1) {
                        cur.reworkStep = i + 1;
                        seqs.push(cur);
                        cur = { items: [], prevRework: i + 1 };
                    }
                });
                if (cur.items.length) seqs.push(cur);

                // For a sequence opened by a rework, find the step it returned to: the most recent
                // earlier action at the same level the new sequence resumes on (needs FROM_LEVEL).
                function returnStepFor(seq) {
                    if (!seq.prevRework || !seq.items.length) return null;
                    const targetLevel = levelOf(seq.items[0].item);
                    if (targetLevel == null) return null;
                    for (let k = seq.prevRework - 2; k >= 0; k--) {
                        if (levelOf(ordered[k]) === targetLevel) return k + 1;
                    }
                    return null;
                }

                seqs.forEach((seq, idx) => {
                    let key, label;
                    if (idx === 0) {
                        key = 'initial';
                        label = 'Initial Submission Sequence';
                    } else {
                        key = (idx === seqs.length - 1) ? 'current' : 'rework';
                        label = key === 'current' ? 'Current Active Sequence' : 'After Rework';
                    }
                    const panel = $('<div class="flow-section"></div>');
                    panel.append(`<div class="flow-section-header ${key}">${th.seqIcons[key]}<span>${th.esc(label)}</span></div>`);
                    const row = $('<div class="flow-row"></div>');
                    seq.items.forEach((entry, j) => {
                        if (j > 0) row.append(connector);
                        row.append(buildNode(entry.item));
                    });
                    panel.append(row);
                    board.append(panel);
                });

                historySection.append(historyTitle, board);
                return historySection;
            };
        });
    }
};
