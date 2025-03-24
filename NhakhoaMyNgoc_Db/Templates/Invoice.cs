using System;
using System.Data;
using System.IO;
using System.Windows.Forms;

namespace NhakhoaMyNgoc_Db
{
    public class Invoice : PrintablePaper
    {
        Customer customer;
        Receipt receipt;
        DataTable receiptDetails;

        public Invoice(Customer customer, Receipt receipt, DataTable receiptDetails)
        {
            this.customer = customer;
            this.receipt = receipt;
            this.receiptDetails = receiptDetails;
        }

        public override string GetTemplateName()
        {
            return "Invoice";
        }

        public override object GetFileName()
        {
            return receipt.Receipt_Id;
        }

        public override void Edit(ref string templateSrc)
        {
            string serviceList = string.Empty;
            foreach (DataRow row in receiptDetails.Rows)
            {
                serviceList += "<tr>";
                foreach (DataColumn column in row.Table.Columns)
                {
                    if (column.ColumnName == "ReceiptDetail_Id") continue;
                    serviceList += "<td>" + row[column].ToString() + "</td>\n";
                }
                serviceList += "</tr>";
            }

            string notes = receipt.Receipt_Notes;
            if (receipt.Receipt_RevisitDate.Year <= DateTime.Now.Year + 1)
                notes += " (Hẹn tái khám ngày " + receipt.Receipt_RevisitDate.ToString("dd/MM/yyyy") + ").";

            var customerProperties = typeof(Customer).GetProperties();
            foreach (var prop in customerProperties)
                if (prop.Name != "Customer_Sex")
                    templateSrc = templateSrc.Replace($"{{{{{prop.Name}}}}}", prop.GetValue(customer).ToString());

            var receiptProperties = typeof(Receipt).GetProperties();
            foreach (var prop in receiptProperties)
                if (prop.Name != "Receipt_Notes")
                    templateSrc = templateSrc.Replace($"{{{{{prop.Name}}}}}", prop.GetValue(receipt).ToString());

            templateSrc = templateSrc
                .Replace("{{!Customer_Sex}}", (customer.Customer_Sex ? "☐" : "☑"))
                .Replace("{{Customer_Sex}}", (customer.Customer_Sex ? "☑" : "☐"))
                .Replace("{{ReceiptDetail}}", serviceList)
                .Replace("{{Receipt_Notes}}", notes);
        }
    }
}
