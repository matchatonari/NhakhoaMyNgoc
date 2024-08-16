using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace NhakhoaMyNgoc_Db
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        static AppData db;
        protected static AppData App
        {
            get
            {
                if (db == null)
                    db = new AppData();
                return db;
            }
        }

        private void btnThemDonHang_Click(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            kHACHHANGBindingSource.EndEdit();
            App.KHACH_HANG.AcceptChanges();
            App.KHACH_HANG.WriteXml(string.Format("{0}//db_KHACHHANG.dat", Application.StartupPath));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // load dữ liệu khách hàng
            string fileName = string.Format("{0}//db_KHACHHANG.dat", Application.StartupPath);
            if (File.Exists(fileName))
                App.KHACH_HANG.ReadXml(fileName);
            kHACHHANGBindingSource.DataSource = App.KHACH_HANG;
            // đưa dữ liệu khách hàng vào combobox họ và tên
            DataTable distinctNames = App.KHACH_HANG.DefaultView.ToTable(true, "HoVaTen");
            cboHoVaTen.DataSource = distinctNames;
            cboHoVaTen.DisplayMember = "HoVaTen";
            // đưa dữ liệu địa chỉ vào textbox
            AutoCompleteStringCollection addresses = new AutoCompleteStringCollection();
            DataTable distinctAddresses = App.KHACH_HANG.DefaultView.ToTable(true, "DiaChi");
            foreach (DataRow row in distinctAddresses.Rows)
                addresses.Add(row["DiaChi"].ToString());
            txtDiaChi.AutoCompleteCustomSource = addresses;
        }
    }
}
