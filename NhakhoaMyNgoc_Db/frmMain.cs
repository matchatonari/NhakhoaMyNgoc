using System;
using System.Data;
using System.Windows.Forms;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace NhakhoaMyNgoc_Db
{
    public partial class frm_Main : Form
    {
        public frm_Main()
        {
            InitializeComponent();
        }

        #region UTIL
        void ClearReceiptBindings()
        {
            txtReceipt_Notes.Text = string.Empty;
            dgv_Receipt_Content.DataSource = null;
            dtpkReceipt_Date.Value = dtpkReceipt_RevisitDate.Value = DateTime.Now;
            btnSaveReceipt.Enabled = false;
        }
        DataTable QueryCustomer()
        {
            return Database.Query("Customer", null, new Dictionary<string, (bool, object)>
            {
                { "Customer_FullName", (true, txtCustomer_FullName.Text) },
                { "Customer_CitizenId", (false, txtCustomer_CitizenId.Text) },
                { "Customer_Address", (true, txtCustomer_Address.Text) },
                { "Customer_Phone", (false, txtCustomer_Phone.Text) }
            });
        }
        Customer GenerateCustomer()
        {
            return new Customer
            {
                Customer_FullName = txtCustomer_FullName.Text,
                Customer_Sex = rdCustomer_Male.Checked ? false : true,
                Customer_Birthdate = dtpkCustomer_Birthdate.Value,
                Customer_CitizenId = txtCustomer_CitizenId.Text,
                Customer_Address = txtCustomer_Address.Text,
                Customer_Phone = txtCustomer_Phone.Text
            };
        }
        #endregion

        #region DON_NHAP
        /// <summary>
        /// Tìm đơn nhập
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SearchStockReceipt_Click(object sender, EventArgs e)
        {
            bsStock.DataSource = Database.GetStock();
            bsStockReceipts.DataSource = Database.GetInputReceiptsBetween(dtpk_Receipt_FromDate.Value, dtpk_Receipt_ToDate.Value);
        }
        /// <summary>
        /// Thêm đơn nhập
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_AddStockReceipt_Click(object sender, EventArgs e)
        {
            var receipt = new StockReceipt { StockReceipt_Date = dtpkStockReceipt_Date.Value };

            foreach (DataGridViewRow row in dgv_StockReceipt_Content.Rows)
            {
                if (row.IsNewRow) continue;

                var c = row.Cells;

                int realQuantity = Convert.ToInt32(c["_StockReceiptDetail_Quantity"].Value) * (rb_Output.Checked ? -1 : 1);
                int price = Convert.ToInt32(c["_StockReceiptDetail_Price"].Value);
                string stockName = c["_StockReceiptDetail_Name"].Value.ToString();

                // Lấy hoặc thêm Item vào DB
                var item = new Item { Stock_Name = stockName };
                DataTable search = Database.Query("Stock", new List<string> { stockName },
                                  new Dictionary<string, (bool, object)> { { "Stock_Name", (true, stockName) } });
                item.Stock_Id = search.Rows.Count > 0 ? Convert.ToInt32(search.Rows[0]["Stock_Id"]) : Database.AddRecord("Stock", item);


                // Tạo detail & tính tổng
                var detail = new StockReceiptDetail
                {
                    StockReceiptDetail_ReceiptId = receipt.StockReceipt_Id,
                    StockReceiptDetail_ItemId = item.Stock_Id,
                    StockReceiptDetail_Quantity = realQuantity,
                    StockReceiptDetail_Price = price
                };
                receipt.StockReceipt_Total += realQuantity * price;

                Database.AddRecord("StockReceiptDetail", detail);
            }

            Database.AddRecord("StockReceipt", receipt);
            dgv_StockReceipt_Content.Rows.Clear();
        }

        /// <summary>
        /// Xoá đơn nhập
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgv_StockReceipt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && dgv_StockReceipt.SelectedRows.Count > 0)
            {
                if (MessageBox.Show("Bạn có chắc muốn xoá các mục này không?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    foreach (DataGridViewRow row in dgv_StockReceipt.SelectedRows)
                    {
                        // xoá
                        Database.DeleteRecord("StockReceiptDetail", new List<int> {
                            Convert.ToInt32(row.Cells["StockReceiptDetail_Id"].Value)
                        });
                        dgv_StockReceipt.Rows.Remove(row);
                    }
                }
            }
        }

        private void dgv_StockReceipt_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            var columnName = dgv_StockReceipt.Columns[e.ColumnIndex].Name;
            if (columnName.Contains("Total")) return;

            Util.AttachUpdateHook(dgv_StockReceipt, "StockReceiptDetail");
        }

        private void tbcIO_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (tbcIO.SelectedIndex)
            {
                case 0:
                    bsStockReceipts.DataSource = Database.GetInputReceiptsBetween(dtpk_Receipt_FromDate.Value, dtpk_Receipt_ToDate.Value);
                    break;
                case 1:
                    bsStock.DataSource = Database.GetStock();
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region DON_HANG
        /// <summary>
        /// Thêm đơn hàng & khách hàng
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_AddReceipt_Click(object sender, EventArgs e)
        {
            bool anyEmptyTextBox = Controls.OfType<TextBox>().Any(tb => string.IsNullOrWhiteSpace(tb.Text));
            if (anyEmptyTextBox)
            {
                MessageBox.Show("Điền đầy đủ thông tin và nhấn nút Tìm trước khi thêm.",
                    "Thêm đơn hàng thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Customer customer = GenerateCustomer();
            DataTable customerTable = QueryCustomer();
            int customerId = 0;

            if (customerTable.Rows.Count == 0)
            {
                customerId = Database.GetNextId("Customer");
                Database.AddRecord("Customer", customer);
            } else
            {
                customerId = Convert.ToInt32(customerTable.Rows[0]["Customer_Id"]);
            }

            // tạo hoá đơn mới
            Receipt receipt = new Receipt
            {
                Receipt_CustomerId = customerId,
                Receipt_Date = dtpkReceipt_Date.Value,
                Receipt_RevisitDate = dtpkReceipt_RevisitDate.Value.Date,
                Receipt_Notes = txtReceipt_Notes.Text
            };

            foreach (DataGridViewRow row in dgv_Receipt_Content.Rows)
            {
                if (!row.IsNewRow)
                {
                    ReceiptDetail service = Util.MapRowTo<ReceiptDetail>(row);
                    service.ReceiptDetail_ReceiptId = Database.GetNextId("Receipt") + 1;

                    Database.AddRecord("ReceiptDetail", service);

                    receipt.Receipt_Total += service.ReceiptDetail_Price * service.ReceiptDetail_Quantity - service.ReceiptDetail_Discount;
                }
            }
            receipt.Receipt_Remaining = receipt.Receipt_Total;
            Database.AddRecord("Receipt", receipt);

            dgv_Receipt.DataSource = Database.GetReceipts(customer);

            // dọn dẹp
            ClearReceiptBindings();
        }
        /// <summary>
        /// Tìm đơn hàng & khách hàng
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SearchReceipt_Click(object sender, EventArgs e)
        {
            bsCustomer.DataSource = null;
            tbcDonHang_KhachHang.SelectedIndex = 1;

            DataTable customerTable = QueryCustomer();
            bsCustomer.DataSource = customerTable;

            cb_Customer_IsActive_CheckedChanged(sender, e);
        }
        /// <summary>
        /// Xoá đơn hàng
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgv_Receipt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && dgv_Receipt.SelectedRows.Count > 0)
            {
                if (MessageBox.Show("Bạn có chắc muốn xoá các mục này không?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    foreach (DataGridViewRow row in dgv_Receipt.SelectedRows)
                    {
                        var receiptId = Convert.ToInt32(row.Cells["Receipt_Id"].Value);
                        Database.DeleteRecord("ReceiptDetail", new List<int> { receiptId });
                        Database.DeleteRecord("Receipt", new List<int> { receiptId });
                        dgv_Receipt.Rows.Remove(row);
                        // xoá binding
                        ClearReceiptBindings();
                    }
                }
            }
        }
        private void dgv_Receipt_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is TextBox textBox)
                textBox.Multiline = true;
        }
        // bind dữ liệu
        private void dgv_Receipt_SelectionChanged(object sender, EventArgs e)
        {
            if (dgv_Receipt.CurrentRow == null)
            {
                ClearReceiptBindings();
                return;
            }

            Receipt receipt = Util.MapRowTo<Receipt>(dgv_Receipt.CurrentRow);
            DataTable details = Database.Query("ReceiptDetail", null, new Dictionary<string, (bool, object)>
            {
                { "ReceiptDetail_ReceiptId", (false, receipt.Receipt_Id) },
            });

            dtpkReceipt_Date.Value = receipt.Receipt_Date;
            dtpkReceipt_RevisitDate.Value = receipt.Receipt_RevisitDate;
            txtReceipt_Notes.Text = receipt.Receipt_Notes;
            dgv_Receipt_Content.DataSource = details;
            btnSaveReceipt.Enabled = true;
        }
        private void dgv_Receipt_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (dgv_Receipt.Columns[e.ColumnIndex].Name == "Receipt_Date" ||
                dgv_Receipt.Columns[e.ColumnIndex].Name == "Receipt_RevisitDate")
            {
                // Hủy bỏ chế độ chỉnh sửa trực tiếp
                e.Cancel = true;

                // Lấy giá trị ngày hiện tại trong ô
                DateTime currentDate = DateTime.Now;
                if (dgv_Receipt.CurrentCell.Value != null)
                    DateTime.TryParse(dgv_Receipt.CurrentCell.Value.ToString(), out currentDate);

                // Mở form chọn ngày
                using (DateTimePickerDialog dateDialog = new DateTimePickerDialog(currentDate))
                {
                    if (dateDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Cập nhật giá trị ô với ngày đã chọn
                        dgv_Receipt.CurrentCell.Value = dateDialog.SelectedDate.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }
            }
        }
        /// <summary>
        /// Sửa đơn hàng
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSaveReceipt_Click(object sender, EventArgs e)
        {
            var currentRow = dgv_Receipt.CurrentRow;
            // lấy giá trị mới
            var newReceipt_Date = dtpkReceipt_Date.Value.ToString("yyyy-MM-dd HH:mm:ss");
            var newReceipt_RevisitDate = dtpkReceipt_RevisitDate.Value.ToString("yyyy-MM-dd HH:mm:ss");

            // update db
            var result = Database.UpdateRecord("Receipt",
                Convert.ToInt32(currentRow.Cells["Receipt_Id"].Value),
                new Dictionary<string, object>
            {
                { "Receipt_Date"       , newReceipt_Date        },
                { "Receipt_RevisitDate", newReceipt_RevisitDate },
                { "Receipt_Notes"      , txtReceipt_Notes.Text  }
            });

            // update ui
            Customer customer = Util.MapRowTo<Customer>(dgv_Customer.CurrentRow);
            dgv_Receipt.DataSource = Database.GetReceipts(customer);

            // lấy lại id của receiptdetail
            DataTable dt = (DataTable)dgv_Receipt_Content.DataSource;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var row = dgv_Receipt_Content.Rows[i];
                var primaryValue = Convert.ToInt32(dt.Rows[i]["ReceiptDetail_Id"]);

                Database.UpdateRecord("ReceiptDetail", primaryValue, new Dictionary<string, object>
                {
                    { "ReceiptDetail_Content" , row.Cells["ReceiptDetail_Content"].Value  },
                    { "ReceiptDetail_Price"   , row.Cells["ReceiptDetail_Price"].Value    },
                    { "ReceiptDetail_Quantity", row.Cells["ReceiptDetail_Quantity"].Value },
                    { "ReceiptDetail_Discount", row.Cells["ReceiptDetail_Discount"].Value }
                });
            }
        }
        #endregion

        #region KHACH_HANG
        /// <summary>
        /// Tìm đơn hàng theo khách hàng
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgv_Customer_SelectionChanged(object sender, EventArgs e)
        {
            if (dgv_Customer.IsHandleCreated)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    if (dgv_Customer.SelectedRows.Count == 0 || dgv_Customer.Rows.Count == 0) return;

                    // Lấy hàng đầu tiên đang được chọn
                    Customer customer = Util.MapRowTo<Customer>(dgv_Customer.CurrentRow);

                    // Truy cập giá trị các ô trong hàng đã chọn
                    txtCustomer_FullName.Text = customer.Customer_FullName;
                    dtpkCustomer_Birthdate.Value = customer.Customer_Birthdate;
                    (rdCustomer_Female.Checked, rdCustomer_Male.Checked) = customer.Customer_Sex.ToString() == "0" ? (false, true) : (true, false);
                    txtCustomer_CitizenId.Text = customer.Customer_CitizenId;
                    txtCustomer_Address.Text = customer.Customer_Address;
                    txtCustomer_Phone.Text = customer.Customer_Phone;

                    dgv_Receipt.DataSource = Database.GetReceipts(customer);
                    dgv_Receipt_SelectionChanged(sender, e);
                }));
            }
        }
        /// <summary>
        /// Xoá khách hàng
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgv_Customer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && dgv_Customer.SelectedRows.Count > 0)
            {
                if (MessageBox.Show("Bạn có chắc muốn xoá những người này không?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    DataTable dt = (DataTable)((BindingSource)dgv_Customer.DataSource).DataSource;
                    foreach (DataGridViewRow row in dgv_Customer.SelectedRows)
                    {
                        Database.UpdateRecord("Customer",
                            Convert.ToInt32(row.Cells["Customer_Id"].Value),
                            new Dictionary<string, object>
                        {
                            { "Customer_IsActive", 0 }
                        });

                        DataRow[] selectedRow = dt.Select("Customer_Id = " + row.Cells["Customer_Id"].Value);
                        selectedRow[0]["Customer_IsActive"] = 0;
                    }
                }
            }
        }
        /// <summary>
        /// Khôi phục khách hàng
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsi_Restore_Click(object sender, EventArgs e)
        {
            DataTable dt = (DataTable)((BindingSource)dgv_Customer.DataSource).DataSource;
            foreach (DataGridViewRow row in dgv_Customer.SelectedRows)
            {
                Database.UpdateRecord("Customer",
                    Convert.ToInt32(row.Cells["Customer_Id"].Value),
                    new Dictionary<string, object>
                {
                    { "Customer_IsActive", 1 }
                });

                DataRow[] selectedRow = dt.Select("Customer_Id = " + row.Cells["Customer_Id"].Value);
                selectedRow[0]["Customer_IsActive"] = 1;
            }
        }
        private void cb_Customer_IsActive_CheckedChanged(object sender, EventArgs e)
        {
            if (!cb_Customer_IsActive.Checked)
                bsCustomer.Filter = "Customer_IsActive = 1";
            else
                bsCustomer.RemoveFilter();
        }
        #endregion
        private void frm_Main_Load(object sender, EventArgs e)
        {
            // khởi tạo db
            Database.Initialize();

            // set phiên bản
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            this.Text = $"Nha khoa Mỹ Ngọc v{version.Major}.{version.Minor}.{version.Build}";

            // không tự tạo cột theo datatable
            dgv_Customer.AutoGenerateColumns
                = dgv_Stock.AutoGenerateColumns
                = dgv_StockReceipt.AutoGenerateColumns
                = dgv_Receipt_Content.AutoGenerateColumns
                = false;

            // event ngày đến >= ngày từ truy vấn đơn nhập
            dtpk_Receipt_FromDate.ValueChanged += (s, ev) =>
                dtpk_Receipt_ToDate.MinDate = dtpk_Receipt_FromDate.Value;

            // reset format N0 mỗi khi cập nhật giá trị
            Util.AttachReformatHook(dgv_Receipt_Content);
            Util.AttachReformatHook(dgv_Receipt);
            Util.AttachReformatHook(dgv_StockReceipt_Content);
            Util.AttachReformatHook(dgv_StockReceipt);

            // sửa db
            dgv_StockReceipt.CellBeginEdit += (s, ev) =>
            {
                dgv_StockReceipt.CurrentCell.Tag =
                    (dgv_StockReceipt.Columns[ev.ColumnIndex].Name, dgv_StockReceipt.CurrentCell.Value);
            };

            Util.AttachUpdateHook(dgv_Receipt, "Receipt");
            Util.AttachUpdateHook(dgv_Customer, "Customer");

            Util.DismissDirtyState(dgv_Customer);
            Util.DismissDirtyState(dgv_StockReceipt);

            // lấy dữ liệu tồn kho
            StockReceiptDetail_ItemId.DisplayMember = "Stock_Name";
            StockReceiptDetail_ItemId.ValueMember = "Stock_Id";

            // update
            msiKiemTraCapNhat.Click += (s, ev) => new frmUpdate().Show();
        }

        private void btn_DeleteDetails_Click(object sender, EventArgs e)
        {
            txtCustomer_FullName.Text
                = txtCustomer_CitizenId.Text
                = txtCustomer_Address.Text
                = txtCustomer_Phone.Text
                = string.Empty;
            txtCustomer_FullName.Focus();
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            Customer customer = Util.MapRowTo<Customer>(dgv_Customer.CurrentRow);
            Receipt receipt = Util.MapRowTo<Receipt>(dgv_Receipt.CurrentRow);
            DataTable receiptDetails = (DataTable)(dgv_Receipt_Content.DataSource);
            new PrintDialog(customer, receipt, receiptDetails).Show();
        }
    }
}
