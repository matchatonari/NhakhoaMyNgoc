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

        private void layDuLieuTuSoCCCD()
        {
            // lấy thông tin từ txtSoCCCD
            DataRow searchResult = App.KHACH_HANG.Rows.Find(txtSoCCCD.Text);
            if (searchResult != null)
            {
                cboHoVaTen.Text = searchResult["HoVaTen"].ToString();
                cbGioiTinh.Checked = (bool)searchResult["GioiTinh"];
                txtDiaChi.Text = searchResult["DiaChi"].ToString();
                txtSoDienThoai.Text = searchResult["SoDienThoai"].ToString();
            }
            else
            {
                cboHoVaTen.Text = txtDiaChi.Text = txtSoDienThoai.Text = string.Empty;
                cbGioiTinh.Checked = false;
            }
        }

        private void btnThemDonHang_Click(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            kHACHHANGBindingSource.EndEdit();
            dONHANGBindingSource.EndEdit();
            App.KHACH_HANG.AcceptChanges();
            App.DON_HANG.AcceptChanges();
            App.KHACH_HANG.WriteXml(string.Format("{0}//db_KHACHHANG.xml", Application.StartupPath));
            App.DON_HANG.WriteXml(string.Format("{0}//db_DONHANG.xml", Application.StartupPath));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // load dữ liệu khách hàng, đơn hàng
            string db_KhachHang = string.Format("{0}//db_KHACHHANG.xml", Application.StartupPath);
            string db_DonHang = string.Format("{0}//db_DONHANG.xml", Application.StartupPath);
            if (File.Exists(db_KhachHang))
                App.KHACH_HANG.ReadXml(db_KhachHang);
            if (File.Exists(db_DonHang))
                App.DON_HANG.ReadXml(db_DonHang);
            kHACHHANGBindingSource.DataSource = App.KHACH_HANG;
            dONHANGBindingSource.DataSource = App.DON_HANG;

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

            // load chi tiết thông tin tại hàng đang chọn
            layDuLieuTuSoCCCD();
        }

        private void dgvDonHang_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                layDuLieuTuSoCCCD();
        }
    }
}
