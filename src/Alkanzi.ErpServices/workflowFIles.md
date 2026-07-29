# section of Approval log
 SM_APPROVAL_LOGS_HEADER Logheader = await approveLogHeaderRepo.GetFirstAsync(c =>
            c.IS_DELETED == false &&
            c.DOC_NAME.Equals(DocType) &&
            c.TRANSACTION_ID == TransactionId);
if Logheader is null then create new log header and save it to the database.

 var Logheader = new SM_APPROVAL_LOGS_HEADER
                { 
                    DOC_ID = model.DOC_ID,
                    DOC_NAME = model.DOC_TYPE,
                    FORM_ID = model.FORM_ID,
                    TRANSACTION_ID = model.TRANSACTION_ID,
                    IS_APPROVED = false
                };
                ((IAuditable)Logheader).MarkCreated(UserId);
                await approveLogHeaderRepo.AddAsync(Logheader);

then insert a approval log details.

var LevelRow = await workFlowLevelsRepo.GetFirstAsync(c =>
                c.IS_DELETED == false && c.FORM_ID == model.FORM_ID && c.LEVEL_ID == Lvl);
            string LevelName = LevelRow != null ? LevelRow.REMARKS : model.FROM_LEVEL_NAME;




            SM_APPROVAL_LOGS_DETAIL detailLog = new SM_APPROVAL_LOGS_DETAIL
            {
                BRANCH_ID = model.BRANCH_ID,
                COMP_ID = model.COMP_ID,
                ORG_ID = model.ORG_ID,
                HDR_ID = HdrId,
                REMARKS = model.REMARKS,
                FROM_LEVEL = CurrentLevel,
                APPROVE_STATUS = model.TargetStatus,
                FROM_LEVEL_NAME = LevelName
            };
            ((IAuditable)detailLog).MarkCreated(UserId);
            await repository.ADD_APPROVE_DETAIL(detailLog);


            

     public class SM_APPROVAL_LOGS_HEADER:IErpAuditable
    {
		public string DOC_NAME { get; set; }
		public int DOC_ID { get; set; }
		public int FORM_ID { get; set; }
		public int TRANSACTION_ID { get; set; }
		public bool IS_APPROVED { get; set; }
	}

     public class SM_APPROVAL_LOGS_DETAIL:IErpAuditable
    {
		public int HDR_ID { get; set; }
		public string REMARKS { get; set; }
		public int FROM_LEVEL { get; set; }
		public int APPROVE_STATUS { get; set; }
		public string IP { get; set; }
		public string HOST_NAME { get; set; }
		public string? FROM_LEVEL_NAME { get; set; }
		
	}

### end of approval log section



public class SM_WORKFLOW_FORMS:BASE
    {
		 
		public string NAME { get; set; }
		//public string DISPLAY_NAME { get; set; }
		public string TABLE_NAME { get; set; }
		public int DOC_ID { get; set; }
		public string REAMRKS { get; set; }
		public string INFO_QUERY { get; set; }
		public int LAST_LEVEL { get; set; }
		public bool NOTIFICATION { get; set; }
		public Enums.ApprovalType APPROVAL_TYPE { get; set; }
        public string REF_COL_VALUE { get; set; }
        public delegate bool FilterDelegate(SM_WORKFLOW_FORMS model);
        public static IEnumerable<SM_WORKFLOW_FORMS> Filter(IEnumerable<SM_WORKFLOW_FORMS> models, FilterDelegate del)
        {
            List<SM_WORKFLOW_FORMS> tmp = new List<SM_WORKFLOW_FORMS>();
            foreach (var model in models)
            {
                if (del(model))
                {
                    tmp.Add(model);
                }
            }
            return tmp;
        }

    }


    public class SM_WORKFLOW_FORM_LEVELS:BASE
    {
       
        public int LEVEL_ID { get; set; }
        public int ROLE_ID { get; set; }
        public int FORM_ID { get; set; }
        public int SECURITY_GROUP_ID { get; set; }
        public string REMARKS { get; set; }
        public string CC { get; set; }
        public string BCC { get; set; }
        public bool? NO_OVERLAP { get; set; } = false;
        public string NO_OVERLAP_CONDITION { get; set; }
        public string UPDATE_SENTENCE { get; set; }
        public string SUBMIT_CONDITION { get; set; }
        public string SUBMIT_PROCEDURE { get; set; }
    }

    #instead of base you can use IErpAuditable