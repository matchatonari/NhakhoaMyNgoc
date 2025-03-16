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

            File.Copy(Path.Combine(Application.StartupPath, "res", "invoice.html"), receiptPath, true);
            File.Copy(Path.Combine(Application.StartupPath, "res", "logo.png"), Path.Combine(Path.GetTempPath(), "logo.png"), true);

            string receiptContent = File.ReadAllText(receiptPath);
            receiptContent = receiptContent.Replace("{{Customer_FullName}}", customer.Customer_FullName)
                .Replace("{{!Customer_Sex}}", (customer.Customer_Sex ? "☐" : "☑"))
                .Replace("{{Customer_Sex}}", (customer.Customer_Sex ? "☑" : "☐"))
                .Replace("{{Customer_Address}}", customer.Customer_Address)
                .Replace("{{Customer_Phone}}", customer.Customer_Phone)
                .Replace("{{ReceiptDetail}}", serviceList)
                .Replace("{{Receipt_Total}}", receipt.Receipt_Total.ToString())
                .Replace("{{Receipt_Notes}}", notes);

            File.WriteAllText(receiptPath, receiptContent);
        }
    }
}
