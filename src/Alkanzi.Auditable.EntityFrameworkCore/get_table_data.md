
### repository For FM_TRANSACTION table, this table already in my dbcontext and mapped to entity class FM_TRANSACTION_MENU. The entity class for the
```csharp table is
public class FM_TRANSACTION_MENU : BASE
    {
        [Key]
        public int ID { get; set; }
        public string DISPLAY_NAME { get; set; }
        public string MAIN_DOC_TYPE { get; set; }
        public string DOC_TYPE { get; set; }
        public int FROM { get; set; }
        public int TO { get; set; }
        public int CURR_NO { get; set; }
        public string TRANSACTION_TYPE { get; set; }
        public string PREFIX { get; set; }
        public int PENDING { get; set; }
        public int SUBMIT { get; set; }
        public int REWORK { get; set; }
        public decimal PENALTY_AMOUNT { get; set; }
        public bool STATUS { get; set; }
        public bool MULTI_WF { get; set; }
        public string TABLE_NAME { get; set; }
    }


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
```
# I need approval.Engine class that has a method that takes the docType returns the  table name row from the table. and another one will get Table & and Trans Id will return the object. method should be generic and not hardcode the entity class. It should use EF's model to resolve the table name to the entity CLR type and then fetch the row by primary key.
// 1. you already have this
var menuRow = await repository.GetAsync(id);
string tableName = menuRow.TABLE_NAME;   // e.g. "FM_SOME_TABLE"
int transId = 10;

// 2. resolve table name -> entity CLR type from EF's model (no hardcoding)
var entityType = _context.Model.GetEntityTypes()
    .FirstOrDefault(e => string.Equals(e.GetTableName(), tableName,
                                       StringComparison.OrdinalIgnoreCase));

if (entityType is null)
    throw new InvalidOperationException($"No entity mapped to table '{tableName}'.");

// 3. fetch by primary key — EF builds the SELECT, you don't
object row = await _context.FindAsync(entityType.ClrType, transId);