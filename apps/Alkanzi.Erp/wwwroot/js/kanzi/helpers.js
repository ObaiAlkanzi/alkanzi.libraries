function showBasicLoader(status) {
    $("#basicLoader").dxLoadPanel('option', 'visible', status);
}
const showLoader = function () {
    $("#basicLoader").dxLoadPanel('option', 'visible', true);
}
const hideLoader = function () {
    $("#basicLoader").dxLoadPanel('option', 'visible', false);
}
class menuReport {
    form = {
        colCount: 1,
        colCount: 1,
        focusStateEnabled: true,
        scrollingEnabled: true,
        showRequiredMark: false,
        showColonAfterLabel: false,
        scrollingEnabled: true,
        formData: {},
        labelMode: "float",
        labelLocation: 'top',
        items: [],
    };
    paramForm = {
        colCount: 1,
        focusStateEnabled: true,
        scrollingEnabled: true,
        showRequiredMark: false,
        showColonAfterLabel: false,
        scrollingEnabled: true,
        formData: {},
        labelMode: "float",
        labelLocation: 'top',
        items: [],
    };
    procedureForm = {
        colCount: 1,
        focusStateEnabled: true,
        scrollingEnabled: true,
        showRequiredMark: false,
        showColonAfterLabel: false,
        scrollingEnabled: true,
        formData: {},
        labelMode: "float",
        labelLocation: 'top',
        items: [],
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
        title: '',
        width: '60%',
    };
    totalParam = 0;
    start(reportId) {
        let thisClass = this;
        showLoader();
        $.get(`${apis.menuReport}filter/${reportId}/parameters`)
            .done((res) => {
                hideLoader();
                if (res.Filter.length > 0) {
                    let x = res.Filter[0];
                    thisClass.finalResult = { FILTER_ID: x.ID, HAS_PARAMS: false };
                    var _tmpFilterValue = x.FILTER_VALUE;
                    if (_tmpFilterValue != null && _tmpFilterValue != undefined)
                    {
                        thisClass.filterValue = _tmpFilterValue.toUpperCase().trim();
                    }
                    
                    thisClass.popupTitle(x.NAME);
                    thisClass.showPopup();
                    //thisClass.formInit.resetValues();
                    //thisClass.paramFormInit.resetValues();
                    thisClass.builder(res);
                    //console.log(reportId,res)
                }
                else {
                    showAlert(`Invalid Report ID: ${reportId}`);
                }
            }).fail(() => {
                hideLoader();
            });
    }
    constructor(filterCounts = 3, PararmCounts = 3) {
        let th = this;
        th.orgId = _OrgId;
        th.compId = _CompId;
        th.branchId = _BranchId;
        th.userId = _UserId;
        let pop = this.popup;
        let f = this.form;
        let _paramForm = this.paramForm;
        let _procedureForm = this.procedureForm;
        pop.onInitialized = function (e) {
            let init = e.component;
            th.popupInit = e.component;
            th.hidePopup = function () {
                init.option('visible', false);
            };
            th.showPopup = function () {
                init.option('visible', true);
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
                            icon: 'fa-regular fa-file-excel',
                            text: 'Export To Excel',
                            type: 'default',
                            stylingMode: "outlined",
                            useSubmitBehavior: true,
                            onClick() {
                                let pointer = true;
                                let validate = th.formInit.validate();
                                if (validate != undefined && validate != null) {
                                    pointer = validate.isValid;
                                }
                                if (pointer) {
                                    th.exportType = 'excel';
                                    if (th.totalParams > 0) {
                                        let validateParam = th.paramFormInit.validate();
                                        if (validateParam.isValid) {
                                            th.paramProcess();
                                        }
                                    } else {
                                        th.filterProcess();
                                    }
                                    //th.submit();
                                }
                            }
                        }
                    },
                    {
                        location: "after",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            icon: 'exportpdf',
                            text: 'Export To PDF',
                            type: 'success',
                            stylingMode: "outlined",
                            useSubmitBehavior: true,
                            onClick() {
                                let pointer = true;
                                let validate = th.formInit.validate();
                                if (validate != undefined && validate != null) {
                                    pointer = validate.isValid;
                                }
                                if (pointer) {
                                    th.exportType = 'pdf';
                                    if (th.totalParams > 0) {
                                        let validateParam = th.paramFormInit.validate();
                                        if (validateParam.isValid) {
                                            th.paramProcess();
                                        }
                                    } else {
                                        th.filterProcess();
                                    }
                                    //th.submit();
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
                                init.option('visible', false);
                            }
                        }
                    },
                ]
            );
        };
        f.onInitialized = function (e) {
            th.formInit = e.component;
            th.processor = function (filter, key, value) {
                var pointer = 0;
                var tail = 0;
                //console.log(filter,'filter')
                while (pointer > - 1) {
                    pointer = filter.indexOf('%' + key + '%', tail);
                    if (pointer > -1) {
                        filter = filter.replace('%' + key + '%', value);
                    }
                    tail = pointer + 1;
                }
                return filter;
            };
            th.builder = function (data) {
                let x = data.Filter[0];
                th.finalResult = { FILTER_ID: x.ID, HAS_PARAMS: false };
                var _tmpFilterValue = x.FILTER_VALUE;
                if (_tmpFilterValue != null && _tmpFilterValue != undefined)
                {
                    th.filterValue = _tmpFilterValue.toUpperCase().trim();
                }
               
                let filters = data.Parameters;
                th.totalFilters = filters.length;
                th.buildFilters(filters);
                th.buildProcedureParam(data.procedurePrams).then((res) => {
                    //console.log(res)
                });
                let params = data.crystalParameters;
                th.totalParams = params.length;
                th.buildParam(params);
            };
            th.buildFilters = function (data) {
                
                data.sort((a, b) => (a.SORT_NO > b.SORT_NO) ? 1 : ((b.SORT_NO > a.SORT_NO) ? -1 : 0));
                let total = data.length;
                let _disabled = false;
                let formItems = [{
                    itemType: 'group',
                    caption: 'Filters',
                    colSpan: 4,
                    colCount: 4,
                    items: []
                }];
                let item = {};
                let editor = {};
                let _width = 'auto';
                let defaultValue = {};
                let _lovArray = [];
                let _preDefinedArray = [];
                let _isParent = 0;
                let _tableQuery = null;
                let _parentId = 0;
                let _source = [];
                let _format = null;
                let fieldName = null;
                for (var i = 0; i < total; i++) {
                    _source = [];
                    _disabled = true;
                    _width = 'auto';
                    item = data[i];
                    //console.log(item)
                    _isParent = item.IS_PARENT;
                    _tableQuery = item.TABLE_QUERY;
                    _parentId = item.PARENT_ID;
                    fieldName = item.NAME.toLowerCase();
                    switch (fieldName) {
                        case "org_id":
                            defaultValue = th.orgId;
                            break;
                        case "comp_id":
                            defaultValue = th.compId;
                            break;
                        case "branch_id":
                            defaultValue = th.branchId;
                            break;
                        case "created_by":
                            defaultValue = th.userId;
                            break;
                        case "user_id":
                            defaultValue = th.userId;
                            break; 
                        case "from_date":
                            defaultValue = new Date(new Date().getFullYear(), 0, 1);
                        default:
                            _disabled = false;
                            defaultValue = item.DEFAULT_VALUE;
                            break;
                    }
                    editor = item.EDITOR_TYPE;
                    switch (item.DATA_TYPE.toLowerCase()) {
                        case 'date':
                            _format = _dateFormat;
                            if (defaultValue === 'current')
                            {
                                defaultValue = new Date(new Date().getFullYear(), 0, 1);
                            }
                            break;
                        case 'lov':
                            _lovArray.push({ name: item.NAME, value: item.LOV_ID, default: defaultValue, isParent: _isParent, parentId: _parentId });
                            break;
                        case 'defaultparams':
                            _disabled = false;
                            if (_preDefinedArray.find(c => c.value === item.PRE_DEFINED) == undefined) {
                                _preDefinedArray.push({ name: item.NAME, value: item.PRE_DEFINED, default: defaultValue });
                            }
                            break;
                        case 'boolean':
                            _source = [{ ID: 1, NAME: 'Yes' }, { ID: 0, NAME: 'No' }];
                            break;
                        default:
                            break;
                    }; 
                    if (fieldName.includes("date") ) {
                        defaultValue = th.getDataValue(fieldName, defaultValue);
                    }
                 
                    formItems[0].items.push({
                        dataField: item.NAME,
                        editorType: editor,
                        label: { text: item.LABEL_NAME, },
                        editorOptions: {
                            displayFormat: _format,
                            dataSource: _source,
                            readOnly: _disabled,
                            width: '100%',
                            value: defaultValue ,
                            //showClearButton: true,
                            valueExpr: 'ID',
                            displayExpr: 'NAME',
                            isParent: _isParent,
                            tableQuery: _tableQuery,
                            parentId: _parentId,
                            id: item.ID,
                            onSelectionChanged(e) {
                                let selecteditem = e.selectedItem;
                                let isParent = e.component.option('isParent');
                                //console.log(isParent,'isParent')
                                if (isParent === 1) {
                                    let query = e.component.option('tableQuery');
                                    let charIndex = 0;
                                    while (charIndex > -1) {
                                        charIndex = query.indexOf('%ID%');
                                        if (charIndex > -1) {
                                            query = query.replace('%ID%', selecteditem.ID);
                                        }
                                    }
                                    query = query.replace('%ID%', selecteditem.ID);
                                    query = query.replace('%COMP_ID%', th.compId);
                                    query = query.replace('%BRANCH_ID%', th.branchId);
                                    query = query.replace('%ORG_ID%', th.orgId);
                                    query = query.replace('%USER_ID%', th.userId);
                                    query = query.replace('%CREATED_BY%', th.userId);
                                    //console.log(query,'query')
                                    let id = e.component.option('id');
                                    let childList = _lovArray.filter(e => e.parentId === id);
                                    //console.log(query)
                                    $.post(`/api/lov/parentQuery/`, { feedback: query }, function (res) {
                                        for (var obj in childList) {
                                            th.formInit.getEditor(childList[obj].name).option('dataSource', res)
                                        }
                                        let paramChilds = th._lovArray.filter(e => e.parentId === id);
                                        for (var obj in paramChilds) {
                                            th.paramFormInit.getEditor(paramChilds[obj].name).option('dataSource', res)
                                        }
                                        //console.log(th._lovArray, 'th._lovArray')
                                    });
                                }
                            }
                        },
                        validationRules: [{ type: 'required' }],
                    });
                }
                th.formInit.option('items', formItems);
                if (_lovArray.length > 0) {
                    th.getLovs(_lovArray, 'filter');
                }
                if (_preDefinedArray.length > 0) {
                    th.filterPredefined(_preDefinedArray);
                }
            };
            th.getDataValue = function (fieldName, val) {
                if ((fieldName.includes("from") || fieldName.includes("start")) && fieldName.includes("date")) {
                    return new Date(new Date().getFullYear(), 0, 1);
                } else if ((fieldName.includes("end") || fieldName.includes("to")) && fieldName.includes("date")) {
                    return new Date(new Date().getFullYear(), 11, 31);
                } else if (fieldName.includes("date") || fieldName.includes("current")) {
                    return new Date();
                }  
            };
            th.buildParam = function (data) {
                //console.log(data,'params')
                data.sort((a, b) => (a.ORDER_NUM > b.ORDER_NUM) ? 1 : ((b.ORDER_NUM > a.ORDER_NUM) ? -1 : 0));
                let total = data.length;
                let _disabled = false;
                let formItems = [{
                    itemType: 'group',
                    caption: 'Parameters',
                    colSpan: 4,
                    colCount: 4,
                    items: []
                }];
                let item = {};
                let editor = {};
                let _width = 'auto';
                let _format = {};
                let defaultValue = {};
                th._lovArray = [];
                let _preDefinedArray = [];
                let _source = [];
                let _isParent = 0;
                let _tableQuery = null;
                let _parentId = 0;
                let _parentType = 1;
                let fieldName = null;
                for (var i = 0; i < total; i++) {
                    _source = [];
                    _disabled = true;
                    _width = 'auto';
                    item = data[i];
                    _isParent = item.IS_PARENT;
                    _tableQuery = item.TABLE_QUERY;
                    _parentId = item.PARENT_ID;
                    _parentType = item.PARENT_TYPE;
                    _format = null;
                    fieldName = item.NAME.toLowerCase();
                    switch (fieldName) {
                        case "org_id":
                            defaultValue = th.orgId;
                            break;
                        case "comp_id":
                            defaultValue = th.compId;
                            break;
                        case "branch_id":
                            defaultValue = th.branchId;
                            break;
                        case "created_by":
                            defaultValue = th.userId;
                            break;
                        case "user_id":
                            defaultValue = th.userId;
                            break; 
                        default:
                            _disabled = false;
                            defaultValue = item.DEFAULT_VALUE;
                            break;
                    }
                    if (fieldName.includes("date")) {
                        defaultValue = th.getDataValue(fieldName, defaultValue);
                    } 
                    editor = item.EDITOR_TYPE;
                    switch (item.DATA_TYPE.toLowerCase()) {
                        case 'date':
                            _format = _dateFormat;
                            if (defaultValue === 'current') {
                                defaultValue = new Date(new Date().getFullYear(), 0, 1);
                            }
                            break;
                        case 'lov':
                            th._lovArray.push({
                                name: item.NAME,
                                value: item.LOV_ID,
                                default: defaultValue,
                                isParent: _isParent,
                                parentId: _parentId,
                                parentType: _parentType,
                            });
                            break;
                        case 'defaultparams':
                            _disabled = false;
                            if (_preDefinedArray.find(c => c.value === item.PRE_DEFINED) == undefined) {
                                _preDefinedArray.push({ name: item.NAME, value: item.PRE_DEFINED, default: defaultValue });
                            }
                            break;
                        case 'boolean':
                            _source = [{ ID: 1, NAME: 'Yes' }, { ID: 0, NAME: 'No' }];
                            break;
                        default:
                            break;
                    }
                    //console.log(item,'item')
                    formItems[0].items.push({
                        dataField: item.NAME,
                        editorType: editor,
                        label: {
                            text: item.DISPLAY_NAME,
                        },
                        editorOptions: {
                            dataSource: _source,
                            subReportName: item.SUB_REPORT_NAME,
                            readOnly: _disabled,
                            width: _width,
                            value: defaultValue,
                            //showClearButton: true,
                            valueExpr: 'ID',
                            displayExpr: 'NAME',
                            format: _format,
                            displayFormat: _format,
                            isParent: _isParent,
                            tableQuery: _tableQuery,
                            parentId: _parentId,
                            id: item.ID,
                            parentType: _parentType,
                            onSelectionChanged(e) {
                                let selecteditem = e.selectedItem;
                                let isParent = e.component.option('isParent');
                                //let parentType = e.component.option('parentType');
                                if (isParent === 1) {
                                    let query = e.component.option('tableQuery');
                                    let charIndex = 0;
                                    while (charIndex > -1) {
                                        charIndex = query.indexOf('%ID%');
                                        if (charIndex > -1) {
                                            query = query.replace('%ID%', selecteditem.ID);
                                        }
                                    }
                                    query = query.replace('%ID%', selecteditem.ID);
                                    query = query.replace('%COMP_ID%', th.compId);
                                    query = query.replace('%BRANCH_ID%', th.branchId);
                                    query = query.replace('%ORG_ID%', th.orgId);
                                    query = query.replace('%USER_ID%', th.userId);
                                    query = query.replace('%CREATED_BY%', th.userId);
                                    //console.log(query)
                                    let id = e.component.option('id');
                                    let childList = th._lovArray.filter(e => e.parentId === id);
                                    $.post(`/api/lov/parentQuery/`, { feedback: query }, function (res) {
                                        for (var obj in childList) {
                                            th.paramFormInit.getEditor(childList[obj].name).option('dataSource', res);
                                        }
                                    });
                                }
                            }
                        },
                        validationRules: [{ type: 'required' }],
                    });
                }
                th.paramFormInit.option('items', formItems);
                if (th._lovArray.length > 0) {
                    th.getLovs(th._lovArray, 'param');
                }
                if (_preDefinedArray.length > 0) {
                    th.paramsPredefined(_preDefinedArray);
                }
            };
            th.buildProcedureParam = async function (data)
            {
                
                data.sort((a, b) => (a.SORT_NO > b.SORT_NO) ? 1 : ((b.SORT_NO > a.SORT_NO) ? -1 : 0));
                let total = data.length;
                let _disabled = false;
                let formItems = [{
                    itemType: 'group',
                    caption: 'Procedures Parameters',
                    colSpan: 4,
                    colCount: 4,
                    items: []
                }];
                let item = {};
                let editor = {};
                let defaultValue = {};
                let _lovArray = [];
                let _preDefinedArray = [];
                let _isParent = 0;
                let _tableQuery = null;
                let _parentId = 0;
                let _source = [];
                let name = null;
                let _format = null;
                for (var i = 0; i < total; i++)
                {
                    _format = null;
                    _source = [];
                    _disabled = true;
                    item = data[i];
                    name = item.NAME;
                    defaultValue = item.DEFAULT_VALUE;
                    _isParent = item.IS_PARENT;
                    _tableQuery = item.TABLE_QUERY;
                    _parentId = item.PARENT_ID;
                    switch (name.toLowerCase())
                    {
                        case "org_id":
                            defaultValue = th.orgId;
                            break;
                        case "comp_id":
                            defaultValue = th.compId;
                            break;
                        case "branch_id":
                            defaultValue = th.branchId;
                            break;
                        case "created_by":
                            defaultValue = th.userId;
                            break;
                        case "user_id":
                            defaultValue = th.userId;
                            break;
                        case "from_date":
                            defaultValue = new Date(new Date().getFullYear(), 0, 1);
                            break;
                        default:
                            _disabled = false;
                            defaultValue = item.DEFAULT_VALUE;
                            break;
                    }
                    editor = item.EDITOR_TYPE;
                    switch (item.DATA_TYPE.toLowerCase())
                    {
                        case 'date':
                            _format = _dateFormat;
                            break;
                        case 'lov':
                            _lovArray.push({ name: item.NAME, value: item.LOV_ID, default: defaultValue, isParent: _isParent, parentId: _parentId });
                            break;
                        case 'defaultparams':
                            _disabled = false;
                            if (_preDefinedArray.find(c => c.value === item.PRE_DEFINED) == undefined)
                            {
                                _preDefinedArray.push({ name: item.NAME, value: item.PRE_DEFINED, default: defaultValue });
                            }
                            break;
                        case 'boolean':
                            _source = [{ ID: 1, NAME: 'Yes' }, { ID: 0, NAME: 'No' }];
                            break;
                        default:
                            break;
                    }
                    formItems[0].items.push({
                        dataField: item.NAME,
                        editorType: editor,
                        label: { text: item.LABEL_NAME, },
                        editorOptions: {
                            dataSource: _source,
                            readOnly: _disabled,
                            width: '100%',
                            value: defaultValue,
                            showClearButton: true,
                            valueExpr: 'ID',
                            displayExpr: 'NAME',
                            id: item.ID,
                            displayFormat: _format,
                        },
                        validationRules: [{ type: 'required' }],
                    });
                };
                //console.log(formItems)
                th.procedureFormInit.option('items', formItems);
                if (_lovArray.length > 0) {
                    th.getLovs(_lovArray, 'procedure');
                }
                if (_preDefinedArray.length > 0) {
                    th.getProcedurePreDefined(_preDefinedArray);
                }
                return true;
            }
            th.userLocalValues = async function (name, defaultValue) {
                let result = { value: defaultValue, disabled: true };
                switch (name.toLowerCase()) {
                    case "org_id":
                        result.defaultValue = th.orgId;
                        break;
                    case "comp_id":
                        result.defaultValue = th.compId;
                        break;
                    case "branch_id":
                        result.defaultValue = th.branchId;
                        break;
                    case "created_by":
                        result.defaultValue = th.userId;
                        break;
                    case "from_date":
                        result.defaultValue = new Date(new Date().getFullYear(), 0, 1);
                        break;
                    default:
                        result.disabled = false;
                        break;
                }
                return result;
            }
            th.getLovs = function (data, flag) {
                let unique = [];
                for (var element of data) {
                    if (unique.indexOf(element.value) == -1) {
                        unique.push(element.value)
                    }
                }

                let result = {
                    lovs: unique,
                    OrgId: th.orgId,
                    CompId: th.compId,
                    BranchId: th.branchId,
                };
                let formInit = null;
                switch (flag) {
                    case 'filter':
                        formInit = th.formInit;
                        break;
                    case 'param':
                        formInit = th.paramFormInit;
                        break;
                    case 'procedure':
                        formInit = th.procedureFormInit;
                        break;
                    default:
                        break;
                }
                $.post(`/api/lov/menuReportLovs/`, result, function (res) {
                    for (var element of data) {
                        formInit.getEditor(element.name).option('dataSource', res[element.value]);
                        formInit.getEditor(element.name).option('value', element.default);
                    }

                })/*.fail(function (error) { })*/;

            };
            th.filterPredefined = function (data) {
                let result = {
                    preDefineds: data.map(c => c.value),
                    OrgId: th.orgId,
                    CompId: th.compId,
                    BranchId: th.branchId,
                };
                let formInit = th.formInit;
                $.post(`/api/lov/preDefined/`, result, function (res) {
                    for (var element of data) {
                        formInit.getEditor(element.name).option('dataSource', res[element.value]);
                        formInit.getEditor(element.name).option('value', element.default);
                    }
                }).fail(function (error) { });
            };
            th.paramsPredefined = function (data) {
                let result = {
                    preDefineds: data.map(c => c.value),
                    OrgId: th.orgId,
                    CompId: th.compId,
                    BranchId: th.branchId,
                };
                let formInit = th.paramFormInit;
                $.post(`/api/lov/preDefined/`, result, function (res) {
                    for (var element of data) {
                        formInit.getEditor(element.name).option('dataSource', res[element.value]);
                        formInit.getEditor(element.name).option('value', element.default);
                    }
                }).fail(function (error) { });
            };
            th.getProcedurePreDefined = function (data) {
                let result = {
                    preDefineds: data.map(c => c.value),
                    OrgId: th.orgId,
                    CompId: th.compId,
                    BranchId: th.branchId,
                };
                let formInit = th.procedureFormInit;
                $.post(`/api/lov/preDefined/`, result, function (res) {
                    for (var element of data) {
                        formInit.getEditor(element.name).option('dataSource', res[element.value]);
                        formInit.getEditor(element.name).option('value', element.default);
                    }
                }).fail(function (error) { });
            }
            th.filterProcess = function () {
                let _reportFilters = th.formInit.option('formData');
                let tmpFilter = th.filterValue;

                let val = null;
                let editor = {};
                for (var i in _reportFilters) {
                    editor = th.formInit.getEditor(i).NAME;
                    if (editor === 'dxDateBox') {
                        val = new Date(_reportFilters[i]);
                        let dd = String(val.getDate()).padStart(2, '0');
                        let mm = String(val.getMonth() + 1).padStart(2, '0');
                        let yyyy = val.getFullYear();
                        //val = `'Date(${yyyy},${mm},${dd})'`;
                        val = `Date(${yyyy},${mm},${dd})`;
                        tmpFilter = th.processor(tmpFilter, i, val);
                    }
                    else {
                        val = _reportFilters[i];
                        tmpFilter = th.processor(tmpFilter, i, val);
                    }
                }
                th.finalResult.FILTER = tmpFilter;
                th.finalResult.COMP_ID = th.compId;
                th.hitLocalApi(th.finalResult);
            };
            th.paramProcess = function () {
                let params = th.paramFormInit.option('formData');
                console.log(params)
                let subReport = {};
                let val = {};
                let tmp = [];
                let _init = th.paramFormInit;
                let editor = null;
                let editorValue = null;
                let editorType = null;
                for (var i in params) {
                    editor = _init.getEditor(i);
                    //console.log(editor.NAME,'editor')
                    val = editor.option('value');
                    subReport = editor.option('subReportName');
                    editorType = editor.NAME;
                    editorValue = params[i];
                    if (editorType === 'dxDateBox') {
                        editorValue = new Date(editorValue);
                        let dd = String(editorValue.getDate()).padStart(2, '0');
                        let mm = String(editorValue.getMonth() + 1).padStart(2, '0');
                        let yyyy = editorValue.getFullYear();
                        //val = `'Date(${yyyy},${mm},${dd})'`;
                        editorValue = `${mm}/${dd}/${yyyy}`;
                    }
                    tmp.push({
                        Name: i,
                        Value: editorValue,
                        SubReport: subReport
                    });
                }
                var JsonObject = JSON.stringify(tmp);
                th.finalResult.subReportJsonObject = JsonObject;
                th.finalResult.HAS_PARAMS = true;
                th.filterProcess();
            };
            th.hitLocalApi = function (finalResult) {
                //console.log(finalResult,'finalResult')
                let expType = th.exportType;
                $.post(`/api/sm_report/menuReport/generate/filter/${expType}`, finalResult, function (res) {
                    var url = res.feedback;
                    var link = document.createElement('a');
                    link.setAttribute("target", "_blank");
                    link.href = url;
                    link.click();
                }).fail(function (error) {
                    console.error(error)
                });
            };
        };
        _paramForm.onInitialized = function (e) {
            th.paramFormInit = e.component;
        };
        _procedureForm.onInitialized = function (e) {
            th.procedureFormInit = e.component;
        };
    }
}
class submitBox {
    returnResult(x) {

    };
    constructor(divisionId, sgId, doc, userId, showSubmit = true, returnResult) {
        if (doc != null && doc != undefined) {

            //var f = this.form;
            let th = this;
            th.returnResult = returnResult;
            th.transDoc = doc;
            th.userId = userId;
            th.sgId = sgId;
            th.divisionId = divisionId;
            let elementTester = document.getElementById(`${doc}submitPopup`);
            if (elementTester != null || elementTester != undefined) {
                elementTester.remove();
            }
            let newPopup = document.createElement("div");
            newPopup.setAttribute("id", `${doc}submitPopup`);

            let newForm = document.createElement("div");
            newForm.setAttribute("id", `${doc}submitForm`);

            let dxList = document.createElement("div");
            dxList.setAttribute("id", `${doc}submitLogs`);

            let scroll = document.createElement("div");
            scroll.setAttribute("id", `${doc}submitScroll`);
            scroll.appendChild(newForm);
            scroll.appendChild(dxList);
            newPopup.appendChild(scroll);
            document.body.appendChild(newPopup);
            setTimeout(() => {
                $(`#${doc}submitPopup`).dxPopup({
                    onInitialized(e) {
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
                                        visible: showSubmit,
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
                                            init.option('visible', false);
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
                    width: '60%',
                });
                $(`#${doc}submitScroll`).dxScrollView({ width: '100%', height: '100%' });
                $(`#${doc}submitForm`).dxForm({
                    onInitialized(e) {
                        th.formInit = e.component;
                        th.formInit.option('items[1].editorOptions.onSelectionChanged', (e) => {
                            let item = e.selectedItem;
                            if (item != undefined || item != null) {
                                th.setLevels(item.ID);
                            }
                        });
                        th.getUserLevel = function () {
                            $.get(`/api/approve/sgApproval/${th.divisionId}/${th.sgId}/${th.transDoc}`).then((res) => {
                                let _isAuthorized = res.IS_AUTHORIZED;
                                //console.log(res)
                                if (_isAuthorized) {
                                    th._userLevel = res.LEVEL_ID;
                                    th._lastLevel = res.LAST_LEVEL;
                                    th._formId = res.FORM_ID;
                                    th._docId = res.DOC_ID;
                                    th.docData = res;
                                    th.overLapType = res.OVERLAP_TYPE;
                                }
                            });
                        };
                        th.startProcess = function (transLvl, transId) {
                            //console.log(transLvl, transId)
                            //console.log(th._userLevel,'_userLevel')
                            //console.log(th.overLapType,'overlap')
                            //console.log(transLvl,'transLvl')
                            let overLapType = th.overLapType;
                            if (overLapType == 1) {

                            } else if (overLapType == 3) {
                                let userLevel = th._userLevel;
                                if (transLvl >= th._lastLevel) {
                                    th.popupInit.option('toolbarItems[1].options.disabled', true);
                                    th.formInit.option('disabled', true);
                                } else {
                                    th.formInit.option('disabled', false);
                                    transLvl == 0 ? transLvl = transLvl + 1 : transLvl = userLevel;
                                    th._transId = transId;
                                    th._transLevel = transLvl;
                                    th.resetApproval(transLvl);
                                    th.popupInit.option('toolbarItems[1].options.disabled', false);
                                }
                                setTimeout(() => {
                                    th.getHistory(th.transDoc, transId);
                                });
                            }
                        };
                        th.resetApproval = function (id) {
                            th.formInit.resetValues();
                            var tmp = [];
                            if (id == 1) {
                                tmp.push({ ID: 1, NAME: "Submit", icon: 'fa fa-check' });
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
                        };
                        th.setLevels = function (status) {
                            let transLevel = th._transLevel;
                            let nexLevel = transLevel + 1;
                            let lastLevl = th._lastLevel;
                            let tmp = [];
                            switch (status) {
                                case 1:
                                    if (transLevel < lastLevl) {
                                        tmp.push({ ID: nexLevel, NAME: "Next Level ", icon: 'fa fa-check' });
                                    } else if (transLevel == lastLevl) {
                                        tmp.push({ ID: nexLevel - 1, NAME: "Approve Level", icon: 'fa fa-check' });
                                    }
                                    break;
                                case 2:
                                    for (var i = transLevel - 1; i > 0; i--) {
                                        tmp.push({ ID: transLevel - 1, NAME: "Level " + i, icon: 'fa fa-chevron-right' });
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
                            let data = th.formInit.option('formData');
                            let result = th.docData;
                            result.TRANSACTION_ID = th._transId;
                            result.FROM_LEVEL = data.FROM_LEVEL;
                            result.APPROVE_STATUS = data.APPROVE_STATUS;
                            result.TO_LEVEL = data.TO_LEVEL;
                            result.REMARKS = data.REMARKS;
                            result = requestParam(result);
                            showBasicLoader(true);
                            $.post(`/api/approve/unique`, result).then((res) => {
                                showBasicLoader(false);
                                if (res.status) {
                                    showIndicator(res.feedback, 'success');
                                    th.hidePopup();
                                    th.returnResult(true);
                                } else {
                                    showIndicator(res.feedback, 'error');
                                }
                            }).fail(function (error) {
                                //console.log(error.responseText)
                                showBasicLoader(false);
                                showIndicator(error.feedback, 'error');
                            });
                        };
                        th.getHistory = function (docName, id) {
                            showBasicLoader(true);
                            th.dxListInit.option('dataSource', []);
                            $.get(`/api/approve/${docName}/${id}`).then((res) => {
                                showBasicLoader(false);
                                th.dxListInit.option('dataSource', res);
                                setTimeout(() => {
                                    th.showPopup();
                                });
                            });
                        };
                        th.getUserLevel();
                    },
                    colCount: 3,
                    focusStateEnabled: true,
                    scrollingEnabled: true,
                    showRequiredMark: false,
                    showColonAfterLabel: false,
                    scrollingEnabled: true,
                    formData: {},
                    labelMode: "float",
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
                            },
                            validationRules: [{ type: 'required' }],
                        },
                    ],
                });
                $(`#${doc}submitLogs`).dxList({
                    elementAttr: { class: 'top-5px transaction-box' },
                    onInitialized(e) {
                        th.dxListInit = e.component;
                    },
                    itemTemplate(data, itemIndex, elelment) {
                        let div = $(`<div>`).addClass(`transaction-submit-content`).addClass(`approve-${data.APPROVE_STATUS}`).append(
                            $(`<img class="dataGrid-row-logo" src="/upload/${data.PROFILE}" onError="userIconstandby(this)" />`),
                            $('<span>').addClass('comment-area').text(`${data.REMARKS}`),
                            $(`<span style='float:right;'>`).text(`${data.STATUS}`),
                        );
                        let div2 = $('<div>').addClass(`transaction-submit-footer`).append(
                            $(`<label>${data.CREATED_AT} </label>`),
                            $(`<label style='margin-left:15px;'> ${data.USER_EMAIL} </label>`),
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
    }
}
class commentBox {
    constructor(doc, showSubmit = true) {
        let th = this;
        if (doc != null && doc != undefined) {
            th.transDoc = doc;
            let elementTester = document.getElementById(`${doc}commentBoxPopup`);
            if (elementTester != null || elementTester != undefined) {
                elementTester.remove();
            }
            let newPopup = document.createElement("div");
            newPopup.setAttribute("id", `${doc}commentBoxPopup`);
            let newForm = document.createElement("div");
            newForm.setAttribute("id", `${doc}commentBoxForm`);
            let dxTransList = document.createElement("div");
            dxTransList.setAttribute("id", `${doc}commentBoxTransList`);
            let dxApprovalList = document.createElement("div");
            dxApprovalList.setAttribute("id", `${doc}commentBoxApprovalList`);
            let dxCommentList = document.createElement("div");
            dxCommentList.setAttribute("id", `${doc}commentBoxCommentList`);

            let scroll = document.createElement("div");
            scroll.setAttribute("id", `${doc}commentBoxScroll`);
            scroll.appendChild(newForm);
            scroll.appendChild(dxTransList);
            scroll.appendChild(dxApprovalList);
            scroll.appendChild(dxCommentList);

            newPopup.appendChild(scroll);
            document.body.appendChild(newPopup);
            setTimeout(() => {
                $(`#${doc}commentBoxPopup`).dxPopup({
                    onInitialized(e) {
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
                                            init.option('visible', false);
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
                    title: '',
                });
                $(`#${doc}commentBoxScroll`).dxScrollView({ width: '100%', height: '100%' });
                $(`#${doc}commentBoxForm`).dxForm({
                    onInitialized(e) {
                        th.formInit = e.component;
                        th.startProcess = function (transId, docType) {
                            th._transId = transId;
                            if (docType == null || docType == undefined) {
                                th.transDoc = getDocName();
                            } else {
                                th.transDoc = docType;
                            }

                            th.formInit.option('formData', { DOC_NAME: th.transDoc, DOC_ID: th._transId });
                            setTimeout(() => {
                                th.getHistory(th.transDoc, transId);
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
                                    th.formInit.option('formData', { DOC_NAME: th.transDoc, DOC_ID: th._transId, REMARKS: null });
                                    let data = th.dxCommentListInit.option('dataSource');
                                    let item = res.obj[0];
                                    data.unshift(item);
                                    setTimeout(() => {
                                        th.dxCommentListInit.option('dataSource', data);
                                    });
                                } else {
                                    showIndicator(res.feedback, 'error');
                                }
                            }).fail(function (error) {
                                console.log(error.responseText)
                                showBasicLoader(false);
                                showIndicator(error.feedback, 'error');
                            });
                        };
                        th.getHistory = function (docName, id) {
                            showBasicLoader(true);
                            $.get(`/api/comments/${docName}/${id}`).then((res) => {
                                showBasicLoader(false);
                                //console.log(res)
                                th.dxTransListInit.option('dataSource', res.TRANS_REMARK);
                                th.dxApprovalListInit.option('dataSource', res.APPROVAL_REMARKS);
                                th.dxCommentListInit.option('dataSource', res.COMMENTS);
                                setTimeout(() => {
                                    th.showPopup();
                                });
                            }).fail((res) => {
                                showBasicLoader(false);
                                console.log(res)
                                showAlert('Check Doc Type')
                            });
                        };
                    },
                    colCount: 1,
                    focusStateEnabled: true,
                    scrollingEnabled: true,
                    showRequiredMark: false,
                    showColonAfterLabel: false,
                    scrollingEnabled: true,
                    formData: {},
                    labelMode: "float",
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
                });
                $(`#${doc}commentBoxTransList`).dxList({
                    onInitialized(e) {
                        th.dxTransListInit = e.component;
                    },
                    elementAttr: { class: 'top-5px transaction-box' },
                    itemTemplate(data, itemIndex, elelment) {
                        let div = $(`<div>`).addClass(`transaction-submit-content`).append(
                            $(`<img class="dataGrid-row-logo" src="/upload/${data.PROFILE}" onError="userIconstandby(this)" />`),
                            $('<span>').addClass('comment-area').text(`${data.USER_EMAIL}`),
                            $(`<span style='float:right;'>`).text(`(Transaction)`),
                        );
                        let div2 = $('<div>').addClass(`transaction-submit-footer`).append(
                            $(`<label>${data.CREATED_AT} </label>`),
                            $(`<label style='margin-left:15px;'><b>By</b> ${data.REMARKS} </label>`)
                                .addClass('comment-box-area'),
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
                });
                $(`#${doc}commentBoxApprovalList`).dxList({
                    onInitialized(e) {
                        th.dxApprovalListInit = e.component;
                    },
                    elementAttr: { class: 'top-5px transaction-box' },
                    itemTemplate(data, itemIndex, elelment) {
                        let div = $(`<div>`).addClass(`transaction-submit-content`).append(
                            $(`<img class="dataGrid-row-logo" src="/upload/${data.PROFILE}" onError="userIconstandby(this)" />`),
                            $('<span>').addClass('comment-area').text(`${data.USER_EMAIL}`),
                            $(`<span style='float:right;'>`).text(`(Approval)`),
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
                $(`#${doc}commentBoxCommentList`).dxList({
                    onInitialized(e) {
                        th.dxCommentListInit = e.component;
                    },
                    elementAttr: { class: 'top-5px transaction-box' },
                    itemTemplate(data, itemIndex, elelment) {
                        let div = $(`<div>`).addClass(`transaction-submit-content`).append(
                            $(`<img class="dataGrid-row-logo" src="/upload/${data.PROFILE}" onError="userIconstandby(this)" />`),
                            $('<span>').addClass('comment-area').text(`${data.USER_EMAIL}`),
                            $(`<span style='float:right;'>`).text(`(Comments)`),
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
    }
}
class documentBox
{
    print(actType, row)
    {
        let uniqueName = row.FILE_NAME;
        let url = row.URL;
        if(row.FILE_FULL_PATH != null && row.FILE_FULL_PATH != undefined)
             url = row.FILE_FULL_PATH; 
        switch (actType)
        {
            case "download":
                let a = document.createElement('a');
                a.setAttribute("target", "_blank");
                a.setAttribute("download", uniqueName);
                //console.log(url, uniqueName)
                a.href = url;
                a.click();
                //window.open(url, '_blank', "download");
                break;
            default:
               // showLoader();
                //alkanziPreview.option('title', `PREVIEW: ${uniqueName}`);
                //alkanziPreview.option('visible', true);
                //setTimeout(() =>
                //{
                //    $(`#alkanziPreviewFrame`).attr('src', url);
                //    $(`#alkanziPreviewFrame`).attr('title', uniqueName);
                //}, 100);
                window.open(url, '_blank');
                break;
        };
        
    };
    constructor(doc, showSubmit = true) {
        let th = this;
        if (doc != null && doc != undefined) {
            th.transDoc = doc;
            let elementTester = document.getElementById(`${doc}documentBoxPopup`);
            if (elementTester != null || elementTester != undefined) {
                elementTester.remove();
            }
            let newPopup = document.createElement("div");
            newPopup.setAttribute("id", `${doc}documentBoxPopup`);
            let newForm = document.createElement("div");
            newForm.setAttribute("id", `${doc}documentBoxForm`);
            let scroll = document.createElement("div");
            scroll.setAttribute("id", `${doc}documentBoxScroll`);

            let grid = document.createElement("div");
            grid.setAttribute("id", `${doc}documentBoxGrid`);

            scroll.appendChild(newForm);
            scroll.appendChild(grid);
            newPopup.appendChild(scroll);
            document.body.appendChild(newPopup);
            let _formId = `#${doc}documentBoxForm`;
            setTimeout(() => {
                $(`#${doc}documentBoxPopup`).dxPopup({
                    onInitialized(e) {
                        let init = e.component;
                        th.popupInit = e.component;
                        th.hidePopup = function () {
                            init.option('visible', false);
                        };
                        th.showPopup = function (show = true) {
                            if (show == undefined) {
                                show = true;
                            }
                            //th.reset();
                            init.option('toolbarItems[1].options.visible', show)
                            init.option('visible', true);
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
                                            init.option('visible', false);
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
                    shading: false,
                    shadingColor: 'rgba(0,0,0,0.5)',
                    title: 'Transaction Documents',
                    //width:500,
                });
                $(`#${doc}documentBoxScroll`).dxScrollView({ width: '100%', height: '100%' });
                $(_formId).dxForm({
                    onInitialized(e) {
                        th.formInit = e.component;
                        let _formInit = e.component;
                        th.startProcess = function (transId, docType, detailId = 0)
                        {
                            //console.log(transId, detailId)
                            th._transId = transId;
                            th._detailId = detailId;
                            th._docType = docType;
                            if (docType == null || docType == undefined) {
                                docType = getDocName();
                            };
                            th.transDoc = docType;
                            if (docType == null || docType == undefined) {
                                showAlert(`INVALID DOC. TYPE`);
                                return false;
                            };
                            //_formInit.getEditor(`DOC_NAME`).option('value', docType);
                            //_formInit.getEditor(`DOC_ID`).option('value', transId);
                            _formInit.getEditor(`DOC_TYPE`).option('value', null);
                            _formInit.getEditor(`REMARKS`).option('value', null);
                            th.getTypes(th.transDoc);
                            setTimeout(() => {
                                th.getHistory(docType, transId);
                            });
                        };                        
                        th.getTypes = function (docName)
                        {                           
                            console.log(`/api/documents/${docName}/doctypes`)
                            //`/api/documents?docName=${url}&id=${transactionID}`
                            $.get(`/api/documents/${docName}/doctypes`).done((res) => {
                                th.formInit.getEditor('DOC_TYPE').option('dataSource', res);
                            }).fail((error) => { console.log(error)});
                        };
                        th.getTypes(th.transDoc);
                        th.submit = function (e) {
                            let fd = new FormData();
                            let data = th.formInit.option('formData');
                            data.DOC_NAME = th.transDoc;
                            data.DOC_ID = th._transId;
                            data.DTL_ID = th._detailId;
                            data = requestParam(data);
                            let files = data.FILE[0];
                            fd.append('File', files);
                            fd.append('REMARKS', data.REMARKS);
                            fd.append('DOC_ID', data.DOC_ID);
                            fd.append('DTL_ID', data.DTL_ID);
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
                                        _formInit.getEditor(`FILE`).option('value', null);
                                        th.getHistory(th._docType, th._transId);
                                        //let item = response.obj[0];
                                        //let data = th.gridInit.option('dataSource');
                                        //data.unshift(item);
                                        //setTimeout(() => {
                                        //    th.gridInit.option('dataSource', data);
                                        //    th.gridInit.option('focusedRowKey', item.ID);
                                        //});
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
                            //debugger
                            //console.log(docName)
                            let url = `/api/documents?docName=${docName}&id=${id}`;
                            //if (docName === 'customer')
                            //{
                            //    url = `/api/documents/owner/${docName}/${id}`;
                            //} else
                            //{
                            //    url = `/api/documents?docName=${docName}&id=${id}`;
                            //};
                            //console.log(url)
                            th.gridInit.option('dataSource', []);
                            $.get(url).done((res) => {
                                showBasicLoader(false);
                                //console.log(res)
                                if (res.success)
                                    th.gridInit.option('dataSource', res.data);
                                setTimeout(() => {
                                    th.showPopup();
                                });
                            }).fail((res) => {
                                showBasicLoader(false);
                                //console.log(res)
                                showAlert('Check Doc Type')
                            });
                        };
                    },
                    colCount: 1,
                    focusStateEnabled: true,
                    scrollingEnabled: true,
                    showRequiredMark: false,
                    showColonAfterLabel: false,
                    scrollingEnabled: true,
                    formData: {},
                    labelMode: "float",
                    labelLocation: 'top',
                    items: [
                        {
                            itemType: 'group',
                            colSpan: 1,
                            colCount: 3,
                            items: [
                                {
                                    dataField: 'EXPIRE_DATE',
                                    editorType:'dxDateBox',
                                    editorOptions: {
                                        placeholder:'Optional',
                                        width:'100%',
                                        displayFormat: _dateFormat,
                                    },
                                },
                                {
                                    dataField: 'DOC_TYPE',
                                    editorType: 'dxSelectBox',
                                    editorOptions: {
                                        valueExpr: 'ID',
                                        displayExpr: 'NAME',
                                        itemTemplate(data)
                                        {
                                            let madatory = data.MANDATORY;
                                            let type = 'gray';
                                            let title = 'Optional';
                                            if (madatory == 1)
                                            {
                                                type = '#d9534f';
                                                title = 'Mandatory';
                                            }
                                            return  $('<div>').append(
                                                $(`<span>`).addClass('dx-icon dx-icon-pdffile upload-doc-icon success-color'),
                                                $(`<span>`).text(data.NAME),
                                                $(`<span>`).css({ 'float': 'right', 'color': type }).text(title)
                                            );
                                        },
                                        buttons: [
                                            {
                                                name: 'docTypebtn',
                                                options: {
                                                    icon: 'search',
                                                    elementAttr: { class: 'custom-success-btn' },
                                                },
                                            }
                                        ],
                                    },
                                    //validationRules: [{ type: 'required' }],
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
                                        buttons: [
                                            {
                                                name: 'docTypebtn',
                                                options: {
                                                    icon: 'edit',
                                                    elementAttr: { class: 'custom-success-btn' },
                                                },
                                            }
                                        ],
                                    },
                                    validationRules: [{ type: 'required' }],
                                },
                            ]
                        },
                        {
                            dataField: 'FILE',
                            editorType: 'dxFileUploader',
                            editorOptions: {
                                hoverStateEnabled: true,
                                focusStateEnabled: true,
                                activeStateEnabled: true,
                                selectButtonText: 'SELECT PDF / IMAGE',
                                labelText: '',
                                //accept: ['.jpg', '.jpeg', '.gif', '.png', '.pdf'],
                                accept: '.jpg,.jpeg,.gif,.png,.pdf,.jfif,.msg,.xlsx,xlsx',
                                multiple: false,
                                uploadMode: 'useForm',
                                maxFileSize: 15728640,
                                minFileSize: 1,
                            },
                            validationRules: [{ type: 'required' }],
                        },
                    ],
                });
                $(`#${doc}documentBoxGrid`).dxDataGrid({
                    onInitialized(e) {
                        th.gridInit = e.component;
                    },
                    onToolbarPreparing(e)
                    {
                        let init = e.component;
                        e.toolbarOptions.items.unshift(
                            {
                                location: "before",
                                widget: "dxButton",
                                options: {
                                    icon: "refresh",
                                    elementAttr: { class: cellColors.customSuccessBtn },
                                    onClick: function ()
                                    {
                                        th.getHistory(th._docType, th._transId);
                                    }
                                }
                            },
                        );
                    },
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
                    onCellPrepared(e)
                    {
                        let field = e.column.dataField;
                        if (e.rowType === 'data')
                        {
                            if (field === 'ID' || field === 'SNUMBER')
                            {
                                e.cellElement.addClass(cellColors.default);
                            } else if (field === 'MANDATORY')
                            {
                                e.cellElement.addClass(e.value == 1 ? cellColors.danger : cellColors.orange);
                            }
                            let column = e.column;
                            if (column.type === 'buttons')
                            {
                                //e.cellElement.addClass(cellColors.darkGray);
                                e.cellElement.addClass(cellColors.cusomtSuccessCell);
                            };
                        }
                    },
                    columns: [
                        {
                            dataField: 'ID',
                            dataType: 'number',
                            sortOrder: "desc",
                            allowEditing: false,
                            width: 80,
                            alignment: 'right',
                            visible: false,
                        },
                        {
                            dataField: 'SNUMBER',
                            caption: 'NO',
                            allowEditing: false,
                            width: 90,
                            alignment: 'right',
                            visible: false,
                        },
                        {
                            dataField: 'FROM_SRC',
                            caption: 'SRC',
                            allowEditing: false,
                            width: 100,
                        },
                        {
                            dataField: 'DOC_TYPE_NAME',
                            caption: 'DOC TYPE',
                            width:150,
                        },
                        {
                            dataField: 'REMARKS',
                        },  
                        {
                            dataField: 'CREATED_BY',
                            caption: 'UPLOADED BY',
                        },
                        {
                            dataField: 'CREATED_AT',
                            caption: 'UPLOADED ON',
                            dataType: 'date',
                            //format: _dateFormat,
                            alignment: 'center',
                            width: 150,
                        },
                        {
                            dataField: 'TYPE',
                            dataField: 'MANDATORY',
                            width: 120,
                            alignment: 'center',
                            cellTemplate(container, options)
                            {
                                $('<div>').text(options.value == 1 ? 'MANDATORY' : 'OPTIONAL').appendTo(container)
                            },
                            fixed: true,
                            fixedPosition:'right',
                        },
                        {
                            type: 'buttons',
                            fixed: true,
                            fixedPosition: 'right',
                            buttons: [
                                {
                                    icon: 'download',
                                    onClick(e)
                                    {
                                        let row = e.row.data;
                                        th.print('download', row);
                                    },
                                },
                                {
                                    icon: 'fa fa-eye',
                                    onClick(e)
                                    {
                                        let row = e.row.data;
                                        th.print('open', row);
                                    },
                                }
                            ],

                        },
                    ],
                });
            });
        }
    }
};
/* ──────────────────────────────────────────────────────────────────────────
   documentTiles — reusable "document cards" widget.

   Renders one card per document type into a host element, and manages
   upload / replace / delete / preview. One file per type (re-uploading
   replaces the previous file). Empty type shows a big "+", an uploaded type
   shows an image / pdf / file thumbnail with hover actions.
   Talks to /api/documents directly (same endpoints as ApprovalChat); uploads
   via its own hidden dxFileUploader. Styling: .doc-tile* in alkanzi-grid-colors.css.

   Usage (inside a controller):
       $scope.docTiles = new documentTiles({
           hostId:     'poDocTiles',         // container <div> id (only markup needed)
           docName:    $scope.docName,       // doc-type group name
           $q:         $q,                   // optional; else native Promise
           getTransId: () => $scope._transId,
           isAddMode:  () => !($scope._transId > 0),
           canEdit:    () => true,           // optional; false → preview-only
       });
       $scope.docTiles.load();               // e.g. in the popup onShown
   ────────────────────────────────────────────────────────────────────────── */
class documentTiles {
    constructor(opts) {
        opts = opts || {};
        let th = this;
        th.hostId      = opts.hostId;
        th.docName     = opts.docName;
        th.$q          = opts.$q;   // optional; falls back to native Promise
        // The widget talks to /api/documents directly (same endpoints as the
        // ApprovalChat helper) — no documentService / config needed. Uploading
        // uses its own hidden dxFileUploader (see _ensureUploader). `service`,
        // `config` and `fileInputId` options are accepted but ignored (back-compat).
        th.getTransId  = opts.getTransId || function () { return 0; };
        th.isAddMode   = opts.isAddMode  || function () { return false; };
        // canEdit → when false, cards are preview-only: no "+" upload, no
        // replace / delete actions. May be a function (re-evaluated per render).
        th.canEdit     = opts.canEdit    || function () { return true; };

        th.types = [];
        th.items = [];
        th._pendingType = null;
        th.IMG_EXT = ['png', 'jpg', 'jpeg', 'gif', 'webp', 'bmp', 'svg'];
    }

    // ── data ────────────────────────────────────────────────────────────────
    // Load document types and uploaded docs INDEPENDENTLY, then paint the tiles.
    // Types and docs must not gate each other: a failed/empty doctypes response
    // must never hide already-uploaded documents. Each promise degrades to [].
    load(docName, id) {
        let th = this;
        if (docName != null) th.docName = docName;
        let transId = (id != null) ? id : th.getTransId();

        // Document types:  GET /api/documents/{docName}/doctypes   → bare array
        let typesP = th._when($.get(`${apis.documents}${th.docName}/doctypes`)).then(
            function (t) {
                console.log(t,'docs')
                return Array.isArray(t) ? t : [];
            },
            function () { return []; }
        );
        let docsP;
        if (transId > 0 && !th.isAddMode()) {
            // Uploaded docs:  GET /api/documents?docName=&id=  → { success, data:[] }
            docsP = th._when($.get(`/api/documents?docName=${th.docName}&id=${transId}`)).then(
                function (res) { return th._normalize(res && res.success ? res.data : res); },
                function () { return []; }
            );
        } else {
            // add mode → keep whatever is staged in memory
            docsP = th._when(th.items || []);
        }
        return th._all([typesP, docsP]).then(function (r) {
            th.types = Array.isArray(r[0]) ? r[0] : [];
            th.items = Array.isArray(r[1]) ? r[1] : [];
            th.render();
        });
    }

    _when(v) { return this.$q ? this.$q.when(v) : Promise.resolve(v); }
    _all(a)  { return this.$q ? this.$q.all(a)  : Promise.all(a); }

    // /api/documents returns a bare array on success but can return an error /
    // wrapper object (e.g. { status:false, feedback:... } when the remote file
    // service is down). Coerce any shape to an array so rendering never throws.
    _normalize(payload) {
        if (Array.isArray(payload)) return payload;
        if (payload && typeof payload === 'object') {
            let keys = ['data', 'documents', 'items', 'result', 'rows'];
            for (let i = 0; i < keys.length; i++)
                if (Array.isArray(payload[keys[i]])) return payload[keys[i]];
            for (let k in payload)
                if (Array.isArray(payload[k])) return payload[k];
        }
        return [];
    }

    _ext(name) {
        if (!name) return '';
        return ('' + name).split('?')[0].split('.').pop().toLowerCase();
    }
    _url(u) {
        if (!u) return '';
        if (('' + u).includes('https://erp.fakhruddin.ae:400')) return u;
        return ('' + u).includes('TrnDocuments') ? u : `/TrnDocuments/${u}`;
    }
    // Open (new tab) or download a file — same behaviour as ApprovalChat.print.
    _print(actType, row) {
        let url = (row && (row.FILE_FULL_PATH || row._objUrl)) || '';
        if (!url) return;
        let a = document.createElement('a');
        a.setAttribute('target', '_blank');
        if (actType === 'download') a.setAttribute('download', (row && row.FILE_NAME) || '');
        a.href = url;
        a.click();
    }

    // Delete a document — POST /api/documents/?key=&values=&type=delete
    _deleteDoc(item, done, fail) {
        $.post(`${apis.documents}?key=${item.ID}&values=${akEncode(item)}&type=${request.delete}`)
            .done(function (res) {
                if (res && res.status) { if (done) done(); }
                else { showIndicator((res && res.feedback) || 'Delete failed', 'error'); if (fail) fail(); }
            })
            .fail(function () { if (fail) fail(); });
    }

    // ── render ────────────────────────────────────────────────────────────────
    render() {
        let th = this;
        let $host = $('#' + th.hostId);
        if (!$host.length) return;
        $host.empty();

        let types = Array.isArray(th.types) ? th.types : [];
        let docs  = Array.isArray(th.items) ? th.items : [];
        let editable = !!th.canEdit();

        // type-id → name, so an uploaded file can show its document-type label
        let nameById = {};
        types.forEach(function (t) { nameById[t.ID] = t.NAME; });

        // One card per UPLOADED document (we no longer render a card per empty type).
        docs.forEach(function (doc) {
            let label = nameById[doc.DOC_TYPE] || doc.DOC_TYPE_NAME || 'Document';
            $host.append(th._fileCard(label, doc, editable));
        });

        // A single "upload" card: pick a document type, then choose the file.
        if (editable && types.length) {
            $host.append(th._uploadCard(types));
        }

        if (!docs.length && !(editable && types.length)) {
            $('<div>').addClass('doc-tile-empty-note')
                .text(editable ? 'No document types configured.' : 'No documents uploaded.')
                .appendTo($host);
        }
    }

    // One uploaded file → preview on click, delete action on hover.
    _fileCard(label, doc, editable) {
        let th = this;
        let name = doc.FILE_NAME  ;
        let $tile = $('<div>').addClass('doc-tile has-file');
        $('<div>').addClass('doc-tile-title')
            .append($('<span>').addClass('doc-tile-name').attr('title', name).text(name))
            .appendTo($tile);
        let $body = $('<div>').addClass('doc-tile-body').appendTo($tile);
        
        let isImg = doc._objUrl ? doc._isImage : th.IMG_EXT.includes(th._ext(name));
        if (isImg) {
            $('<img>').addClass('doc-tile-thumb').attr('src', doc._objUrl || doc.FILE_FULL_PATH || th._url(doc.UNIQUE_NAME)).appendTo($body);
        } else if (th._ext(name) === 'pdf') {
            $('<img>').addClass('doc-tile-icon').attr('src', '/images/pdf-file2.png').appendTo($body);
        } else {
            $('<i>').addClass('doc-tile-icon fa-regular fa-file-lines').appendTo($body);
        }
        $body.on('click', function () { th._print('open', doc); });
        if (editable) {
            let $act = $('<div>').addClass('doc-tile-actions').appendTo($tile);
            $('<span title="Delete">').addClass('doc-tile-act danger fa-regular fa-trash-can')
                .on('click', function (ev) { ev.stopPropagation(); th.remove(doc); }).appendTo($act);
        }
        let foot = doc.REMARKS || doc.FILE_NAME || doc.UNIQUE_NAME || '';
        $('<div>').addClass('doc-tile-foot').attr('title', foot).text(foot).appendTo($tile);
        return $tile;
    }

    // Upload card — a doc-type picker (dxSelectBox); selecting a type opens the
    // file dialog and uploads to that type. Mandatory types are flagged with "*".
    _uploadCard(types) {
        let th = this;
        let $tile = $('<div>').addClass('doc-tile empty doc-tile-upload');
        $('<div>').addClass('doc-tile-title')
            .append($('<span>').addClass('doc-tile-name').text('Add Document'))
            .appendTo($tile);
        let $body = $('<div>').addClass('doc-tile-body doc-tile-upload-body').appendTo($tile);
        $('<span>').addClass('doc-tile-plus fa-solid fa-cloud-arrow-up').appendTo($body);
        $('<div>').addClass('doc-tile-typesel').appendTo($body).dxSelectBox({
            dataSource: types,
            valueExpr: 'ID',
            displayExpr: function (d) { return d ? (d.MANDATORY == 1 ? (d.NAME + ' *') : d.NAME) : ''; },
            placeholder: 'Select type…',
            searchEnabled: true,
            onValueChanged: function (e) {
                if (e.value != null && e.value !== '') {
                    let typeId = e.value;
                    e.component.option('value', null);   // reset for the next upload
                    th.pickFile(typeId);
                }
            },
        });
        return $tile;
    }

    // ── actions ────────────────────────────────────────────────────────────────
    // File picking is driven by a single hidden dxFileUploader (same mechanism as
    // the ApprovalChat "openUploaderBtn"): accept-filter + size limit, POST field
    // name "myFile" → /api/documents/upload. Created once, reused by every tile.
    _ensureUploader() {
        let th = this;
        if (th._uploaderInit) return;
        let hostId = (th.hostId || 'docTiles') + '_uploader';
        th._uploaderHostId = hostId;
        let $el = $('#' + hostId);
        if (!$el.length) {
            // off-screen (not display:none) so the click reliably opens the dialog
            $el = $('<div>').attr('id', hostId)
                .css({ position: 'absolute', left: '-9999px', width: 0, height: 0, overflow: 'hidden' })
                .appendTo(document.body);
        }
        $el.dxFileUploader({
            elementAttr: { id: hostId },
            multiple: false,
            uploadMode: 'useButtons',     // we call upload() ourselves after picking
            uploadMethod: 'POST',
            name: 'myFile',
            accept: '.jpg,.jpeg,.gif,.png,.jfif,.pdf,.msg,.xls,.xlsx,.doc,.docx',
            maxFileSize: 15728640,        // 15 MB
            dialogTrigger: '#' + hostId,
            onInitialized: function (e) { th._uploaderInit = e.component; },
            onValueChanged: function (e) {
                let file = e.value && e.value[0];
                if (!file) return;
                if (th.isAddMode()) {
                    // no record yet → stage in memory (one per type); don't upload
                    th._stageFile(th._pendingType, file);
                    e.component.option('value', []);
                    return;
                }
                e.component.option('uploadUrl', th._uploadUrl(th._pendingType));
                showLoader();
                e.component.upload();
            },
            onUploaded: function (e) {
                hideLoader();
                let res; try { res = JSON.parse(e.request.response); } catch (x) { res = {}; }
                e.component.option('value', []);
                th._afterUpload(res);
            },
            onUploadError: function () { hideLoader(); showIndicator('Upload failed', 'error'); },
        });
    }

    pickFile(typeId) {
        this._pendingType = typeId;
        this._ensureUploader();
        if (this._uploaderInit) this._uploaderInit.option('value', []);
        // open the native file dialog through the uploader's trigger element
        $('#' + this._uploaderHostId).trigger('click');
    }

    _uploadUrl(typeId) {
        // POST /api/documents/upload?transId=&docType=&docName=&remarks=
        return `${apis.documents}upload?transId=${this.getTransId()}`
             + `&docType=${typeId}`
             + `&docName=${encodeURIComponent(this.docName)}`
             + `&remarks=${encodeURIComponent('')}`;
    }

    _stageFile(typeId, file) {
        let th = this;
        // many files per type are allowed → append (don't replace)
        if (!Array.isArray(th.items)) th.items = [];
        th.items.push({
            ID: 0, DOC_TYPE: typeId, REMARKS: '', FILE: file, FILE_NAME: file.name,
            _objUrl: (window.URL || window.webkitURL).createObjectURL(file),
            _isImage: th.IMG_EXT.includes(th._ext(file.name)),
        });
        th.render();
    }

    _afterUpload(res) {
        if (res && res.status) { this.load(); }             // append; reload the list
        else { showIndicator((res && res.feedback) || 'Upload failed', 'error'); }
    }

    remove(item) {
        let th = this;
        if (!item) return;
        if (th.isAddMode()) {
            th.items = (Array.isArray(th.items) ? th.items : []).filter(d => d !== item);
            th.render();
            return;
        }
        showLoader();
        th._deleteDoc(item, function () {
            hideLoader();
            showIndicator('File Removed Successfully');
            th.load();
        }, function () {
            hideLoader();
            showIndicator('process failed', 'error');
        });
    }
}
class docReport {
    actionHandler;
    constructor(docName, emailOption) {
        let th = this;
        if (docName != undefined && docName != null) {
            if (emailOption == null || emailOption == undefined) {
                emailOption = false;
            }
            th.docType = docName;
            th.emailOption = emailOption;
            this.config.docType = docName;
            this.getReports().done((res) => {
                let tmp = [];
                let total = res.length;
                let item = null;
                for (var i = 0; i < total; i++) {
                    item = res[i];
                    let filter = item.FILTER_VALUE;
                    let _id = item.ID;
                    let reportItem = {
                        id: _id,
                        text: item.NAME,
                        FILTER_VALUE: filter,
                        items: [
                            {
                                id: `${_id}_1`,
                                icon: 'pdffile',
                                text: 'PDF',
                                FILTER_VALUE: filter,
                                onItemClick(e) {
                                    let itemData = e.itemData;
                                    th.expType = 'pdf';
                                    th.collect(_id, itemData.FILTER_VALUE);
                                }
                            },
                            {
                                id: `${_id}_2`,
                                icon:'xlsfile',
                                text: 'EXCL',
                                FILTER_VALUE: filter,
                                onItemClick(e) {
                                    let itemData = e.itemData;
                                    th.expType = 'excel';
                                    th.collect(_id, itemData.FILTER_VALUE);
                                }
                            },
                            {
                                id: `${_id}_3`,
                                icon: 'fa-solid fa-envelope',
                                text: 'EMAIL',
                                visible: th.emailOption == true ? true : false,
                                FILTER_VALUE: filter,
                                onItemClick(e) {
                                    let itemData = e.itemData;
                                    th.expType = 'email';
                                    th.collect(_id, itemData.FILTER_VALUE);
                                }
                            },
                        ],
                        
                    }
                    tmp.push(reportItem);
                }
                this.reports = tmp;
            });
            this.reFreshReport =  function () {
                let config = this.config;
                config.docType = getDocName();
                showLoader();
                return $.get(apis.transDocReport, config, (res) => {
                    hideLoader();
                    let tmp = [];
                    let total = res.length;
                    let item = null;
                    for (var i = 0; i < total; i++) {
                        item = res[i];
                        tmp.push({
                            id: item.ID,
                            text: item.NAME,
                            FILTER_VALUE: item.FILTER_VALUE,
                            onItemClick(e) {
                                let itemData = e.itemData;
                                th.collect(itemData.id, itemData.FILTER_VALUE);
                            }
                        });
                    }
                    this.reports = tmp;
                    //return this.reports;
                }).fail(() => {
                    hideLoader();
                    });
            }
        }
        th.actionHandler = function(data) {
            showBasicLoader(true);
            //let docType = getDocName();
            let expType = th.expType;
            if (expType == 'email') {
                expType = 'pdf';
            }
            //if (docType == 'payroll') {
            //    expType = 'excel';
            //}
            $.post(`/api/sm_report/menuReport/generate/filter/${expType}`, data, function (res) {
                showBasicLoader(false);
                var url = res.feedback;
                if (th.expType != 'email') {
                    var link = document.createElement('a');
                    link.setAttribute("target", "_blank");
                    link.href = url;
                    link.click();
                } else {
                    console.log(th.row);
                    if (th.row.SEND_TO_EMAIL == null || th.row.SEND_TO_EMAIL == undefined || th.row.SEND_TO_EMAIL == '') {
                        showIndicator('email not defined', 'error');
                        return false;
                    }
                    th.row.URL = url;
                    th.row.SENT_BY = getConfig().UserId;
                    console.log(th.row);
                    th.sendEmail(th.row);
                }
                
            }).fail(function (error) {
                showBasicLoader(false);
                console.error(error)
            });
        }
    };
    config = {
        docType: null,
        CompId: _CompId,
        BranchId: _BranchId,
        SecurityGroupId: _sgId,
    };
    _reportKeys = [{ name: '%ORG_ID%', val: _OrgId }, { name: '%COMP_ID%', val: _CompId }, { name: '%BRANCH_ID%', val: _BranchId },];
    getReports() {
        let config = this.config;
        //console.log(apis.transDocReport,config)
        showBasicLoader(true);
        return $.get(apis.transDocReport, config, function (res) {
            //console.log(res, 'res');
            //debugger;
            showBasicLoader(false);
        }).fail(() => {
            showBasicLoader(false);
        });
    };
    collect(id, filter) {
        let row = this.row;
        if (row != null && row != undefined) {
            let tmpFilter = filter.toUpperCase();
            let totalReportKeys = this._reportKeys.length;
            for (var i = 0; i < totalReportKeys; i++) {
                let pointer = 0;
                let tail = 0;
                while (pointer > - 1) {
                    pointer = tmpFilter.indexOf(this._reportKeys[i].name, tail);
                    //console.log(pointer, this._reportKeys[i].name,'repoer key')
                    if (pointer > -1) {
                        tmpFilter = tmpFilter.replace(this._reportKeys[i].name, this._reportKeys[i].val);
                    }
                    tail = pointer + 1;
                }
            }
            for (var i in row) {
                tmpFilter = this.filter(tmpFilter, i, row[i]);
            }
            let data = { FILTER_ID: id, FILTER: tmpFilter, };
            data = setInsertDefaultParams(data);
            this.actionHandler(data);
        }
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
    sendEmail(data) {
        $.ajax({
            url: "/api/GenericEmail/PostSendGenericEmail",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(data),
            success: function (response) {
                if (response.status == true) {
                    showIndicator(response.feedback);
                } else {
                    showIndicator(response.feedback, 'error');
                }
            },
            error: function (xhr, status, error) {
                showIndicator(`Error: ${error}`, 'error');
                console.error("Error:", error);
            }
        });

    }

}

// Global helper to print a docReport entry without needing a docReport instance.
// row         : data row used for %KEY% substitution (e.g. $scope._selected)
// reportItem  : report record returned by transDocReport (must expose ID + FILTER_VALUE)
// expType     : 'pdf' (default) | 'excel' | 'email'
// options     : { onComplete?(res), onError?(err) }
const printDocReport = function (row, reportItem, expType, options) {
    if (row == null || reportItem == null) {
        showIndicator('Select a record and a report first.', 'error');
        return;
    }
    expType = expType || 'pdf';
    options = options || {};

    let filter = (reportItem.FILTER_VALUE || '').toUpperCase();
    let reportKeys = [
        { name: '%ORG_ID%',    val: _OrgId },
        { name: '%COMP_ID%',   val: _CompId },
        { name: '%BRANCH_ID%', val: _BranchId },
    ];
    reportKeys.forEach(k => {
        while (filter.indexOf(k.name) > -1) {
            filter = filter.replace(k.name, k.val);
        }
    });
    for (let key in row) {
        let token = '%' + key + '%';
        while (filter.indexOf(token) > -1) {
            filter = filter.replace(token, row[key]);
        }
    }

    let data = setInsertDefaultParams({
        FILTER_ID: reportItem.ID,
        FILTER: filter,
    });

    let postExp = expType === 'email' ? 'pdf' : expType;
    showBasicLoader(true);
    $.post(`/api/sm_report/menuReport/generate/filter/${postExp}`, data, function (res) {
        showBasicLoader(false);
        let url = res.feedback;
        if (expType !== 'email') {
            let link = document.createElement('a');
            link.setAttribute('target', '_blank');
            link.href = url;
            link.click();
        } else {
            if (row.SEND_TO_EMAIL == null || row.SEND_TO_EMAIL === '') {
                showIndicator('email not defined', 'error');
                return;
            }
            let payload = Object.assign({}, row, { URL: url, SENT_BY: getConfig().UserId });
            $.ajax({
                url: '/api/GenericEmail/PostSendGenericEmail',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(payload),
                success: function (response) {
                    if (response.status === true) showIndicator(response.feedback);
                    else showIndicator(response.feedback, 'error');
                },
                error: function (xhr, status, error) {
                    showIndicator(`Error: ${error}`, 'error');
                    console.error('Error:', error);
                }
            });
        }
        if (typeof options.onComplete === 'function') options.onComplete(res);
    }).fail(function (error) {
        showBasicLoader(false);
        console.error(error);
        if (typeof options.onError === 'function') options.onError(error);
    });
};

// Resolve the central-store on-hand quantity for a given item.
// Backed by RequisitionController.ItemStock (proc IM_REQUISITIONS.ITEM_STOCK_BY_STORE),
// which returns one row per store. The "central" row is the one whose STORE_CODE
// or STORE_NAME contains "CENTRAL" (case-insensitive); falls back to 0 if missing.
// Returns: jQuery promise resolving to { qty:Number, row:Object|null, all:Array }.
const getCentralStockForItem = function (itemId) {
    let dfd = $.Deferred();
    if (!itemId) {
        dfd.resolve({ qty: 0, row: null, all: [] });
        return dfd.promise();
    }
    $.get(`${apis.imRequisition}itemStock`, { id: itemId })
        .done((rows) => {
            rows = rows || [];
            let central = rows.find(r =>
                ((r.STORE_CODE || '') + ' ' + (r.STORE_NAME || ''))
                    .toUpperCase().indexOf('CENTRAL') > -1
            ) || null;
            dfd.resolve({
                qty: central ? (central.AVAILABLE || 0) : 0,
                row: central,
                all: rows,
            });
        })
        .fail(() => dfd.resolve({ qty: 0, row: null, all: [] }));
    return dfd.promise();
};

class generalAccess {
    resetTabs = async function (branches) {
        let tabHistory = JSON.parse(sessionStorage.getItem(`tab-hisotry`));
        if (tabHistory != null || tabHistory != undefined) {
            let tmp = tabHistory;
            tabHistory.forEach((tabItem, index) => {
                let bIndex = branches.findIndex(c => c.ID == parseInt(tabItem.branchId));
                if (bIndex == -1) {
                    tmp.splice(index, 1);
                    //console.log(index);
                    //debugger;
                };
            });
            sessionStorage.setItem(`tab-hisotry`, JSON.stringify(tmp));
        };
        return true;
    };
    setTabSession = async function (selectedBranch) {
        let branchName = null;
        try {
            branchName = selectedBranch.NAME;
            let docName = getDocName();
            let newItem = {
                docType: docName,
                branchId: selectedBranch.ID,
                branchName: selectedBranch.NAME,
                NAME: '',
                NODETYPE: '',
                PATH: '',
                ICON: '',
                DOC_TYPE: docName,
                ID: 0
            };

            let tabHistory = JSON.parse(sessionStorage.getItem(`tab-hisotry`));
            if (tabHistory == null || tabHistory == undefined) {
                tabHistory = [];
            };
            let itemIndex = tabHistory.findIndex(c => c.docType === docName);
            if (itemIndex > -1) {
                tabHistory[itemIndex] = newItem;
            }
            else {
                tabHistory.push(newItem);
            };
            sessionStorage.setItem(`tab-hisotry`, JSON.stringify(tabHistory));
        }
        catch (e) {
            branchName = null;
        }
        return branchName;
    };
    endTabSession = async function () {
        try {
            //let tabHistory = JSON.parse(sessionStorage.getItem(`tab-hisotry`));
            //if (tabHistory != null && tabHistory != undefined) {
            //    let itemIndex = tabHistory.findIndex(c => c.docType === docName);
            //    if (itemIndex > -1) {
            //        tabHistory.splice(itemIndex, 1);
            //        sessionStorage.setItem(`tab-hisotry`, JSON.stringify(tabHistory));
            //    }
            //}
            sessionStorage.setItem(`tab-hisotry`, JSON.stringify([]));
            return true;
        }
        catch (e) {
            return false;
        }
    };
    tabBranch = async function (itemData) {
        let docName = itemData.DOC_TYPE;
        let tabHistory = JSON.parse(sessionStorage.getItem(`tab-hisotry`));
        if (tabHistory == null || tabHistory == undefined) {
            tabHistory = [];
        }

        let itemIndex = tabHistory.findIndex(c => c.docType === docName);

        if (itemIndex > -1) {
            return tabHistory[itemIndex].branchName;
        }
        else if (itemIndex == -1) {
            let mainBranchId = sessionStorage.getItem('mainBranchId');
            let mainBranchName = sessionStorage.getItem('mainBranchName');
            let newItem = {
                docType: docName,
                branchId: mainBranchId,
                branchName: mainBranchName,
                NAME: itemData.NAME,
                NODETYPE: itemData.NODETYPE,
                PATH: itemData.PATH,
                ICON: itemData.ICON,
                DOC_TYPE: docName,
                ID: itemData.ID,
            };
            tabHistory.push(newItem);
            sessionStorage.setItem(`tab-hisotry`, JSON.stringify(tabHistory));
            return mainBranchName;
        }
    };
    setMenuLocalStorage = async function (itemData) {
        let docName = itemData.DOC_TYPE;
        localStorage.setItem('MenuPath', JSON.stringify(itemData));
        sessionStorage.setItem('MenuPath', JSON.stringify(itemData));
        sessionStorage.setItem('docType', docName);
        sessionStorage.setItem('tabName', itemData.NAME);
        sessionStorage.setItem('lastPage', JSON.stringify(itemData));
        return this.tabBranch(itemData);
    };
    getTabName = () => {
        return sessionStorage.getItem('tabName');
    }
};
class _customFinanceTrans {
    _rowData = {};
    getSubItems() {
        let e = this;
        this.subItems = [
            {
                ID: 1,
                text: 'Finance impact',
                icon: 'dx-icon dx-icon-folder',
                onItemClick() {
                    let row = e.row;
                    e._rowData = e.row;
                    $(`#_customFinanceTransGrid`).dxDataGrid('option', 'dataSource', []);
                    e.startProcess(row.DOC_TYPE, row.ID, row.DOC_NUM, 'financeImpact', 'Finance impact');
                }
            },
            {
                ID: 2, text: 'Invoice scheduler', icon: 'dx-icon dx-icon-folder',
                onItemClick() {
                    let row = e.row;
                    e._rowData = e.row;
                    e.startProcess(row.DOC_TYPE, row.ID, row.DOC_NUM, 'invoiceScheduler', 'Invoice scheduler');
                }
            },
            {
                ID: 3, text: 'Normalization scheduler', icon: 'dx-icon dx-icon-folder',
                onItemClick() {
                    let row = e.row;
                    e._rowData = e.row;
                    e.startProcess(row.DOC_TYPE, row.ID, row.DOC_NUM, 'normalizationScheduler', 'Normalization scheduler');
                }
            },
        ];
        //return this.subItems;
    };
    getData(docType, transId, docNum, trans)
    {
        let config = getConfig();
        let branch = (this._rowData.BRANCH_ID == undefined ? _BranchId : this._rowData.BRANCH_ID);
        config.docType = docType;
        config.id = transId;
        config.docNo = docNum;
        config.trans = trans;
        config.OrgId = this._rowData.ORG_ID;
        config.CompId = this._rowData.COMP_ID;
        config.BranchId = branch;


        //console.log(config,'-----')
        console.log(this._rowData, '-----')

        if (this._rowData != null && this._rowData.OVERLAP_CONFIG != undefined && this._rowData.OVERLAP_CONFIG > 0) {
            let customConfig = {
                SecurityGroupId: config.SecurityGroupId,
                UserId: config.UserId,
                CompId: this._rowData.COMP_ID,
                OrgId: this._rowData.ORG_ID,
                BranchId: branch,
                docType: docType,
                id: transId,
                docNo: docNum,
                trans: trans,
            };
            console.log(customConfig)
            return $.get(`${apis.general}financeTrans`, customConfig)
        }
        console.log(config,'config')
        return $.get(`${apis.general}financeTrans`, config)
    };
    getTrandInfo(docType, docNum) {
        let config = getConfig();
        config.docType = docType;
        config.docNo = docNum;
        return $.get(`${apis.general}financeTransInfo`, config);
    }
    financeCols = [
        {
            dataField: 'ID',
            dataType: 'number',
            allowEditing: false,
            visible: false,
            width: 140,
            fixed: true,
            fixedPosition: "left",
            alignment: 'center',
        },
        {
            dataField: 'DOC_NO',
            dataType: 'number',
            allowEditing: false,
            sortOrder: "desc",
            width: 140,
            fixed: true,
            fixedPosition: "left",
            alignment: 'center',
        },
        {
            dataField: 'DOC_TYPE',
        },
        {
            dataField: 'SOURCE_TYPE',
            alignment: 'center',
        },
        {
            dataField: 'GL_CODE',
            alignment: 'center',
        },
        {
            dataField: 'GL_CODE_COMBINATION',
        },
        {
            dataField: 'LEDGER_TYPE_NAME',
            caption: 'LEDGER TYPE',
        },
        {
            dataField: 'POSTED_DATE',
            dataType: 'date',
            width: 140,
            format: _dateFormat,
            alignment: 'center',
        },
        {
            dataField: 'GL_DATE',
            dataType: 'date',
            width: 140,
            format: _dateFormat,
            alignment: 'center',
        },
        {
            dataField: 'AMOUNT',
            dataType: 'number',
            alignment: 'center',
            width: 140,
            format: _decFormat,
            fixed: true,
            fixedPosition: "right",
        },
    ];
    normalizationCols = [
        {
            dataField: 'ID',
            dataType: 'number',
            allowEditing: false,
            visible: false,
            width: 140,
            fixed: true,
            fixedPosition: "left",
            alignment: 'center',
        },
        {
            dataField: 'SCHEDULE_ID',
            dataType: 'number',
            allowEditing: false,
            sortOrder: "desc",
            width: 140,
            fixed: true,
            fixedPosition: "left",
            alignment: 'center',
        },
        {
            dataField: 'DOC_TYPE',
        },
        {
            dataField: 'GL_CODE',
            alignment: 'center',
        },
        {
            dataField: 'MAIN_DOC_TYPE',
        },
        {
            dataField: 'INVOICE_AMOUNT',
            dataType: 'number',
            format: _decFormat,
            alignment: 'center',
        },
        {
            dataField: 'SCHEDULE_DATE',
            dataType: 'date',
            width: 140,
            format: _dateFormat,
            alignment: 'center',
        },
        {
            dataField: 'BRANCH_ID',
            caption: 'BRANCH',
            width: 140,
            alignment: 'center',
        },
        {
            dataField: 'POSTED',
            alignment: 'center',
        },
        {
            dataField: 'SCHEDULE_AMT',
            caption: 'AMOUNT',
            dataType: 'number',
            alignment: 'center',
            width: 140,
            format: _decFormat,
            fixed: true,
            fixedPosition: "right",
        },
    ];
    constructor() {
        this.getSubItems();
        let th = this;
        let newPopup = document.createElement("div");
        newPopup.setAttribute("id", `_customFinanceTransPopup`);

        let scroll = document.createElement("div");
        scroll.setAttribute("id", `_customFinanceTransScroll`);

        let tab = document.createElement("div");
        tab.setAttribute("id", `_customFinanceTransTab`);
        scroll.appendChild(tab);
        newPopup.appendChild(scroll);
        document.body.appendChild(newPopup);
        setTimeout(() => {
            $(`#_customFinanceTransScroll`).dxScrollView({ width: '100%', height: '100%' });
            $(`#_customFinanceTransTab`).dxTabPanel({
                onInitialized(e) {
                    th.tabInit = e.component;
                },
                activeStateEnabled: false,
                focusStateEnabled: false,
                hoverStateEnabled: false,
                onTitleClick(e) {
                    let item = e.itemData;
                    let trans = null;
                    if (item.title === 'Other Finacne Impact') {
                        trans = 'otherFinacneImpact';
                        //$(`#_otherFinanceTransGrid`).dxDataGrid('option', 'columns', th.normalizationCols);

                    } else {
                        trans = 'normalizationScheduler';
                    }
                    //console.log(th._docType, th._transId, th._trans)
                    th.getData(th._docType, th._transId, th._docNum, trans).done((res) => {
                        //console.log(trans)
                        //console.log(res)
                        if (item.title === 'Other Finacne Impact') {
                            $(`#_otherFinanceTransGrid`).dxDataGrid('option', 'dataSource', res);

                        } else {
                            $(`#_customFinanceTransGrid`).dxDataGrid('option', 'dataSource', res);
                        }
                    });
                },
                items: [
                    {
                        title: "Finance",
                        icon: "folder",
                        template() {
                            return $(`<div id="_customFinanceTransGrid"></div>`).dxDataGrid(defaultGrid());
                            //$(`#_customFinanceTransGrid`).dxDataGrid({
                            //    export: { fileName: 'Excel Report', enabled: true },
                            //});
                        }
                    },
                    {
                        title: "Other",
                        icon: "folder",
                        template() {
                            return $(`<div id="_otherFinanceTransGrid"></div>`).dxDataGrid(defaultGrid());
                            //$(`#_customFinanceTransGrid`).dxDataGrid({
                            //    export: { fileName: 'Excel Report', enabled: true },
                            //});
                        }
                    }
                ],
            });
            $(`#_customFinanceTransPopup`).dxPopup({
                onInitialized(e) {
                    let init = e.component;
                    th.popupInit = e.component;
                    th.startProcess = function (docType = 'reservation', transId = 0, docNum = 0, trans = 'financeImpact', title) {
                        th.tabInit.option('items[0].title', title);

                        th.getData(docType, transId, docNum, trans).done((res) => {
                            th._docType = docType;
                            th._transId = transId;
                            th._docNum = docNum;
                            th._trans = trans;
                            th.tabInit.option('items[1].disabled', true);
                            th.tabInit.option('selectedIndex', 0);
                            switch (trans) {
                                case 'financeImpact':
                                    $(`#_customFinanceTransGrid`).dxDataGrid('option', 'columns', th.financeCols);
                                    break;
                                case 'normalizationScheduler':
                                    th.tabInit.option('items[1].disabled', false);
                                    th.tabInit.option('items[1].title', 'Other Finacne Impact');
                                    $(`#_customFinanceTransGrid`).dxDataGrid('option', 'columns', th.normalizationCols);
                                    break;
                                default:
                                    $(`#_customFinanceTransGrid`).dxDataGrid('option', 'columns', th.normalizationCols);
                                    break;
                            }
                            setTimeout(() => {
                                $(`#_customFinanceTransGrid`).dxDataGrid('option', 'dataSource', res);
                            });
                        });
                        init.option('visible', true);
                        init.option('title', `${docType}: Finance`);
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
                                    icon: 'close',
                                    text: 'Exit',
                                    type: 'normal',
                                    onClick() {
                                        init.option('visible', false);
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
                title: '',
                width: '80%',
                export: { fileName: 'Excel Report', enabled: true },
                summary: {
                    recalculateWhileEditing: true,
                    totalItems: [{
                        column: "TOTAL",
                        summaryType: "sum",
                        valueFormat: 'fixedPoint',
                        displayFormat: "{0}",
                        precision: '2',
                    }, {
                        column: "SCHEDULE_AMT",
                        summaryType: "sum",
                        valueFormat: 'fixedPoint',
                        displayFormat: "{0}",
                        precision: '2',
                    },]
                }
            });

        });
    }
};

var _globals = {}; 
$('body').append($('<div/>', {
    'id': 'chatLoader', 'data-info': 'for-chat-loader'
}));
_globals['_chatLoader'] = $(`#chatLoader`).dxLoadPanel({
    position: "center",
    shading: true,
    shadingColor: 'rgba(0,0,0,0.5)',
}).dxLoadPanel('instance');
class ApprovalChat
{
    hdrId = 0;
    getUrl = '';
    list = {
        elementAttr: { class: 'chat-list' },
        keyExpr: 'ID',
        displayExpr: 'MSG',
        selectionMode: 'none',
        noDataText: 'No Approval',
        //width: 600,
        //height:'auto',
        hoverStateEnabled: false,
        focusStateEnabled: false,
        showSelectionControls: true,
        searchEnabled: true,
        searchExpr: ["ID", "MSG"],
    };
    popup = {
        showCloseButton: false,
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
        width: 900,
        wrapperAttr: { id:'chatApproval',class: 'control-panel-popup chat-popup' },
    };
    dropDownPop = {
        wrapperAttr: { id: 'approvalDropDownPopover' },
        target: '#approvalDropDown',
        width: 300,
        height: '82.2%',
        showTitle: true,
        title: 'Options',
        shading: false,
        wrapperAttr: { class: 'control-panel-popup chat-popup' },
    };
    msgEnterPopover = { 
        target: '#kzMsgInput',
        width: '600', 
        showTitle: true,
        title: 'Message',
        hideOnParentScroll: false,
        //hideOnOutsideClick: false,
        shading: false,
        wrapperAttr: { class: 'control-panel-popup chat-popup' },
    };
    progrss = {
        elementAttr: { class: 'approval-progress-list' },
        selectionMode: 'none',
        noDataText: 'No Approval',
        hoverStateEnabled: false,
        focusStateEnabled: false, 
    };
    chatFormPopover = {
        target: "#emptyid",
        showEvent: "dxclick",
        shading: false,
        shadingColor: "rgba(0, 0, 0, 0.5)",
        hideOnOutsideClick: true,
        visible: false,
    };
    documents = {
        elementAttr: { class: 'approval-documents-list' },
        noDataText: 'No Documents',
        hoverStateEnabled: false,
        focusStateEnabled: false,
        selectByClick: true,
        selectionMode: "single",
    };
    print(actType, row)
    {
        let uniqueName = row.FILE_NAME;
        //let url = row.URL; 
        let url = row.FILE_FULL_PATH; 
        let a = document.createElement('a');
        switch (actType)
        {
            case "download":
               
                a.setAttribute("target", "_blank");
                a.setAttribute("download", uniqueName); 
                a.href = url;
                a.click(); 
                break;
            default:
                a.setAttribute("target", "_blank"); 
                a.href = url;
                a.click(); 
                //alkanziPreview.option('title', `Preview: ${row.REMARKS}`);
                //alkanziPreview.option('visible', true);
                //setTimeout(() =>
                //{
                //    alkanziPreview.focus();
                //    $(`#alkanziPreviewFrame`).attr('src', url);
                //    $(`#alkanziPreviewFrame`).attr('title', uniqueName);
                //}, 100); 
                break;
        };

    };
    constructor()
    {
        let popup = this.popup;
        let x = this.list;
        let th = this;
        let _progrss = this.progrss;
        let _dropDownPop = this.dropDownPop; 
        let _documents = this.documents; 
        _dropDownPop.onInitialized = function (e)
        {
            let init = e.component;
            th._dropDownPopInit = init;
            init.option('contentTemplate', () =>
            {
                return $(`<div>`).dxList({
                    elementAttr: { class: 'selection-list' },
                    onInitialized(e)
                    {
                        th._dropDownList = e.component;
                    },
                    dataSource: [],
                    keyExpr: 'ID',
                    displayExpr: 'NAME',
                    searchExpr: 'NAME',
                    selectionMode: 'single',
                    noDataText: 'No Available Options',
                    width: 'auto',
                    hoverStateEnabled: false,
                    focusStateEnabled: true,
                    showSelectionControls: true,
                    searchEnabled: true,
                    //searchExpr: ["ID", "NAME"],
                })
            })
            init.option('toolbarItems',
                [
                    {
                        location: "before",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            width: 230,
                            icon: 'fa-solid fa-paper-plane',
                            text: 'Select',
                            onClick()
                            {
                                let target = th._dropDownList.option('selectedItem');
                                //th._config.targetRef = target.ID;
                                let msg = th.chatMessageBox.option('value');
                                msg = msg.trim();
                                if (msg.length > 0)
                                {
                                    
                                    $.each(th._config, function (e, item)
                                    {
                                        item.targetRef = target.ID;
                                        item.msg = msg; 
                                    });
                                    //th._config.msg = msg; 
                                    th._actionHandler(th._config, th._row.action);
                                };
                            }
                        }
                    },
                ]);
        };
        this.msgEnterPopover.onInitialized = function (e)
        {
            let init = e.component;
            th._msgBoxPopoverInit = init;
            init.option('contentTemplate', () =>
            {
                return $(`<div>`).dxTextArea({ 
                    onInitialized(e)
                    {
                        th._msgTextArea = e.component;
                    },
                    placeHolder: '...Message',
                    onValueChanged(e)
                    { 
                        th.chatMessageBox.option('value', e.value);
                    },
                    elementAttr: { class: 'kzMsgArea' },
                    height: 350,
                })
            })
            init.option('toolbarItems',
                [
                    {
                        location: "before",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            //width: '600',
                            icon: 'save',
                            text: 'Edit',
                            elementAttr: { class: alkanziColors.darkNormalWhite },
                            onClick()
                            {
                                th.chatMessageBox.option('value', th._msgTextArea.option('value'));
                                th._msgBoxPopoverInit.hide();
                            }
                        }
                    },
                ]);
        };
        popup.onInitialized = function (e)
        {
            let compo = e.component;
            th._popupInit = compo;
            th.open = function (config, title, callBack = () => { })
            {
                th.callBack = callBack;
                if (config.length > 1)
                {
                    th._config = config;
                    th._row = config[0];
                    th.chatMessageBox.option('value', 'ok');
                    th.listInit.option(`disabled`, true);
                    th._progrssInit.option(`disabled`, true);
                   
                    //compo.option(`title`, title);
                    th._getDropDownActions();
                    compo.show();
                }
                else
                {  
                    console.log(config)
                    th.listInit.option(`disabled`, false);
                    th._progrssInit.option(`disabled`, false);
                    config[0].userId = _UserId;
                    config[0].sgId = _sgId;
                    th._row = config[0];
                    let request = config[0].request;
                    if (request == "submit")
                    {
                        $.each(config, function (e, item)
                        {
                            item.msg = "ok";
                        }); 
                        //console.log(config, request)
                        //th.callBack();
                        th._actionHandler(config, request);
                    } else
                    {
                        _globals._chatLoader.option(`container`, '#chatApproval')
                        _globals._chatLoader.show();
                        th.chatMessageBox.option('value', 'ok');
                        th._config = config;
                        //th._row = config[0]; 
                        //compo.option(`title`, title);
                        th._getFullLog();
                        th._getDropDownActions();
                        th._getDocProgress();
                        th._getDocuments();
                        th._getDocumentTypes();
                        compo.show();
                    }
                    
                }
                compo.option(`title`, `Process ${th._row.transId}: Approval | Documents | Comments`);
                
            };
            th._fetchDropDownList = () =>
            {
                $.get(`${apis.revertApproval}dropList`, th._row).done((res) =>
                {
                    th._dropDownList.option(`dataSource`, res);
                });
                
            };
            compo.option('toolbarItems',
                [
                    {
                        location: "after",
                        toolbar: "top",
                        widget: 'dxButton',
                        options: {
                            icon: 'close',
                            hoverStateEnabled: false,
                            stylingMode: 'text',
                            onClick(e)
                            {
                                compo.hide();
                            }
                        }
                    },
                    {
                        location: "before",
                        toolbar: "bottom",
                        widget: 'dxTextBox',
                        options: {
                            //height:40,
                            elementAttr: { class: 'chat-input', id:'kzMsgInput' },
                            onInitialized(e)
                            {
                                th.chatMessageBox = e.component;
                            },
                            showClearButton: true,
                            //width: 500,
                            placeholder: 'Type a message',
                            buttons: [
                                {
                                    name: 'sessionNtn',
                                    options: {
                                        icon: 'edit',
                                        elementAttr: { class: alkanziColors.semiYellow },
                                        onClick(e)
                                        {
                                            
                                            th._msgBoxPopoverInit.show();
                                            setTimeout(() =>
                                            {
                                                th._msgTextArea.option(`value`, th.chatMessageBox.option(`value`));
                                            }, 100)
                                        },
                                    },
                                }
                            ],
                            validationRules: [{ type: 'required' }],
                        }
                    }, 
                    {
                        location: "before",
                        toolbar: "bottom",
                        widget: 'dxDropDownButton',
                        options: {
                            elementAttr: { id: 'approvalDropDown' },
                            text: 'Select Action',
                            icon: 'fa-solid fa-paper-plane',
                            onInitialized(e)
                            {
                                th._dropDownActionInit = e.component;
                            },
                            displayExpr: 'name',
                            noDataText: 'No Actions',
                            keyExpr: 'id',
                            useSelectMode: true,
                            dataSource:[],
                            onItemClick(e)
                            {
                                const action = e.itemData.action;
                                th._row.action = action;
                                if (action === 'revertTo' || action === 'rework')
                                {
                                    th._dropDownPopInit.show();
                                    setTimeout(() => { th._fetchDropDownList() }, 100);
                                } else
                                {
                                    let msg = th.chatMessageBox.option('value');
                                    msg = msg.trim();
                                    if (msg.length > 0)
                                    {
                                        $.each(th._config, function (e, item)
                                        {
                                            item.msg = msg;
                                        });
                                        //th._config.msg = msg;
                                        th._actionHandler(th._config, action);
                                    }
                                }

                            },
                        }
                    },
                    {
                        location: "before",
                        toolbar: "bottom",
                        widget: 'dxSelectBox',
                        options: {
                            /*height: 40,*/
                            searchEnabled: true,
                            placeholder:'Upload File',
                            text: "Upload Document",
                            icon: 'fa-solid fa-cloud-arrow-up',
                            dataSource: [],
                            onInitialized(e)
                            {
                                th._documentTypesInit = e.component;
                            },
                            displayExpr: 'NAME',
                            valueExpr: 'ID',
                            onItemClick(e)
                            {
                                let item = e.itemData;
                                if (item != undefined && item != null)
                                {
                                    th._row.fileType = item.NAME;
                                    let msg = th.chatMessageBox.option('value');
                                    th._fileUploaderInit.option('uploadUrl', `/api/documents/upload?transId=${th._row.transId}&docType=${item.ID}&docName=${th._row.docType}&remarks=${akEncode(msg)}`);
                                    //th._fileUploaderInit.option('uploadUrl', `/Pm_Transactions/AlkanziUploadDocument?transId=${th._row.transId}&docType=${item.ID}&docName=${th._row.docType}&remarks=${akEncode(msg)}`);
                                     $("#openUploaderBtn").click(); 
                                }
                                
                            }
                        }
                    },
                    {
                        location: "before",
                        toolbar: "bottom",
                        widget: 'dxFileUploader',
                        visible:false,
                        options: {
                            elementAttr: { id: 'openUploaderBtn' },
                            onInitialized(e)
                            {
                                th._fileUploaderInit = e.component; 
                            },
                            hoverStateEnabled: true,
                            focusStateEnabled: true,
                            activeStateEnabled: true,
                            selectButtonText: 'SELECT PDF / IMAGE',
                            labelText: '', 
                            accept: '.jpg,.jpeg,.gif,.png,.pdf,.jfif,.msg,.xlsx,xlsx',
                            multiple: false,
                            uploadMode: 'useForm',
                            maxFileSize: 15728640,
                            minFileSize: 1,
                            dialogTrigger: "#openUploaderBtn",  
                            uploadMode: "instantly",
                            name: "myFile",
                            uploadMethod: "POST",
                            multiple: true,
                            //uploadUrl: "/upload"
                            onUploaded(e)
                            {
                                //console.log(e.file)
                                //if (th._row.fileType === 'Material Invoice')
                                //{
                                //    uploadInvoice(e.file, th._row.docType, th._row.transId)
                                //}
                              
                                hideLoader();
                                let res = JSON.parse(e.request.response); 
                                if (res.status)
                                {
                                    showIndicator(res.feedback)
                                    th._getDocuments();
                                };
                            }

                        }
                    }
                ]);
        };
        x.onInitialized = function (e)
        {
            let _init = e.component;
            th.listInit = _init;
            _init.option(`itemTemplate`, (data,itemIndex,element) =>
            { 
                let processType = data.PROCESS_TYPE;
                const result = $('<div>').addClass(`chat-item ${data.MSG_TYPE} ${processType}`);
                $('<div>').addClass('chat-user').html(`<b class='chat-process ${processType}'>${processType}</b>  ${data.POSTED_BY}`).appendTo(result);
                $(`<img src="${data.PROFILE}" class="profile-img" />`).appendTo(result);
                const msgContainer = $('<div>').addClass(`message-container`);
                $('<span>').addClass(`chat-message`).html(`${data.MSG}`).appendTo(msgContainer); 
                if (_sgId == 1 && processType !== 'Transaction' && _UserId == 1)
                {
                    $(`<div>`).addClass('chat-toolbar').append(
                        $(`<span>`).addClass(`chat-date`).text(data.POST_DATE),
                        $('<div>').addClass('dx-icon dx-icon-trash').click(() =>
                        {
                            let type = request.delete;
                            let transRow = {
                                SRC: data.SRC,
                                ID: data.ID,
                                CREATED_BY: data.CREATED_BY,
                                REMARKS: data.MSG,
                                CREATED_AT: data.CREATED_AT,
                                APPROVE_STATUS: data.APPROVE_STATUS,
                                type: type,
                            };
                            th._logHandler(transRow, type);
                        }),
                        $('<div>').addClass('dx-icon dx-icon-edit').click((e) =>
                        {
                            let transRow = {
                                SRC: data.SRC,
                                ID: data.ID,
                                CREATED_BY: data.CREATED_BY,
                                REMARKS: data.MSG,
                                CREATED_AT: data.CREATED_AT,
                                APPROVE_STATUS: data.APPROVE_STATUS,
                                FROM_LEVEL: data.FROM_LEVEL,
                                type: request.update,
                            };
                            th.openFormOver(transRow, e.currentTarget);
                        })
                    ).appendTo(msgContainer);
                } else
                {
                    $(`<div>`).addClass('chat-toolbar').append(
                        $(`<span>`).addClass(`chat-date`).text(data.POST_DATE), 
                    ).appendTo(msgContainer);
                };
                msgContainer.appendTo(result);
                return result;

            });
            th._actionHandler = (data, type) =>
            { 
                
                showLoader();
                 $.post(`${apis.revertApproval}?key=0&values=${akEncode(data)}&type=${type}`).done((res) =>
                 {
                     hideLoader();
                     console.log(res)
                     if (data.length > 1)
                     {
                         bulkStatus.open(res, `Bulk Approval`);
                         th._popupInit.hide();
                         showIndicator(`Process Complete Successfully`);
                         setTimeout(() =>
                         {
                             bulkStatus.hidePopup();
                         }, 800);
                         th.callBack();
                     } else
                     {
                         if (res[0].status)
                         {
                             showIndicator(res[0].feedback);
                             th._getFullLog();
                             th._getDropDownActions();
                             th._getDocProgress();
                             th.callBack();
                             if (type === 'revertTo' || type === 'rework')
                             {
                                 th._dropDownPopInit.hide();
                             }
                         } else
                         {
                             showAlert(res[0].feedback);
                         }
                         
                     }
                    
                }).fail(function (res)
                {
                    hideLoader();
                    console.log(res)
                    errorAlert(res.feedback);
                });
            };
            th._logHandler = (data, type) =>
            {
                editConfirm('Approval Log', `Confirm ${type} ?!`, 'default')
                    .show().done(function (dialogResult)
                    {
                        if (dialogResult)
                        {
                            $.post(`${apis.approvalProgress}logDetails?key=${data.ID}&values=${akEncode(data)}&type=${type}`).done((res) =>
                            {
                                //console.log(res)
                                if (res.status)
                                {
                                    showIndicator(res.feedback);
                                    th._getFullLog();
                                } else
                                {
                                    showAlert(res.feedback);
                                }
                            }).fail(function (res)
                            {
                                console.log(res)
                                errorAlert(res.feedback);
                            });
                        }
                    });
               
            };
        };
        _progrss.onInitialized = function (e)
        {
            th._progrssInit = e.component;
            e.component.option(`itemTemplate`, (data) =>
            {
                const $buttonContent = $('<div>').addClass('workflow-vertical');
                // Profile photo in the circle; fall back to a user avatar icon when
                // there's no photo or it fails to load. The icon sits next to the img
                // (hidden) and the img's onError hides itself and reveals the icon.
                const _fallbackIcon = `<i class="fa-solid fa-circle-user" style="width:100%;height:100%;display:flex;align-items:center;justify-content:center;font-size:22px;"></i>`;
                const _avatar = data.USER_PROFILE
                    ? `<img src="${data.USER_PROFILE}" onError="this.style.display='none';this.nextElementSibling.style.display='flex';" style="width:100%;height:100%;border-radius:50%;object-fit:cover;" />${_fallbackIcon.replace('display:flex', 'display:none')}`
                    : _fallbackIcon;
                $buttonContent.append(
                    $(`<div class='step ${data.STATUS}  ${data.IS_FINAL}'>`).html(`<div class="circle">${_avatar}</div>
      <div class="content">
        <div class="label">${data.LABEL}</div>
        <div class="desc usr">${data.USER_NAME}</div>
        <div class="desc">${data.POST_DATE}</div>
      </div>`)
                ).click(() =>
                {
                    if (_sgId == 1 && _UserId == 1)
                    {
                        let transRow = {
                            SRC: data.SRC,
                            ID: data.ID,
                            CREATED_BY: data.CREATED_BY,
                            REMARKS: data.MSG,
                            CREATED_AT: data.CREATED_AT,
                            APPROVE_STATUS: data.ACTION == 0 ? 1 : data.ACTION,
                            FROM_LEVEL: data.TRANS_STATUS,
                            TRANS_ID: data.TRANS_ID,
                            DOC_TYPE: data.DOC_TYPE,
                            type: data.ID == 0 ? request.insert : request.update,
                        };
                        th.openFormOver(transRow, $buttonContent);
                    }
                    
                   
                });
                return $buttonContent;
            })
        }; 
        _documents.onInitialized = (e) =>
        {
            th._documentsInit = e.component;
            e.component.option(`itemTemplate`, (data, itemIndex) =>
            {
                //console.log(data, itemIndex)
                const $buttonContent = $('<div>');
                let iconClass = 'fa-solid fa-file-image';
                if (data.FILE_TYPE === 'PDF')
                {
                    iconClass = 'fa-regular fa-file-pdf';
                } else if (data.FILE_TYPE === 'XLSX')
                {
                    iconClass = 'fa-solid fa-file-excel';
                } else if (data.FILE_TYPE === 'DOCX')
                {
                    iconClass = 'fa-solid fa-file-word';
                };
                let deleted = data.IS_DELETED == 1 ? 'Deleted' : 'Active';
                $buttonContent.append(
                    $(`<div class='step'>`).html(`
                  <div class="content">
                    <span class="${iconClass} icon ${data.FILE_TYPE}"></span>
                    <div class="document-label">${data.REMARKS}</div>
                    <div class="desc docType">${data.DOC_TYPE_NAME}</div> 
                    <div class="desc user">By: ${data.CREATED_BY}</div> 
                    <div class="desc src"><b>Src:</b> ${data.FROM_SRC}</div> 
                    <div class="desc date">${data.CREATED_AT}</div>
                    <div class="desc ${deleted}">${deleted}</div>
                  </div>`)
                );
                if ((_sgId == 1 || _sgId == 1281) && deleted === 'Active')
                {
                    $('<span>').addClass(`dx-icon dx-icon-trash action-icon`).click(() =>
                    {
                        //console.log(data)
                        editConfirm('Document', `Confirm delete the document ?!`, 'default')
                            .show().done(function (dialogResult)
                            {
                                if (dialogResult)
                                {
                                    return th._documentsAction({ ID: data.ID });
                                }
                            });
                    }).appendTo($buttonContent);
                }
                $('<span>').addClass(`fa-solid fa-download action-icon`).click(() =>
                {
                    //th._documentsInit.selectItem(itemIndex);
                    th.print('download', data);
                }).appendTo($buttonContent);
                $('<span>').addClass(`fa-solid fa-eye action-icon`).click(() =>
                {
                    th.print('open', data);
                }).appendTo($buttonContent); 
                return $buttonContent;
            })
        };
        this.chatFormPopover.onInitialized = function (e)
        {
            let init = e.component;
            th.chatFormPopoverInit = init; 
            init.option(`contentTemplate`, () =>
            {
                return $(`<div>`).dxForm({
                    onInitialized(e)
                    {
                        th.chatFormInit = e.component;
                    },
                    focusStateEnabled: true,
                    scrollingEnabled: true,
                    showRequiredMark: true,
                    formData: {},
                    labelMode: "floating",
                    labelLocation: 'top',
                    colCount: 2,
                    items: [
                        {
                            dataField: 'CREATED_AT',
                            label: { text: 'Posted At' },
                            editorType: 'dxDateBox',
                            editorOptions: {
                                type: 'datetime',
                                width: '100%',
                                displayFormat: 'dd-MMM-yyyy HH:mm',
                                //displayFormat: _dateFormat,
                            }
                        },
                        {
                            dataField: 'CREATED_BY',
                            label: { text: 'Posted By' },
                            editorType: 'dxSelectBox',
                            editorOptions: {
                                dataSource: {
                                    sort: { selector: "FULL_NAME", desc: false },
                                    store: new DevExpress.data.CustomStore({
                                        key: "ID",
                                        cacheRawData: true,
                                        loadMode: "raw",
                                        load()
                                        {
                                            return $.get(`${apis.users}/forProcess`)
                                        },
                                    }),
                                },
                                valueExpr: 'ID',
                                displayExpr: 'FULL_NAME',
                                searchEnabled: true,
                            },
                        },
                        {
                            dataField: 'APPROVE_STATUS',
                            label: { text: 'Status' },
                            editorType: 'dxSelectBox',
                            editorOptions: {
                                dataSource: {
                                    sort: { selector: "NAME", desc: false },
                                    store: new DevExpress.data.CustomStore({
                                        key: "ID",
                                        cacheRawData: true,
                                        loadMode: "raw",
                                        load()
                                        {
                                            return $.get(apis.approveStatus)
                                        },
                                    }),
                                },
                                valueExpr: 'ID',
                                displayExpr: 'NAME',
                                searchEnabled: true,
                            }
                        },
                        {
                            //TRANS_STATUS
                            dataField: 'FROM_LEVEL',
                            label: { text: 'Level' },
                            editorType: 'dxNumberBox',
                            editorOptions: {min:0,showClearButton:true},
                        },
                        //{
                        //    itemType: 'empty',
                        //    colSpan:2,
                        //},
                        {
                            dataField: 'REMARKS',
                            colSpan: 2,
                            editorType: 'dxTextArea',
                            editorOptions: {
                                height: 90,
                                showClearButton: true,
                            },
                            validationRules: [{ type: 'required' }],
                        },
                        {
                            itemType: 'group',
                            colSpan: 2,
                            cssClass: 'buttons-group',
                            colCountByScreen: {
                                xs: 3,
                                sm: 3,
                                md: 3,
                                lg: 3,
                            },
                            items: [
                                {
                                    itemType: 'button',
                                    name: 'Reset',
                                    buttonOptions: {
                                        onClick: () =>
                                        {
                                            init.hide();
                                        },
                                        icon: 'close',
                                        text: 'Close',
                                        width: '100%',
                                    },
                                },
                                {
                                    itemType: 'button', 
                                    name: 'chatFormDeleteBtn',
                                    buttonOptions: {
                                        onClick: () =>
                                        {
                                            let data = th.chatFormInit.option('formData');
                                            th._logHandler(data, request.delete);
                                        },
                                        icon: 'trash',
                                        text: 'Delete',
                                        type: 'danger',
                                        width: '100%',
                                    },
                                },
                                {
                                    itemType: 'button',
                                    buttonOptions: {
                                        text: 'Save',
                                        elementAttr: { class: alkanziColors.popupToolbtn },
                                        onClick: () =>
                                        {
                                            let data = th.chatFormInit.option('formData'); 
                                            th._logHandler(data,data.type);
                                        },
                                        width: '100%',
                                    },
                                },
                            ],
                        },

                    ],
                })
            });

        };
        th._getFullLog = () =>
        {
            $.get(apis.revertApproval, th._row, (res) =>
            {
                th.listInit.option(`dataSource`, res);
                //console.log(res)
                setTimeout(() => { th.listInit.scrollToItem(res.length - 1) }, 100);
            });
        };
        th._getDropDownActions = () =>
        {
            $.get(`${apis.revertApproval}dropAction`, th._row).done((res) =>
            {
                th._dropDownActionInit.option(`dataSource`, res);
            });
        };
        th._getDocProgress = () =>
        {
            $.get(`${apis.approvalProgress}docProgress`, th._row).done((res) =>
            {
                th._progrssInit.option(`dataSource`, res);
            });
        };
        th._getDocumentTypes = () =>
        { 
            $.get(`${apis.documents}${th._row.docType}/doctypes`).done((res) =>
            {
                th._documentTypesInit.option(`dataSource`, res);
            }); 
        };
        th._getDocuments = () =>
        {
            //console.log(th._row)
            let url = `/api/documents?docName=${th._row.docType}&id=${th._row.transId}`;
            //if (th._row.docType === 'customer')
            //{
            //    url = `/api/documents/owner/${th._row.docType}/${th._row.transId}`;
            //};
            th._documentsInit.option(`dataSource`, []);
            $.get(url).done((res) =>
            {
                th._documentsInit.option(`dataSource`, res.success ? res.data : []);
            });
             
        };
        th._documentsAction = (data) =>
        {
            $.post(`${apis.documents}?key=${data.ID}&values=${akEncode(data)}&type=${request.delete}`).done((res) =>
            {
                if (res.status)
                {
                    showIndicator(`File Removed Successfully`);
                    th._getDocuments();
                };
                
            });

        };
        th.openFormOver = (data, target) =>
        {
            console.log(data) 
            th.chatFormPopoverInit.show(target);
            setTimeout(() =>
            {
                th.chatFormInit.getEditor(`APPROVE_STATUS`).option('readOnly', data.SRC !== 'APPR_LOG');;
                th.chatFormInit.option('formData', data);
                th.chatFormInit.getButton('chatFormDeleteBtn').option(`disabled`, data.ID == 0);
            }, 100); 
        }
    }
};
class alkanziFormPopup
{
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
        screenByWidth: function (width)
        {
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
        wrapperAttr: { class: 'form-tab-panel-popup alkanzi-custom-popup' },
    };
    workflowSteps = {
        items:[],
    }
    constructor(config)
    {
        //InitColCount = 1, showSubmit = true, module = 'all', saveBtnText = 'Save', popupClass = ''
        config.saveBtnText = config.saveBtnText == undefined ? 'Save' : config.saveBtnText;
        config.showSubmit = config.showSubmit == undefined ? true : config.showSubmit;
        config.colCount = config.colCount == undefined ? 1 : config.colCount;
        var pop = this.popup;
        var f = this.form;
        let th = this;
        let wfSteps = this.workflowSteps;
        wfSteps.onInitialized = (e) => {
            let init = e.component;
            //init.option(`items`, workflowSteps.map(function (step, index) {
            //    return {
            //        location: "before",
            //        template: function () {
            //            return buildStepHTML(step, index, workflowSteps.length);
            //        }
            //    };
            //}));
        };
        pop.onInitialized = function (e)
        {
            let init = e.component;
            if (config.toolbarColor != undefined)
            {
                init.option(`wrapperAttr`, { class: `form-tab-panel-popup alkanzi-custom-popup ${config.toolbarColor}` });
            }
            th.popupInit = e.component;
            th.hidePopup = function ()
            {
                init.hide();
            };
            th.showPopup = function (show = true)
            {
                if (show == undefined)
                {
                    show = true;
                }
                init.option('toolbarItems[1].options.visible', show)
                init.show();
            };
            th.set = function (title, data)
            {
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
            th.popupTitle = function (title)
            {
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
                            onClick(e)
                            {
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
                            onClick(e)
                            {
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
                            onClick(e)
                            {
                                th._isMinimized = false;
                                init.hide();
                            }
                        }
                    },
                    {
                        location: "after",
                        toolbar: "bottom",
                        widget: 'dxButton',
                        options: {
                            text: 'Exit',
                            //visible:false,
                            elementAttr: { class: alkanziColors.softGray },
                            onClick() {
                                th._isMinimized = false;
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
                            text: config.saveBtnText,
                            elementAttr: { class: alkanziColors.softGray },
                            visible: config.showSubmit,
                            onInitialized(e)
                            {
                                th._submitBtnInit = e.component;
                            },
                            onClick()
                            {
                                let validate = th.formInit.validate();
                                if (validate == undefined)
                                {
                                    th.submit();
                                } else
                                {
                                    if (validate.isValid)
                                    {
                                        th.submit();
                                    }
                                }

                            },
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
        f.onInitialized = function (e)
        {
            let comp = e.component;
            th.formInit = comp;
            comp.option('colCount', config.colCount);
            th.getData = function ()
            {
                return comp.option('formData');
            };
            th.setData = function (data)
            {
                comp.option('formData', data);
                if (data.DOC_TYPE != undefined && data.DOC_TYPE != null) {
                    _loadApprovalProgress(data.DOC_TYPE, data.ID);
                }
            };
            th.getValueOf = function (name)
            {
                return comp.getEditor(name).option('value');
            };
            th.setValueOf = function (name, val)
            {
                return comp.getEditor(name).option('value', val);
            };
            th.getSourceOf = function (name, data)
            {
                return comp.getEditor(name).option('dataSource', data);
            };
            th.setSourceOf = function (name, data)
            {
                return comp.getEditor(name).option('dataSource', data);
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
            th.focusItem = function (name)
            {
                comp.getEditor(name).focus();
            };
        };
    }
};
const general = new generalAccess();

// ============================================================
// kzProgressList — global horizontal workflow progress strip.
// Same pattern as serviceLPO.js (_loadApprovalProgress in consts.js):
// renders <div class="step …"> children directly into a plain
// <div class="workflow-horizontal"> container so the existing
// dashboard.css horizontal layout works without dxList wrappers.
//
// Markup inside the popup body:
//   <div id="<formVar>Progress" class="workflow-horizontal" style="display:none"></div>
//
// Usage in a controller:
//   kzProgressList.load('#<formVar>Progress', docType, transId);
// load() with id=0 (or no progress rows yet) automatically falls
// through to loadTemplate(), which previews the workflow stages.
// ============================================================
const kzProgressList = {
    // Back-compat shim — earlier draft returned a dxList config object.
    // Kept so existing `$scope.xxxProgress = kzProgressList.config()`
    // lines don't break; load() detects strings (selectors) vs objects.
    config() { return { _ref: null }; },

    _renderSteps($container, rows) {
        $container.empty();
        $.each(rows, function (i, data) {
            $('<div>')
                .addClass('step ' + (data.STATUS || '') + ' ' + (data.IS_FINAL || ''))
                .html(
                    '<div class="circle">' + (data.STATUS_ID != null ? data.STATUS_ID : '') + '</div>'
                    + '<div class="content">'
                    +     '<div class="label">' + (data.LABEL || '') + '</div>'
                    +     '<div class="desc usr">' + (data.USER_NAME || '') + '</div>'
                    +     '<div class="desc">' + (data.POST_DATE || '') + '</div>'
                    + '</div>'
                )
                .appendTo($container);
        });
        $container.show();
    },

    _resolve(target) {
        // Accepts a CSS selector, a DOM element, a jQuery object, or the
        // legacy `_ref` config object (unused now but tolerated).
        if (!target) return $();
        if (target._ref !== undefined) return $();
        return $(target);
    },

    load(target, docType, id) {
        const $container = kzProgressList._resolve(target);
        if (!$container.length) return;
        if (!(id > 0)) {
            kzProgressList.loadTemplate(target, docType);
            return;
        }
        $.get(apis.approvalProgress + 'docProgress', { docType: docType, transId: id }).done(function (res) {
            const rows = Array.isArray(res) ? res : [];
            if (rows.length === 0) {
                kzProgressList.loadTemplate(target, docType);
            } else {
                kzProgressList._renderSteps($container, rows);
            }
        });
    },

    // Fetches the first workflow for `docType` and renders its levels as
    // pending steps so the strip previews the pipeline before save.
    loadTemplate(target, docType) {
        const $container = kzProgressList._resolve(target);
        if (!$container.length) return;
        $.get(apis.workflows + 'byDoctype', { doc: docType }).done(function (workflows) {
            const wfs = Array.isArray(workflows) ? workflows : [];
            if (!wfs.length) { $container.empty().hide(); return; }
            const wfName = wfs[0].NAME || '';
            $.get(apis.workflows + 'levels/' + wfs[0].ID).done(function (levels) {
                const arr = Array.isArray(levels) ? levels : [];
                const rows = arr.map(function (lvl, i) {
                    return {
                        STATUS_ID: lvl.LEVEL_ID != null ? lvl.LEVEL_ID : (i + 1),
                        STATUS:    'pending',
                        IS_FINAL:  (i === arr.length - 1) ? 'final' : '',
                        LABEL:     lvl.REMARKS || ('Level ' + (lvl.LEVEL_ID || (i + 1))),
                        USER_NAME: wfName,
                        POST_DATE: '',
                    };
                });
                kzProgressList._renderSteps($container, rows);
            }).fail(function () { $container.empty().hide(); });
        }).fail(function () { $container.empty().hide(); });
    },
};
const customAlert = function (msg)
{
    console.log(msg)
    if (msg != undefined) {
        let startIndex = msg.indexOf('ORA-20100:');
        if (startIndex > -1) {
            let endIndex = msg.indexOf('ORA-06512:');
            if (endIndex > -1) {
                let baseMsg = msg.substring(10, endIndex - startIndex);
                return $('<center>').append($(`<h4>`).html(baseMsg));
            }

        }
        return $('<center>').append($(`<h4>`).html(msg))
    };
}
const showAlert = function (msg) {
    $("#showAlertPopup").dxPopup('option', 'visible', true);
    $("#showAlertPopup").dxPopup('option', 'contentTemplate', customAlert(msg));

    //customAlert('Process Failed', msg).show();
}
const addBookMark = function (data) {
    //let doc = getDocName();
    //let row = { DOC_TYPE: doc, TRANS_ID: data.ID, DOC_NUM: data.DOC_NUM, REMARKS: 'No Remarks', HDR_ID: 1 };
    //row = setInsertDefaultParams(row);
    //showLoader();
    //$.post(`${apis.bookmarks}details`, { type: request.insert, data: row }).done((res) => {
    //    hideLoader();
    //    if (res.status) {
    //        showIndicator(res.feedback, 'success');           
    //    } else {
    //        showAlert(res.feedback)
    //    }
    //});
}
// iPhone-style status icon map (FIB design)
const _notifStatusIcon = {
    'Success': { icon: 'fa-circle-check',       color: '#34C759' },
    'Info':    { icon: 'fa-circle-info',        color: '#007AFF' },
    'Warning': { icon: 'fa-triangle-exclamation', color: '#FF9500' },
    'Urgent':  { icon: 'fa-circle-exclamation', color: '#FF3B30' },
};

// Map Flexion's NOTI_TYPE / CATEGORY to the FIB STATUS bucket.
const _notifNotiTypeToStatus = {
    APPROVE: 'Success',
    SUBMIT:  'Info',
    COMMENT: 'Info',
    REJECT:  'Urgent',
    REWORK:  'Warning',
};

const _notifTimeAgo = function (dateStr) {
    if (!dateStr) return 'now';
    const now = new Date();
    const date = new Date(dateStr);
    const diff = Math.floor((now - date) / 1000);
    if (isNaN(diff) || diff < 60) return 'now';
    if (diff < 3600)  return Math.floor(diff / 60)    + 'm ago';
    if (diff < 86400) return Math.floor(diff / 3600)  + 'h ago';
    return Math.floor(diff / 86400) + 'd ago';
};

// Resolve the iOS-toast STATUS bucket from any of the shapes we receive
// (FIB push payload, Flexion KZ_NOTIFICATIONS feed item, or SignalR push).
const _notifResolveStatus = function (item) {
    if (!item) return 'Info';
    if (_notifStatusIcon[item.STATUS]) return item.STATUS;
    if (item.NOTI_TYPE && _notifNotiTypeToStatus[item.NOTI_TYPE]) return _notifNotiTypeToStatus[item.NOTI_TYPE];
    if (item.CATEGORY) {
        const c = String(item.CATEGORY);
        if (_notifStatusIcon[c]) return c;
        const lc = c.toLowerCase();
        if (lc === 'success' || lc === 'approval') return 'Success';
        if (lc === 'warning' || lc === 'rework')   return 'Warning';
        if (lc === 'error'   || lc === 'urgent' || lc === 'reject') return 'Urgent';
    }
    return 'Info';
};

// Backwards-compat alias for SignalR callbacks (notificationHub.js calls
// notification(data) on sendNotification/prfNotification). Routing
// everything through notiMessage gives the FIB iOS look.


function akEncode(data)
{
    return encodeURIComponent(JSON.stringify(data))
};

async function uploadInvoice(file,docType,transId)
{
    //const fileInput = document.getElementById("fileInput");
    //const file = fileInput.files[0];
    console.log(transId, docType)
    console.log(file)
    if (!file)
    {
        showAlert("Please select a file");
        return;
    } 
    const formData = new FormData();
    formData.append("File", file);          // must match your model property
    formData.append("DocType", docType);  // example
    formData.append("DocId", transId);          // example

    const response = await fetch("https://localhost:7107/api/openAI/extract-invoice", {
        method: "POST",
        body: formData
    });

    if (!response.ok)
    {
        const error = await response.text();
        console.error("Upload failed:", error);
        alert("Upload failed");
        return;
    } 
    const result = await response.json();
    console.log("API result:", result);
    alert("Upload successful");
}

