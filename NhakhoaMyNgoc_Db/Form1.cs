using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;

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
                dgvDonHang.DataSource = mUCDONHANGBindingSource;
            }
            else
            {
                cboHoVaTen.Text = cboDiaChi.Text = txtSoCCCD.Text = string.Empty;
                dtpkNgaySinh.Value = DateTime.Now;
                cbGioiTinh.Checked = false;
            }
        }

        private string layHash()
        {
            // Generate a random byte array
            byte[] randomBytes = new byte[32]; // 256 bits
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            // Compute the hash
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(randomBytes);
                string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return hash;
            }
        }

        private void btnThemDonHang_Click(object sender, EventArgs e)
        {
            // tính lại giá thành
            nmThanhTien.Value = nmSoTien.Value * nmSoLuong.Value - nmGiamGia.Value;
            // kiểm tra khách hàng có trong db chưa? chưa thì thêm vào.
            DataRow searchResult = App.KHACH_HANG.Rows.Find(txtSoCCCD.Text);
            if (searchResult == null)
            {
                DataRow newGuest = App.KHACH_HANG.NewRow();
                newGuest["HoVaTen"] = cboHoVaTen.Text;
                newGuest["GioiTinh"] = cbGioiTinh.Checked;
                newGuest["NgaySinh"] = dtpkNgaySinh.Value;
                newGuest["SoCCCD"] = txtSoCCCD.Text;
                newGuest["DiaChi"] = cboDiaChi.Text;
                newGuest["SoDienThoai"] = txtSoDienThoai.Text;
                App.KHACH_HANG.Rows.Add(newGuest);
            }
            // thêm nội dung điều trị
            DataRow newItem = App.MUC_DON_HANG.NewRow();
            newItem["NoiDung"] = txtNoiDungDieuTri.Text;
            newItem["SoTien"] = nmSoTien.Value;
            newItem["NgayKham"] = dtpkNgayKham.Value;
            newItem["SoCCCD"] = txtSoCCCD.Text;
            newItem["GiamGia"] = nmGiamGia.Value;
            newItem["ThanhTien"] = nmThanhTien.Value;
            newItem["SoLuong"] = nmSoLuong.Value;
            newItem["MaMucDonHang"] = layHash();
            App.MUC_DON_HANG.Rows.Add(newItem);
            dgvDonHang.SelectionChanged -= dgvDonHang_SelectionChanged;
            dgvDonHang.DataSource = null;
            dgvDonHang.DataSource = mUCDONHANGBindingSource;
            layDuLieuTuSoCCCD();
            dgvDonHang.SelectionChanged += dgvDonHang_SelectionChanged;
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
            // tránh lỗi khi đang load dữ liệu
            dgvDonHang.SelectionChanged -= dgvDonHang_SelectionChanged;
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
            dgvDonHang.SelectionChanged += dgvDonHang_SelectionChanged;
        }

        private void cboHoVaTen_TextChanged(object sender, EventArgs e)
        {
            cboDiaChi.Text = txtSoCCCD.Text = txtSoDienThoai.Text = string.Empty;
        }

        private void dgvDonHang_SelectionChanged(object sender, EventArgs e)
        {
            int sum = 0;
            foreach (DataGridViewRow row in dgvDonHang.SelectedRows)
                sum += Convert.ToInt32(row.Cells[dgvDonHang.Columns.Count - 1].Value);
            lblThanhTien.Text = string.Format("Thành tiền: {0:#,###0}đ", sum);
        }

        private void nmSoTien_ValueChanged(object sender, EventArgs e)
        {
            nmThanhTien.Value = nmSoTien.Value * nmSoLuong.Value - nmGiamGia.Value;
        }

        private void nmSoLuong_ValueChanged(object sender, EventArgs e)
        {
            nmThanhTien.Value = nmSoTien.Value * nmSoLuong.Value - nmGiamGia.Value;
        }

        private void nmGiamGia_ValueChanged(object sender, EventArgs e)
        {
            nmThanhTien.Value = nmSoTien.Value * nmSoLuong.Value - nmGiamGia.Value;
        }

        private void btnXoaDonHang_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc muốn xoá các mục này không?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                dgvDonHang.SelectionChanged -= dgvDonHang_SelectionChanged;
                DataRow[] selectedRows = new DataRow[dgvDonHang.SelectedRows.Count];
                int i = 0;
                foreach (DataGridViewRow row in dgvDonHang.SelectedRows)
                    selectedRows[i++] = App.MUC_DON_HANG.Rows.Find(row.Cells[0].Value);
                dgvDonHang.DataSource = null;
                for (int j = 0; j < i; j++)
                    selectedRows[j].Delete();
                App.MUC_DON_HANG.AcceptChanges();
                dgvDonHang.DataSource = mUCDONHANGBindingSource;
                layDuLieuTuSoCCCD();
                dgvDonHang.SelectionChanged += dgvDonHang_SelectionChanged;
            }
        }
    }
}
