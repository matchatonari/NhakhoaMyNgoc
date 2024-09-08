using System;
using System.Net;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Diagnostics;

namespace NhakhoaMyNgoc_Db
{
    public partial class frmCapNhat : Form
    {
        public frmCapNhat()
        {
            InitializeComponent();
        }

        private void frmCapNhat_Load(object sender, EventArgs e)
        {
            int newVersionMajor, newVersionMinor, newVersionBuild;
            WebClient client = new WebClient();
            client.DownloadFile("https://gist.githubusercontent.com/matchatonari/809e7cbabbf91269fca9d9735352db22/raw/cb7c8cd374aad12774cffd09c5861f23897a1d54/checkForUpdates.xml",
                    Application.StartupPath + "\\res\\checkForUpdates.xml");

            string xmlContent = File.ReadAllText(Application.StartupPath + "\\res\\checkForUpdates.xml");
            xmlContent = xmlContent.Replace("&", "&amp;");

            XmlDocument oDom = new XmlDocument();
            oDom.LoadXml(xmlContent);

            string str = oDom.SelectSingleNode("//currentVersion/major").InnerText;
            Int32.TryParse(str, out newVersionMajor);

            str = oDom.SelectSingleNode("//currentVersion/minor").InnerText;
            Int32.TryParse(str, out newVersionMinor);

            str = oDom.SelectSingleNode("//currentVersion/tiny").InnerText;
            Int32.TryParse(str, out newVersionBuild);

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version.Major < newVersionMajor || (version.Major == newVersionMajor && version.Minor < newVersionMinor) ||
                (version.Major == newVersionMajor && version.Minor == newVersionMinor && version.Build < newVersionBuild))
            {
                btnOK.Enabled = false;
                lblTrangThai.Text = string.Format("Đang cập nhật lên v{0}.{1}.{2}...", newVersionMajor, newVersionMinor, newVersionBuild);
                str = oDom.SelectSingleNode("//path").InnerText;
                client.DownloadProgressChanged += client_DownloadProgressChanged;
                client.DownloadFileCompleted += (s, args) =>
                {
                    try
                    {
                        // Close the current application or wait for it to close
                        string oldExePath = Application.ExecutablePath;

                        // Rename the old executable
                        string backupPath = oldExePath + ".bak";
                        if (File.Exists(backupPath))
                        {
                            File.Delete(backupPath);
                        }
                        File.Move(oldExePath, backupPath);

                        // Replace with the new executable
                        File.Move(Application.StartupPath + "\\newVersion.exe", oldExePath);

                        // Restart the application
                        Process.Start(oldExePath);
                        Application.Exit(); // Exit the current application
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Có lỗi xảy ra: " + ex.Message);
                    }
                };
                client.DownloadFileAsync(new Uri(str), Application.StartupPath + "\\newVersion.exe");
            }
            else
            {
                lblTrangThai.Text = "Phiên bản hiện tại là mới nhất.";
            }
        }
        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            if (progressBar.Value == 100)
            {
                lblTrangThai.Text = "Hãy khởi động lại ứng dụng.";
                btnOK.Enabled = true;
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
