using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Microsoft.SqlServer.Server;
using System.Reflection;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;

namespace NhakhoaMyNgoc_Db
{
    public partial class PrintDialog : Form
    {
        Customer customer;
        Receipt receipt;
        DataTable receiptDetails;
        public PrintDialog(Customer customer, Receipt receipt, DataTable receiptDetails)
        {
            InitializeComponent();
            this.customer = customer;
            this.receipt = receipt;
            this.receiptDetails = receiptDetails;
        }

        private async void PrintDialog_Load(object sender, EventArgs e)
        {
            string serviceList = "";
            foreach (DataRow row in receiptDetails.Rows)
            {
                serviceList += "<tr>";
                foreach (DataColumn column in row.Table.Columns)
                {
                    if (column.ColumnName == "ReceiptDetail_Id")
                        continue;

                    serviceList += "<td>" + row[column].ToString() + "</td>\n";
                }
                serviceList += "</tr>";
            }

            string notes = receipt.Receipt_Notes;
            if (receipt.Receipt_RevisitDate.Year <= DateTime.Now.Year + 1)
                notes += " (Hẹn tái khám ngày " + receipt.Receipt_RevisitDate.ToString("dd/MM/yyyy") + ").";

            string receiptPath = Path.Combine(Path.GetTempPath(), receipt.Receipt_Id + ".html");
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

            webView.CoreWebView2InitializationCompleted += (s, ev) => {
                if (ev.IsSuccess)
                    webView.CoreWebView2.Navigate("file:///" + receiptPath.Replace("\\", "/"));
                else
                    MessageBox.Show(ev.InitializationException.Message, "Lỗi khởi tạo WebView2");
            };
            await webView.EnsureCoreWebView2Async();
        }

        private async void btnPrint_Click(object sender, EventArgs e)
        {
            string receiptPath = Path.Combine(Path.GetTempPath(), receipt.Receipt_Id + ".html");
            string pdfPath = receiptPath.Replace(".html", ".pdf");

            var options = webView.CoreWebView2.Environment.CreatePrintSettings();
            options.Orientation = CoreWebView2PrintOrientation.Portrait;
            options.ScaleFactor = 1.0;

            bool success = await webView.CoreWebView2.PrintToPdfAsync(pdfPath, options);
            if (success)
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = pdfPath,
                    Verb = "print",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(psi);
            } else
            {
                MessageBox.Show("Xuất PDF thất bại.", "Lỗi");
            }
            this.Close();
        }
    }
}
