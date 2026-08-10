# query to get user transactions
```sql
SELECT A.*,B.HDR_ID FORM_ID,B.LEVEL_ID,C.NAME,C.LAST_LEVEL,C.DOC_ID,D.DOC_TYPE,D.DISPLAY_NAME,C.TABLE_NAME FROM 
(SELECT  DISTINCT A.SECURITY_GROUP_ID FROM SM_DIVISION_SECURITY_GROUPS_USERS A WHERE IS_DELETED = 0 AND USER_ID = 2144 )A
,SM_WORKFLOW_LVL_SECURITY_GROUPS B,SM_WORKFLOW_FORMS C
,FM_TRANSACTION_MENU D
WHERE  A.SECURITY_GROUP_ID = B.SECURITY_GROUP_ID
AND B.HDR_ID = C.ID
AND B.IS_DELETED = 0 AND C.IS_DELETED = 0
AND C.DOC_ID = D.ID 

### LEVEL_ID is user level in the workflow 
# result
```
601	5	Asset Purchase Txn	1	1	113	AssetPurchase	AssetPurchase	AM_TRANS_HDR
601	12	Unit Ownership	2	1	141	unitOwnership	unitOwnership	PM_UNIT_OWNERSHIP_HEADER
601	12	Unit Ownership	2	2	141	unitOwnership	unitOwnership	PM_UNIT_OWNERSHIP_HEADER
601	15	Credit Notes	3	2	162	CreditNotes	CreditNotes	FM_CRDR_NOTE_HEADER
601	23	Upload Receipts	1	1	105	UploadReceipts	UploadReceipts	FM_RECEIPTS_MASTER
601	30	AP Deposit	2	2	120	APDeposit	APDeposit	FM_FUND_MASTER
601	32	AR Deposit	1	1	121	ARDeposit	AR Deposit	FM_FUND_MASTER
601	33	Fund Termination	3	1	125	FundTerminate	FundTerminate	FM_FUND_MASTER
601	33	Fund Termination	3	3	125	FundTerminate	FundTerminate	FM_FUND_MASTER
601	34	AP Return	2	2	123	APReturn	APReturn	FM_FUND_MASTER
601	35	AP Cancellation	2	2	122	APCancellation	APCancellation	FM_FUND_MASTER
601	51	Fund Return	2	1	107	ARFundReturn	ARFundReturn	FM_FUND_MASTER
601	51	Fund Return	2	2	107	ARFundReturn	ARFundReturn	FM_FUND_MASTER
601	52	Receipt Uploaded Ajman	1	1	461	AjmUploadReceipts	AjmUploadReceipts	FM_RECEIPTS_MASTER
601	53	Ajman Receipts	1	1	462	AjmanReceipts	AjmanReceipts	FM_RECEIPTS_MASTER
601	56	Fund Return Ajman	2	2	503	FundReturnAjm	FundReturnAjm	FM_FUND_MASTER
601	57	Fund Termination Ajm	2	3	504	FundTerminationAjm	FundTerminationAjm	FM_FUND_MASTER
601	58	AR Deposit Ajm	2	2	502	ARDepositAjm	ARDepositAjm	FM_FUND_MASTER
601	59	Debit Note Ajman	1	1	522	DebitNotesAjm	DebitNotesAjm	FM_CRDR_NOTE_HEADER
601	60	Credit Notes Ajman	3	2	521	CreditNotesAjm	CreditNotesAjm	FM_CRDR_NOTE_HEADER
601	61	CrNoteInv	3	2	523	CrNoteInv	CrNoteInv	FM_CRDR_NOTE_HEADER
601	64	Service Sales	2	2	582	SrvSales	SrvSales	FM_SERVICE_HDR
601	65	Parking Receipts	2	2	601	ParkingReceipts	ParkingReceipts	FM_RECEIPTS_MASTER
601	66	Wifi Receipts	2	2	602	WifiReceipts	WifiReceipts	FM_RECEIPTS_MASTER
601	74	Journal Voucher(<=100K)	2	1	1	JournalVoucher	Journal Voucher	FM_JOURNAL_HDR
601	74	Journal Voucher(<=100K)	2	2	1	JournalVoucher	Journal Voucher	FM_JOURNAL_HDR
601	80	Asset Register	1	1	801	AssetRegister	AssetRegister	AM_REGISTER
601	81	Misc Receipt	2	2	821	MiscReceipt	MiscReceipt	FM_RECEIPTS_MASTER
601	84	Internal Receipts	2	2	841	InternalReceipts	InternalReceipts	FM_RECEIPTS_MASTER
601	87	Petty Cash > 5000	3	3	882	GeneralPettyCash	GeneralPettyCash	FM_PETTY_CASH_HDR
601	93	Customer Payment	2	2	981	CustomerPayment	CustomerPayment	FM_PAYMENT_VOUCHER
601	94	Owner Payment	2	2	982	OwnerPayment	OwnerPayment	FM_PAYMENT_VOUCHER
601	103	Supplier Payment Voucher	2	2	1241	SupVoucher	SupVoucher	FM_PAYMENT_VOUCHER
601	104	General Payment Voucher	2	2	8	PaymentVoucher	Payment Voucher	FM_PAYMENT_VOUCHER
601	105	Employee Requisition	3	1	1161	requisition	requisition	HRM_EMPLOYEE_REQUISITIONS
846	105	Employee Requisition	3	1	1161	requisition	requisition	HRM_EMPLOYEE_REQUISITIONS
601	112	Debit Notes Inv	2	2	1261	DebitNotesInv	DebitNotesInv	FM_CRDR_NOTE_HEADER
601	113	Owner Service	2	2	1262	OwnerSaleService	OwnerSaleService	FM_SERVICE_HDR
601	1321	AM Depreciation	2	2	2164	AssetDepreciation	AssetDepreciation	AM_DEPRECIATION_HDR
601	1323	MLC Receipt	3	3	1485	MLCReceipt	MLCReceipt	FM_RECEIPTS_MASTER
601	1341	GL Receipt Miscs	2	1	1546	GLReceipt	GLReceipt	FM_RECEIPTS_MASTER
601	1341	GL Receipt Miscs	2	2	1546	GLReceipt	GLReceipt	FM_RECEIPTS_MASTER
601	1342	Lease Commission	4	2	1549	lcomm	lcomm	LSP_COMMIS_DSHBRD_TRANS
601	1371	Change Bank Account Request	3	1	1573	baccapp	baccapp	HRM_CH_BANK_ACC_REQUEST
846	1371	Change Bank Account Request	3	1	1573	baccapp	baccapp	HRM_CH_BANK_ACC_REQUEST
601	1421	Sale Commission	4	1	1661	scomm	scomm	LSP_COMMIS_DSHBRD_TRANS
601	1441	Account Payee	2	2	1701	PayeeAccounts	PayeeAccounts	FM_SUPPLIER_PAYEE
601	1442	Leave Report Back	2	1	1702	LeaveReportBack	LeaveReportBack	HRM_LEAVE_RPORT_BACK_REQ
846	1442	Leave Report Back	2	1	1702	LeaveReportBack	LeaveReportBack	HRM_LEAVE_RPORT_BACK_REQ
601	1541	Loan Cash Installment	3	3	1981	loanCashInstallment	loanCashInstallment	HRM_LOAN_REQUEST_CASH_INSTALLM
601	1561	Compensatory Leave	3	1	1821	compLeave	compLeave	HRM_COMPENSATORY_LEAVE_REQUEST
846	1561	Compensatory Leave	3	1	1821	compLeave	compLeave	HRM_COMPENSATORY_LEAVE_REQUEST
601	1581	ASSET GRN	2	2	2162	AssetGrn	AssetGrn	AM_TRANS_HDR
601	1582	Asset Dispose	2	2	2163	AssetDispose	AssetDispose	AM_TRANS_HDR
601	1601	Lease Sales	2	2	1929	LeaseSales	LeaseSales	FM_SERVICE_HDR
601	1621	Attendance Justification Approval	3	2	2204	attJustification	attJustification	EMPATTENDANCE_JUSTIFICATION
846	1621	Attendance Justification Approval	3	2	2204	attJustification	attJustification	EMPATTENDANCE_JUSTIFICATION
601	1681	Reimbursement Application	3	1	2363	ariesapp	ariesapp	HRM_ARIES_REQUEST
846	1681	Reimbursement Application	3	1	2363	ariesapp	ariesapp	HRM_ARIES_REQUEST
601	1741	Sales Contracts	2	1	2587	salesContract	Sales Contracts	CO_CONTRACTS
601	1742	General Receipt	2	2	2588	GeneralReceipts	GeneralReceipts	FM_RECEIPTS_MASTER
601	1901	Salary Certificate Application	3	1	2881	letterapp	letterapp	HRM_LETTER_REQUEST
846	1901	Salary Certificate Application	3	1	2881	letterapp	letterapp	HRM_LETTER_REQUEST
601	1922	Performance Appraisal	3	1	2918	performanceApp	performanceApp	APPRAISAL_MASTERS
846	1922	Performance Appraisal	3	1	2918	performanceApp	performanceApp	APPRAISAL_MASTERS
601	1961	Srv Purchase	2	2	2993	SrvPurchase	SrvPurchase	FM_SERVICE_HDR
601	2381	Owner BPS	6	4	4942	prfTransOwner	PRF OWNER	PRF_TRANSACTIONS
601	2383	BPS Transactions	2	1	4550	prfTrans	PRF TRANS	PRF_TRANSACTIONS
846	2383	BPS Transactions	2	1	4550	prfTrans	PRF TRANS	PRF_TRANSACTIONS
601	2401	Petty Cash (HC)	3	3	3621	fmPettyCash	Petty Cash	FM_PETTYCASH
601	2461	Stock Adjustment	2	2	7341	stockAdjustment	Stock Adjustment	IM_STOCK_ADJUSTMENT
601	2481	PRO Expenses	2	2	7349	proExpenses	proExpenses	HRM_PRO_EXPENSES_HDR
601	2702	Legal BPS 2	7	1	14451	prfTransLegal	PRF LEGAL	PRF_TRANSACTIONS
601	2721	BPS PROJECT FP	8	6	14758	prfTransProject	PRF Project	PRF_TRANSACTIONS
601	2722	BPS PROJECT FP-PALM	9	7	14758	prfTransProject	PRF Project	PRF_TRANSACTIONS
601	2723	BPS PROJECT FPD	9	7	14758	prfTransProject	PRF Project	PRF_TRANSACTIONS
601	2724	BPS HR GENERAL	6	5	14759	prfTransHR	PRF HR	PRF_TRANSACTIONS
601	2725	BPS HR GENERAL - FPD	6	5	14759	prfTransHR	PRF HR	PRF_TRANSACTIONS
601	2726	BPS IT GENERAL	8	6	14767	prfTransIT	PRF IT	PRF_TRANSACTIONS
601	2727	BPS IT GENERAL - FPD	8	6	14767	prfTransIT	PRF IT	PRF_TRANSACTIONS
601	2741	BPS FPD	6	4	14850	prfTransFPD	PRF FPD	PRF_TRANSACTIONS
601	2762	Admin BPS FPD	9	7	15374	prfTransAdmin	PRF Admin	PRF_TRANSACTIONS
601	2763	Journal Voucher(>100K)	3	1	1	JournalVoucher	Journal Voucher	FM_JOURNAL_HDR
601	2763	Journal Voucher(>100K)	3	2	1	JournalVoucher	Journal Voucher	FM_JOURNAL_HDR
601	2764	Treppan BPS	7	5	15377	prfTransTreppan	PRF TREPPAN	PRF_TRANSACTIONS
601	2765	Finance FP BPS	6	1	15384	prfTransFP	PRF FP	PRF_TRANSACTIONS
601	2765	Finance FP BPS	6	4	15384	prfTransFP	PRF FP	PRF_TRANSACTIONS
601	2766	MOA BPS	8	6	15385	prfTransMOA	PRF MOA	PRF_TRANSACTIONS
601	2782	BPS FPD-BOOK ONLY	4	4	14850	prfTransFPD	PRF FPD	PRF_TRANSACTIONS
601	2841	BPS Procurement	7	1	16003	prfTransProc	PRF PROC.	PRF_TRANSACTIONS
601	2842	Finance Treppan BPS	5	3	16007	prfTransTreppanFin	PFR TREPPAN FIN	PRF_TRANSACTIONS
601	2844	Journal Voucher Ahma	2	1	15493	JournalVoucherAhma	JournalVoucherAhma	FM_JOURNAL_HDR
601	2844	Journal Voucher Ahma	2	2	15493	JournalVoucherAhma	JournalVoucherAhma	FM_JOURNAL_HDR
601	2845	BPS UK	8	6	16315	prfTransUK	PRF UK	PRF_TRANSACTIONS
601	2846	BPS UK FIN	5	3	16316	prfTransUKFin	PRF UK FIN	PRF_TRANSACTIONS
601	2862	BPS CSD	8	6	16323	prfTransCSD	PRF CSD	PRF_TRANSACTIONS
601	2961	Marketing BPS	8	6	19001	prfTransMarketing	prfTransMarketing	PRF_TRANSACTIONS
601	2981	BPS PROJECT RAK	7	5	14758	prfTransProject	PRF Project	PRF_TRANSACTIONS
601	3001	Initiator, Project Mgr, Director of Business & Operations, CAO, CFO, Secretary, CEO	7	5	13941	serviceLPO	Service LPO	FM_PURCHASE_ORDER
601	3023	Inventory LPO - IT	5	4	5041	imPurchaseOrder	Purchase Order	IM_PURCHASE_ORDERS
601	3043	Service Charges BPS	7	5	15739	prfTransService	prfTransService	"	PRF_TRANSACTIONS"
601	3061	Initiator, Project Mgr, Director of Business & Operations, CAO, CFO, Secretary, CEO	7	5	13941	serviceLPO	Service LPO	FM_PURCHASE_ORDER
601	3081	BPS PROJECT FPD-ESCROW	9	7	14758	prfTransProject	PRF Project	PRF_TRANSACTIONS
601	3121	BPS ROI	7	5	20633	prfTransRoi	prfTransRoi	PRF_TRANSACTIONS
601	3142	Service Charges BPS-Treppan	7	5	15739	prfTransService	prfTransService	PRF_TRANSACTIONS
601	3143	Service Charges BPS-FPD	7	5	15739	prfTransService	prfTransService	PRF_TRANSACTIONS
601	3201	Initiator, IT Mgr, CFO, CEO	4	3	13941	serviceLPO	Service LPO	FM_PURCHASE_ORDER
601	3221	Initiator, Finance Mgr Ass (Alian),CAO,CFO, CEO	5	4	13941	serviceLPO	Service LPO	FM_PURCHASE_ORDER
601	3341	Dubai Court PRF (LEGAL+CFO+CEO)	4	3	5102	csPrf	csPrf	CS_PAY_REQUEST
601	3341	Dubai Court PRF (LEGAL+CFO+CEO)	4	3	5102	csPrf	csPrf	CS_PAY_REQUEST
601	3342	Dubai Court PRF (LEGAL+CFO)	3	3	5102	csPrf	csPrf	CS_PAY_REQUEST
601	3343	Legal - Direct Finance	3	2	3621	fmPettyCash	Petty Cash	FM_PETTYCASH
601	3343	Legal - Direct Finance	3	2	3621	fmPettyCash	Petty Cash	FM_PETTYCASH
601	3361	Initiator, Project Mgr, DBO, CFO, Secretary, CEO	6	4	13941	serviceLPO	Service LPO	FM_PURCHASE_ORDER
601	3381	Ovington Assets	5	4	21915	assetLPO	assetLPO	IM_PURCHASE_ORDERS
601	3382	Ovington LPOs	5	4	5041	imPurchaseOrder	Purchase Order	IM_PURCHASE_ORDERS