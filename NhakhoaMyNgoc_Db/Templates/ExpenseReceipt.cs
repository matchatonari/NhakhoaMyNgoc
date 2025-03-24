using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NhakhoaMyNgoc_Db.Templates
{
    class ExpenseReceipt : PrintablePaper
    {
        Expense receipt;
        public ExpenseReceipt(Expense expense)
        {
            receipt = expense;
        }
        public override object GetFileName()
        {
            return receipt.Expense_Id;
        }
        public override string GetTemplateName()
        {
            return receipt.Expense_IsInput ? "IncomeReceipt" : "ExpenseReceipt";
        }
        public override void Edit(ref string templateSrc)
        {
            var receiptProperties = typeof(Expense).GetProperties();
            foreach (var prop in receiptProperties)
                templateSrc = templateSrc.Replace($"{{{{{prop.Name}}}}}", prop.GetValue(receipt).ToString());
        }
    }
}
