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

        public override string GetResultPath()
        {
            return Path.Combine(Path.GetTempPath(), receipt.Receipt_Id + ".html");
        }

        public override void Render()
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

            string receiptPath = GetResultPath();

            File.Copy(Path.Combine(PrintablePaper.RESOURCE_PATH, "invoice.html"), receiptPath, true);
            File.Copy(Path.Combine(PrintablePaper.RESOURCE_PATH, "logo.png"), Path.Combine(Path.GetTempPath(), "logo.png"), true);

            string receiptContent = File.ReadAllText(receiptPath);
            var customerProperties = typeof(Customer).GetProperties();
            foreach (var prop in customerProperties)
                if (prop.Name != "Customer_Sex")
                    receiptContent = receiptContent.Replace($"{{{{{prop.Name}}}}}", prop.GetValue(customer).ToString());

            var receiptProperties = typeof(Receipt).GetProperties();
            foreach (var prop in receiptProperties)
                if (prop.Name != "Receipt_Notes")
                    receiptContent = receiptContent.Replace($"{{{{{prop.Name}}}}}", prop.GetValue(receipt).ToString());

            receiptContent = receiptContent
                .Replace("{{!Customer_Sex}}", (customer.Customer_Sex ? "☐" : "☑"))
                .Replace("{{Customer_Sex}}", (customer.Customer_Sex ? "☑" : "☐"))
                .Replace("{{ReceiptDetail}}", serviceList)
                .Replace("{{Receipt_Notes}}", notes);

            File.WriteAllText(receiptPath, receiptContent);
        }
    }
}
