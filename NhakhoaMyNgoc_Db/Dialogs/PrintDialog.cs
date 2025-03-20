using System;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;

namespace NhakhoaMyNgoc_Db
{
    public partial class PrintDialog : Form
    {
        PrintablePaper Document;
        public PrintDialog(PrintablePaper doc)
        {
            InitializeComponent();
            Document = doc;
        }

        private async void PrintDialog_Load(object sender, EventArgs e)
        {
            string receiptPath = Document.GetResultPath();
            Document.Render();

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
            string receiptPath = Document.GetResultPath();
            string pdfPath = receiptPath.Replace(".html", ".pdf");

            var options = webView.CoreWebView2.Environment.CreatePrintSettings();
            options.Orientation = CoreWebView2PrintOrientation.Portrait;
            options.ScaleFactor = 1.0;

            bool success = await webView.CoreWebView2.PrintToPdfAsync(pdfPath, options);
            if (success) {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = pdfPath,
                    Verb = "print",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(psi);
            } else {
                MessageBox.Show("Xuất PDF thất bại.", "Lỗi");
            }
            this.Close();
        }
    }
}
