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
                DataRow[] history = App.MUC_DON_HANG.Select(string.Format("SoCCCD = '{0}'", txtSoCCCD.Text), "NgayKham ASC");
                mUCDONHANGBindingSource.DataSource = history;
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
            if (txtNoiDungDieuTri.Text == string.Empty || txtSoCCCD.Text == string.Empty)
                MessageBox.Show("Thêm đơn hàng thất bại. Điền đầy đủ thông tin và nhấn nút 'Tìm' để tải đầy đủ dữ liệu trước khi thêm.", "Thêm đơn hàng thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
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
                    newGuest["NgaySinh"] = dtpkNgaySinh.Value.Date;
                    newGuest["SoCCCD"] = txtSoCCCD.Text;
                    newGuest["DiaChi"] = cboDiaChi.Text;
                    newGuest["SoDienThoai"] = txtSoDienThoai.Text;
                    App.KHACH_HANG.Rows.Add(newGuest);
                }
                // thêm nội dung điều trị
                DataRow newItem = App.MUC_DON_HANG.NewRow();
                newItem["NoiDung"] = txtNoiDungDieuTri.Text;
                newItem["SoTien"] = nmSoTien.Value;
                newItem["NgayKham"] = dtpkNgayKham.Value.Date;
                newItem["SoCCCD"] = txtSoCCCD.Text;
                newItem["GiamGia"] = nmGiamGia.Value;
                newItem["ThanhTien"] = nmThanhTien.Value;
                newItem["SoLuong"] = nmSoLuong.Value;
                newItem["MaMucDonHang"] = layHash();
                App.MUC_DON_HANG.Rows.Add(newItem);
                App.MUC_DON_HANG.AcceptChanges();
                dgvDonHang.DataSource = null;
                dgvDonHang.SelectionChanged -= dgvDonHang_SelectionChanged;
                dgvDonHang.DataSource = mUCDONHANGBindingSource;
                layDuLieuTuSoCCCD();
                dgvDonHang.SelectionChanged += dgvDonHang_SelectionChanged;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // lưu dữ liệu và xuất ra file xml
            kHACHHANGBindingSource.EndEdit();
            mUCDONHANGBindingSource.EndEdit();
            dONNHAPBindingSource.EndEdit();
            App.KHACH_HANG.AcceptChanges();
            App.MUC_DON_HANG.AcceptChanges();
            App.DON_NHAP.AcceptChanges();
            App.TON_KHO.AcceptChanges();
            App.KHACH_HANG.WriteXml(string.Format("{0}//xml//KHACHHANG.xml", Application.StartupPath));
            App.MUC_DON_HANG.WriteXml(string.Format("{0}//xml//MUCDONHANG.xml", Application.StartupPath));
            App.DON_NHAP.WriteXml(string.Format("{0}//xml//DONNHAP.xml", Application.StartupPath));
            App.TON_KHO.WriteXml(string.Format("{0}//xml//TONKHO.xml", Application.StartupPath));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // load dữ liệu khách hàng, đơn hàng
            string db_KhachHang = string.Format("{0}//xml//KHACHHANG.xml", Application.StartupPath);
            string db_MucDonHang = string.Format("{0}//xml//MUCDONHANG.xml", Application.StartupPath);
            string db_DonNhap = string.Format("{0}//xml//DONNHAP.xml", Application.StartupPath);
            string db_TonKho = string.Format("{0}//xml//TONKHO.xml", Application.StartupPath);
            if (File.Exists(db_KhachHang))
                App.KHACH_HANG.ReadXml(db_KhachHang);
            if (File.Exists(db_MucDonHang))
                App.MUC_DON_HANG.ReadXml(db_MucDonHang);
            if (File.Exists(db_DonNhap))
                App.DON_NHAP.ReadXml(db_DonNhap);
            if (File.Exists(db_TonKho))
                App.TON_KHO.ReadXml(db_TonKho);

            kHACHHANGBindingSource.DataSource = App.KHACH_HANG;
            mUCDONHANGBindingSource.DataSource = App.MUC_DON_HANG;
            dONNHAPBindingSource.DataSource = App.DON_NHAP;

            // đưa dữ liệu khách hàng vào combobox họ và tên
            DataTable distinctNames = App.KHACH_HANG.DefaultView.ToTable(true, "HoVaTen");
            cboHoVaTen.DataSource = distinctNames;
            cboHoVaTen.DisplayMember = "HoVaTen";
            // đưa tên các vật tồn kho vào combobox tên vật liệu nhập kho
            cboTenVatLieuNhapKho.DataSource = App.TON_KHO;
            cboTenVatLieuNhapKho.DisplayMember = "TenVatLieu";
            // set date of ngaynhapden >= ngaynhaptu
            dtpkNgayNhapDen.MinDate = dtpkNgayNhapTu.Value.Date;
        }

        private void btnTimDonHang_Click(object sender, EventArgs e)
        {
            // tránh lỗi khi đang load dữ liệu
            dgvDonHang.SelectionChanged -= dgvDonHang_SelectionChanged;
            // tìm theo số cccd
            if (txtSoCCCD.Text != string.Empty)
                layDuLieuTuSoCCCD();
            else
            {
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
            dgvDonHang.SelectionChanged += dgvDonHang_SelectionChanged;
            // vô hiệu hoá nút xoá nếu không có đơn hàng nào
            if (dgvDonHang.Rows.Count == 0)
            {
                btnXoaDonHang.Enabled = false;
            }
            else
            {
                btnXoaDonHang.Enabled = true;
            }
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
                // tránh lỗi RowNotInTableException
                foreach (DataGridViewRow row in dgvDonHang.SelectedRows)
                    selectedRows[i++] = App.MUC_DON_HANG.Rows.Find(row.Cells[0].Value);
                dgvDonHang.DataSource = null;
                for (int j = 0; j < i; j++)
                    selectedRows[j].Delete();
                // rebind
                App.MUC_DON_HANG.AcceptChanges();
                dgvDonHang.DataSource = mUCDONHANGBindingSource;
                layDuLieuTuSoCCCD();
                dgvDonHang.SelectionChanged += dgvDonHang_SelectionChanged;
            }
        }

        private void btnSuaThongTinKhachHang_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc muốn sửa thông tin của khách hàng không?\nLưu ý: Nếu phải sửa số CCCD, thực hiện sửa số CCCD trước, sau đó nhấn 'Sửa thông tin', sau đó mới sửa những thông tin khác, rồi lại nhấn 'Sửa thông tin' một lần nữa.", "Xác nhận sửa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                DataRow searchResult = App.KHACH_HANG.Rows.Find(txtSoCCCD.Text);
                if (searchResult != null)
                {
                    // nếu cccd chưa bị sửa
                    searchResult["HoVaTen"] = cboHoVaTen.Text;
                    searchResult["GioiTinh"] = cbGioiTinh.Checked;
                    searchResult["NgaySinh"] = dtpkNgaySinh.Value.Date;
                    searchResult["DiaChi"] = cboDiaChi.Text;
                    searchResult["SoDienThoai"] = txtSoDienThoai.Text;
                }
                else
                {
                    // nếu cccd đã bị sửa
                    if (cboHoVaTen.Text == string.Empty ||
                        cboDiaChi.Text == string.Empty ||
                        txtSoDienThoai.Text == string.Empty)
                        MessageBox.Show("Kiểm tra đầy đủ thông tin trước khi sửa.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                    {
                        DataRow[] anotherSearchResult = App.KHACH_HANG.Select(string.Format("HoVaTen = '{0}' AND GioiTinh = '{1}' AND NgaySinh = '{2}' AND DiaChi = '{3}' AND SoDienThoai = '{4}'", cboHoVaTen.Text, cbGioiTinh.Checked, dtpkNgaySinh.Value.Date, cboDiaChi.Text, txtSoDienThoai.Text));
                        if (anotherSearchResult.Length > 0)
                            anotherSearchResult[0]["SoCCCD"] = txtSoCCCD.Text;
                        else
                            MessageBox.Show("Sửa thông tin thất bại. Có thể bạn đã làm sai quy trình sửa, hoặc khách hàng này chưa có trong cơ sở dữ liệu của bạn.", "Không thể sửa thông tin", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                App.KHACH_HANG.AcceptChanges();
            }
        }

        private void dgvDonHang_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvDonHang.Rows.Count > 0)
            {
                // lấy đơn hàng dựa theo mã
                DataRow searchResult = App.MUC_DON_HANG.Rows.Find(dgvDonHang.Rows[e.RowIndex].Cells[0].Value);
                searchResult[dgvDonHang.Columns[e.ColumnIndex].Name] = dgvDonHang.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                searchResult["ThanhTien"] = Convert.ToInt32(dgvDonHang.Rows[e.RowIndex].Cells[4].Value) * 
                    Convert.ToInt32(dgvDonHang.Rows[e.RowIndex].Cells[5].Value) - 
                    Convert.ToInt32(dgvDonHang.Rows[e.RowIndex].Cells[7].Value);
                // rebind
                App.MUC_DON_HANG.AcceptChanges();
                dgvDonHang.DataSource = null;
                dgvDonHang.SelectionChanged -= dgvDonHang_SelectionChanged;
                dgvDonHang.DataSource = mUCDONHANGBindingSource;
                layDuLieuTuSoCCCD();
                dgvDonHang.SelectionChanged += dgvDonHang_SelectionChanged;
            }
        }

        private void cbNgayNhapDen_CheckedChanged(object sender, EventArgs e)
        {
            dtpkNgayNhapDen.Enabled = cbNgayNhapDen.Checked;
        }

        private void nmSoLuongVatLieu_ValueChanged(object sender, EventArgs e)
        {
            nmThanhTienVatLieu.Value = nmSoLuongVatLieu.Value * nmDonGiaVatLieu.Value;
        }

        private void nmDonGiaVatLieu_ValueChanged(object sender, EventArgs e)
        {
            nmThanhTienVatLieu.Value = nmSoLuongVatLieu.Value * nmDonGiaVatLieu.Value;
        }

        private void btnThemDonNhap_Click(object sender, EventArgs e)
        {
            if (cboTenVatLieuNhapKho.Text == string.Empty)
                MessageBox.Show("Nhập dữ liệu thất bại. Điền đầy đủ thông tin trước khi thêm.", "Nhập dữ liệu thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
            {
                string tenVatLieu = cboTenVatLieuNhapKho.Text;
                // tính lại giá thành
                nmThanhTienVatLieu.Value = nmDonGiaVatLieu.Value * nmSoLuongVatLieu.Value;
                // kiểm tra vật liệu có trong db chưa? chưa thì thêm vào.
                DataRow searchResult = App.TON_KHO.Rows.Find(cboTenVatLieuNhapKho.Text);
                if (searchResult == null)
                {
                    DataRow newUniqueItem = App.TON_KHO.NewRow();
                    newUniqueItem["TenVatLieu"] = tenVatLieu;
                    newUniqueItem["SoLuong"] = nmSoLuongVatLieu.Value;
                    newUniqueItem["ThanhTien"] = nmThanhTienVatLieu.Value;
                    App.TON_KHO.Rows.Add(newUniqueItem);
                }
                // thêm đơn nhập/xuất
                DataRow newItem = App.DON_NHAP.NewRow();
                newItem["TenVatLieu"] = tenVatLieu;
                newItem["DonGia"] = nmDonGiaVatLieu.Value;
                newItem["NgayNhap"] = dtpkNgayNhapTu.Value.Date;
                newItem["ThanhTien"] = nmThanhTienVatLieu.Value;
                newItem["SoLuong"] = nmSoLuongVatLieu.Value;
                newItem["MaDonNhap"] = layHash();
                App.DON_NHAP.Rows.Add(newItem);
                App.DON_NHAP.AcceptChanges();
                dgvDonNhap.DataSource = null;
                //dgvDonNhap.SelectionChanged -= dgvDonHang_SelectionChanged;
                dgvDonNhap.DataSource = dONNHAPBindingSource;
                //dgvDonHang.SelectionChanged += dgvDonHang_SelectionChanged;

                // cập nhật số hàng trong kho
                DataRow currentGood = App.TON_KHO.Rows.Find(tenVatLieu);
                int currentNumber = Convert.ToInt32(currentGood["SoLuong"]) + (int)nmSoLuongVatLieu.Value;
                currentGood["SoLuong"] = currentNumber;
                // công thức mới: không phụ thuộc vào đơn giá được lưu trong CSDL
                int newCost = Convert.ToInt32(currentGood["ThanhTien"]) + (int)nmSoLuongVatLieu.Value * (int)nmDonGiaVatLieu.Value;
                currentGood["ThanhTien"] = newCost;

                cboTenVatLieuNhapKho.Text = tenVatLieu;
            }
        }

        private void btnXoaDonNhap_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc muốn xoá các mục này không?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                //dgvDonHang.SelectionChanged -= dgvDonNhap_SelectionChanged;
                DataRow[] selectedRows = new DataRow[dgvDonNhap.SelectedRows.Count];
                int i = 0;
                // tránh lỗi RowNotInTableException
                foreach (DataGridViewRow row in dgvDonNhap.SelectedRows)
                {
                    selectedRows[i] = App.DON_NHAP.Rows.Find(row.Cells[5].Value);

                    // cập nhật số hàng còn trong kho
                    DataRow currentItem = App.TON_KHO.Rows.Find(row.Cells[1].Value.ToString().Trim());
                    int currentNumber = Convert.ToInt32(currentItem["SoLuong"]) - Convert.ToInt32(selectedRows[i]["SoLuong"]);
                    currentItem["SoLuong"] = currentNumber;
                    int newCost = Convert.ToInt32(currentItem["ThanhTien"]) - Convert.ToInt32(row.Cells[2].Value) * Convert.ToInt32(row.Cells[3].Value);
                    currentItem["ThanhTien"] = newCost;

                    i++;
                }
                dgvDonNhap.DataSource = null;
                for (int j = 0; j < i; j++)
                    selectedRows[j].Delete();
                // rebind
                App.DON_NHAP.AcceptChanges();
                dgvDonNhap.DataSource = dONNHAPBindingSource;
               // dgvDonNhap.SelectionChanged += dgvDonNhap_SelectionChanged;
            }
        }

        private void btnTimDonNhap_Click(object sender, EventArgs e)
        {
            if (cbNgayNhapDen.Checked)
            {
                DataRow[] searchResult = App.DON_NHAP.Select(string.Format("NgayNhap >= #{0}# AND NgayNhap <= #{1}#", dtpkNgayNhapTu.Value.Date, dtpkNgayNhapDen.Value.Date));
                dONNHAPBindingSource.DataSource = searchResult;
            }
            else
            {
                DataRow[] searchResult = App.DON_NHAP.Select(string.Format("NgayNhap = #{0}#", dtpkNgayNhapTu.Value.Date));
                dONNHAPBindingSource.DataSource = searchResult;
            }
        }

        private void dtpkNgayNhapTu_ValueChanged(object sender, EventArgs e)
        {
            dtpkNgayNhapDen.MinDate = dtpkNgayNhapTu.Value.Date;
        }

        private void dgvDonNhap_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvDonNhap.Rows.Count > 0)
            {
                // lấy đơn hàng dựa theo mã
                DataRow searchResult = App.DON_NHAP.Rows.Find(dgvDonNhap.Rows[e.RowIndex].Cells[5].Value);
                // set lại trong database
                searchResult[dgvDonNhap.Columns[e.ColumnIndex].Name.Replace("VatLieu", "")] = dgvDonNhap.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                int newCost = Convert.ToInt32(dgvDonNhap.Rows[e.RowIndex].Cells[2].Value) *
                    Convert.ToInt32(dgvDonNhap.Rows[e.RowIndex].Cells[3].Value);
                int change = Convert.ToInt32(searchResult["ThanhTien"]) - newCost;
                searchResult["ThanhTien"] = newCost;
                // tính lại tồn kho
                DataRow obj = App.TON_KHO.Rows.Find(dgvDonNhap.Rows[e.RowIndex].Cells[1].Value.ToString().Trim());
                int currentCost = Convert.ToInt32(obj["ThanhTien"]) - change;
                obj["ThanhTien"] = currentCost;
                // rebind
                App.DON_NHAP.AcceptChanges();
                dgvDonNhap.DataSource = null;
                //dgvDonHang.SelectionChanged -= dgvDonHang_SelectionChanged;
                dgvDonNhap.DataSource = dONNHAPBindingSource;
                //dgvDonHang.SelectionChanged += dgvDonHang_SelectionChanged;
            }
        }
    }
}
