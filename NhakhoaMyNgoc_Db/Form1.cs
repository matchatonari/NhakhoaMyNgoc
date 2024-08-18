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
                dtpkNgaySinh.Value = DateTime.Parse(searchResult["NgaySinh"].ToString());
                cboDiaChi.Text = searchResult["DiaChi"].ToString();
                txtSoDienThoai.Text = searchResult["SoDienThoai"].ToString();
                DataRow[] history = App.MUC_DON_HANG.Select(string.Format("SoCCCD = '{0}'", txtSoCCCD.Text));
                mUCDONHANGBindingSource.DataSource = history;
            }
            else
            {
                cboHoVaTen.Text = cboDiaChi.Text = txtSoCCCD.Text = string.Empty;
                dtpkNgaySinh.Value = DateTime.Now;
                cbGioiTinh.Checked = false;
            }
        }

        private void btnThemDonHang_Click(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            kHACHHANGBindingSource.EndEdit();
            mUCDONHANGBindingSource.EndEdit();
            App.KHACH_HANG.AcceptChanges();
            App.MUC_DON_HANG.AcceptChanges();
            App.KHACH_HANG.WriteXml(string.Format("{0}//db_KHACHHANG.xml", Application.StartupPath));
            App.MUC_DON_HANG.WriteXml(string.Format("{0}//db_MUCDONHANG.xml", Application.StartupPath));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // load dữ liệu khách hàng, đơn hàng
            string db_KhachHang = string.Format("{0}//db_KHACHHANG.xml", Application.StartupPath);
            string db_MucDonHang = string.Format("{0}//db_MUCDONHANG.xml", Application.StartupPath);
            if (File.Exists(db_KhachHang))
                App.KHACH_HANG.ReadXml(db_KhachHang);
            if (File.Exists(db_MucDonHang))
                App.MUC_DON_HANG.ReadXml(db_MucDonHang);
            kHACHHANGBindingSource.DataSource = App.KHACH_HANG;

            // đưa dữ liệu khách hàng vào combobox họ và tên
            DataTable distinctNames = App.KHACH_HANG.DefaultView.ToTable(true, "HoVaTen");
            cboHoVaTen.DataSource = distinctNames;
            cboHoVaTen.DisplayMember = "HoVaTen";
        }

        private void btnTimDonHang_Click(object sender, EventArgs e)
        {
            // tìm theo số cccd
            if (txtSoCCCD.Text != string.Empty)
                layDuLieuTuSoCCCD();
            if (cboHoVaTen.Text != string.Empty)
            {
                if (cboDiaChi.Text != string.Empty)
                {
                    DataRow[] peopleFound = App.KHACH_HANG.Select(string.Format("HoVaTen = '{0}' AND DiaChi = '{1}'", cboHoVaTen.Text, cboDiaChi.Text));
                    txtSoCCCD.Text = peopleFound[0]["SoCCCD"].ToString();
                    layDuLieuTuSoCCCD();
                }
                else
                {
                    // tìm theo tên
                    DataRow[] addressesFound = App.KHACH_HANG.Select(string.Format("HoVaTen = '{0}'", cboHoVaTen.Text));
                    cboDiaChi.DataSource = addressesFound;
                    cboDiaChi.DisplayMember = "DiaChi";
                    // nếu chỉ có 1 địa chỉ
                    if (cboDiaChi.Items.Count == 1)
                    {
                        DataRow[] peopleFound = App.KHACH_HANG.Select(string.Format("HoVaTen = '{0}' AND DiaChi = '{1}'", cboHoVaTen.Text, cboDiaChi.Text));
                        txtSoCCCD.Text = peopleFound[0]["SoCCCD"].ToString();
                        layDuLieuTuSoCCCD();
                    }
                }
            }
        }

        private void cboHoVaTen_TextChanged(object sender, EventArgs e)
        {
            cboDiaChi.Text = txtSoCCCD.Text = txtSoDienThoai.Text = string.Empty;
        }
    }
}
