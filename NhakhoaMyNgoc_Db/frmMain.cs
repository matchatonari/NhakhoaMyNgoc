using System;
using System.Data;
using System.Windows.Forms;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.CodeDom;
using System.Runtime.Remoting.Channels;

namespace NhakhoaMyNgoc_Db
{
    public partial class frm_Main : Form
    {
        public frm_Main()
        {
            InitializeComponent();
        }

        #region UTIL
        DataTable QueryCustomer()
        {
            return Database.Query("Customer", null, new Dictionary<string, (QueryOperator, object)>
            {
                { "Customer_FullName", (QueryOperator.LIKE, txtCustomer_FullName.Text) },
                { "Customer_CitizenId", (QueryOperator.EQUALS, txtCustomer_CitizenId.Text) },
                { "Customer_Address", (QueryOperator.LIKE, txtCustomer_Address.Text) },
                { "Customer_Phone", (QueryOperator.EQUALS, txtCustomer_Phone.Text) }
            });
        }
        void UpdateStockList()
        {
            DataTable stockList = Database.Query("StockList");

            cboStockReceipt_StockId.DataSource = new DataView(stockList);
            cboStockReceipt_StockId.ValueMember
                = StockReceipt_StockId.ValueMember
                = "StockList_Id";
            cboStockReceipt_StockId.DisplayMember
                = StockReceipt_StockId.DisplayMember
                = "StockList_Alias";
            ((DataView)cboStockReceipt_StockId.DataSource).RowFilter = "StockList_IsActive = 1";

            StockReceipt_StockId.DataSource = stockList;
        }

        void LoadStockReceipts(object sender, EventArgs e)
        {
            DateTime from = dtpk_Receipt_FromDate.Value.Date;
            DateTime to = dtpk_Receipt_ToDate.Value.AddDays(1).AddSeconds(-1).Date;
            DataTable result = Database.Query("StockReceipt", conditions: new Dictionary<string, (QueryOperator, object)>
                {
                    { "StockReceipt_Date", (QueryOperator.BETWEEN, (from, to)) }
                });
            DataTable renderResult = result.Clone();
            renderResult.Columns["StockReceipt_IsInput"].DataType = typeof(bool);
            result.AsEnumerable().ToList().ForEach(row =>
            {
                var newRow = renderResult.NewRow();
                newRow.ItemArray = row.ItemArray;
                newRow["StockReceipt_IsInput"] = Convert.ToInt32(row["StockReceipt_IsInput"]) == 1; // Chuyển 0/1 thành bool
                renderResult.Rows.Add(newRow);
            });
            dgv_StockReceipt.DataSource = renderResult;

            bsStock.DataSource = Database.Query("Stock");
            dgv_StockReceipt_SelectionChanged(sender, e);
        }
        #endregion

        #region DON_NHAP
        /// <summary>
        /// Thêm đơn nhập
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_AddStockReceipt_Click(object sender, EventArgs e)
        {
            var receipt = Util.GenerateObject<StockReceipt>(
                null,
                dtpkStockReceipt_Date,
                rb_Input,
                txtStockReceipt_Correspondent,
                txtStockReceipt_Division,
                txtStockReceipt_Reason,
                cboStockReceipt_StockId,
                txtStockReceipt_CertificateId,
                null
            );

            int receiptId = Database.AddRecord("StockReceipt", receipt);

            foreach (DataGridViewRow row in dgv_StockReceipt_Content.Rows)
            {
                if (row.IsNewRow) continue;

                var c = row.Cells;

                var detail = Util.MapRowTo<StockReceiptDetail>(row);
                string stockName = c["StockReceiptDetail_ItemName"].Value.ToString();
                int realQuantity = Convert.ToInt32(c["StockReceiptDetail_Quantity"].Value) * (rb_Output.Checked ? -1 : 1);

                // Lấy hoặc thêm Item vào DB
                var item = new Item {
                    Stock_Name = stockName,
                    Stock_Unit = detail.StockReceiptDetail_Unit
                };
                DataTable search = Database.Query("Stock", new List<string> { stockName },
                                  new Dictionary<string, (QueryOperator, object)> { { "Stock_Name", (QueryOperator.COLLATE, stockName) } });
                item.Stock_Id = search.Rows.Count > 0 ? Convert.ToInt32(search.Rows[0]["Stock_Id"]) : Database.AddRecord("Stock", item);

                // Tạo detail & tính tổng
                detail.StockReceiptDetail_ReceiptId = receiptId;
                detail.StockReceiptDetail_ItemId = item.Stock_Id;
                detail.StockReceiptDetail_Quantity = realQuantity;

                receipt.StockReceipt_Total += realQuantity * detail.StockReceiptDetail_Price;

                Database.AddRecord("StockReceiptDetail", detail);
            }

            Database.UpdateRecord("StockReceipt", receiptId, new Dictionary<string, object> { { "StockReceipt_Total", receipt.StockReceipt_Total } });
            dgv_StockReceipt_Content.Rows.Clear();
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

            Customer customer = Util.GenerateObject<Customer>(
                txtCustomer_FullName,
                rdCustomer_Male,
                dtpkCustomer_Birthdate,
                txtCustomer_CitizenId,
                txtCustomer_Address,
                txtCustomer_Phone
            );
            DataTable customerTable = QueryCustomer();
            int customerId = 0;

            if (customerTable.Rows.Count == 0)
                customerId = Database.AddRecord("Customer", customer);
            else
                customerId = Convert.ToInt32(customerTable.Rows[0]["Customer_Id"]);

            // tạo hoá đơn mới
            Receipt receipt = new Receipt
            {
                Receipt_CustomerId = customerId,
                Receipt_Date = dtpkReceipt_Date.Value,
                Receipt_RevisitDate = dtpkReceipt_RevisitDate.Value.Date,
                Receipt_Notes = txtReceipt_Notes.Text
            };
            int receiptId = Database.AddRecord("Receipt", receipt);

            foreach (DataGridViewRow row in dgv_Receipt_Content.Rows)
            {
                if (!row.IsNewRow)
                {
                    ReceiptDetail service = Util.MapRowTo<ReceiptDetail>(row);
                    service.ReceiptDetail_ReceiptId = receiptId;

                    Database.AddRecord("ReceiptDetail", service);

                    receipt.Receipt_Total += service.ReceiptDetail_Price * service.ReceiptDetail_Quantity - service.ReceiptDetail_Discount;
                }
            }
            receipt.Receipt_Remaining = receipt.Receipt_Total;

            Database.UpdateRecord("Receipt", receiptId, new Dictionary<string, object> {
                { "Receipt_Total", receipt.Receipt_Total },
                { "Receipt_Remaining", receipt.Receipt_Remaining }
            });

            dgv_Receipt.DataSource = Database.GetReceipts(customer);

            // dọn dẹp
            Util.ClearForm(
                txtReceipt_Notes,
                dgv_Receipt_Content,
                dtpkReceipt_Date,
                txtCustomer_CitizenId
            );
            dgv_Receipt_SelectionChanged(sender, e);
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
                Util.ClearForm(
                    txtReceipt_Notes,
                    dgv_Receipt_Content,
                    dtpkReceipt_Date
                );
                return;
            }

            Receipt receipt = Util.MapRowTo<Receipt>(dgv_Receipt.CurrentRow);
            DataTable details = Database.Query("ReceiptDetail", null, new Dictionary<string, (QueryOperator, object)>
            {
                { "ReceiptDetail_ReceiptId", (QueryOperator.EQUALS, receipt.Receipt_Id) },
            });

            dtpkReceipt_Date.Value = receipt.Receipt_Date;
            dtpkReceipt_RevisitDate.Value = receipt.Receipt_RevisitDate;
            txtReceipt_Notes.Text = receipt.Receipt_Notes;
            dgv_Receipt_Content.DataSource = details;
            btnSaveReceipt.Enabled = true;

            // cập nhật checkbox tái khám
            cbRevisitDate.Checked = (DateTime.Now.Year + 1 >= receipt.Receipt_RevisitDate.Year);
            cbRevisitDate_CheckedChanged(sender, e);
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
            var receiptId = Convert.ToInt32(currentRow.Cells["Receipt_Id"].Value);
            // lấy giá trị mới
            var newReceipt_Date = dtpkReceipt_Date.Value.ToString("yyyy-MM-dd HH:mm:ss");
            var newReceipt_RevisitDate = dtpkReceipt_RevisitDate.Value.ToString("yyyy-MM-dd HH:mm:ss");

            // update db
            Database.UpdateRecord("Receipt", receiptId,
                new Dictionary<string, object>
            {
                { "Receipt_Date"       , newReceipt_Date        },
                { "Receipt_RevisitDate", newReceipt_RevisitDate },
                { "Receipt_Notes"      , txtReceipt_Notes.Text  }
            });
            foreach (DataGridViewRow row in dgv_Receipt_Content.Rows)
            {
                var primaryValue = row.Cells["ReceiptDetail_Id"].Value;
                if (primaryValue == DBNull.Value)
                {
                    // Nội dung vừa được thêm vào
                    ReceiptDetail newContent = Util.MapRowTo<ReceiptDetail>(row);
                    newContent.ReceiptDetail_ReceiptId = receiptId;
                    Database.AddRecord<ReceiptDetail>("ReceiptDetail", newContent);
                } else
                {
                    var edits = new Dictionary<string, object>();
                    foreach (DataGridViewColumn col in dgv_Receipt_Content.Columns)
                        edits.Add(col.Name, row.Cells[col.Name].Value);
                    Database.UpdateRecord("ReceiptDetail", Convert.ToInt32(primaryValue), edits);
                }
            }

            // update ui
            Customer customer = Util.MapRowTo<Customer>(dgv_Customer.CurrentRow);
            dgv_Receipt.DataSource = Database.GetReceipts(customer);
            dgv_Receipt_SelectionChanged(sender, e);
        }

        private void btnPrintReceipt_Click(object sender, EventArgs e)
        {
            Customer customer = Util.MapRowTo<Customer>(dgv_Customer.CurrentRow);
            Receipt receipt = Util.MapRowTo<Receipt>(dgv_Receipt.CurrentRow);
            DataTable receiptDetails = (DataTable)(dgv_Receipt_Content.DataSource);
            Invoice invoice = new Invoice(customer, receipt, receiptDetails);
            new PrintDialog(invoice).Show();
        }

        private void cbRevisitDate_CheckedChanged(object sender, EventArgs e)
        {
            if (dgv_Receipt.Rows.Count == 0) return;

            DateTime oldDate = Convert.ToDateTime(dgv_Receipt.CurrentRow.Cells["Receipt_RevisitDate"].Value);
            dtpkReceipt_RevisitDate.Enabled = cbRevisitDate.Checked;
            dtpkReceipt_RevisitDate.Value = cbRevisitDate.Checked ? oldDate : DateTimePicker.MaximumDateTime;
        }
        #endregion

        #region KHACH_HANG
        private void btn_DeleteDetails_Click(object sender, EventArgs e)
        {
            txtCustomer_FullName.Text
                = txtCustomer_CitizenId.Text
                = txtCustomer_Address.Text
                = txtCustomer_Phone.Text
                = string.Empty;
            txtCustomer_FullName.Focus();
        }
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
                = dgv_StockReceipt_Content.AutoGenerateColumns
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
            Util.AttachUpdateHook(dgv_StockReceipt, "StockReceipt");

            Util.AttachDeleteHook(dgv_Customer, "Customer");
            Util.AttachDeleteHook(dgv_Receipt, "Receipt", true);
            Util.AttachDeleteHook(dgv_StockReceipt, "StockReceipt", true);
            Util.AttachDeleteHook(dgv_Receipt_Content, "ReceiptDetail");
            Util.AttachDeleteHook(dgv_StockReceipt_Content, "StockReceiptDetail");

            tsi_Restore.Click += (s, ev) => Util.AttachRestoreHook(dgv_Customer, "Customer");

            Util.DismissDirtyState(dgv_Customer);
            Util.DismissDirtyState(dgv_StockReceipt);

            // update
            msiKiemTraCapNhat.Click += (s, ev) => new frmUpdate().Show();
            btnEditStockList.Click += (s, ev) =>
            {
                if (new StockListEditor().ShowDialog() == DialogResult.OK)
                    UpdateStockList();
            };
            btn_SearchStockReceipt.Click += (s, ev) => LoadStockReceipts(s, ev);
        }

        private void tbcMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tbcMain.SelectedIndex == 1)
                UpdateStockList();
        }

        private void dgv_StockReceipt_SelectionChanged(object sender, EventArgs e)
        {
            var r = dgv_StockReceipt.CurrentRow;
            if (r == null)
            {
                Util.ClearForm(
                    dtpkStockReceipt_Date,
                    txtStockReceipt_Correspondent,
                    txtStockReceipt_Division,
                    txtStockReceipt_CertificateId,
                    cboStockReceipt_StockId,
                    dgv_StockReceipt_Content
                );
                return;
            }

            StockReceipt receipt = Util.MapRowTo<StockReceipt>(r);
            DataTable details = Database.Query("StockReceiptDetail", null, new Dictionary<string, (QueryOperator, object)>
            {
                { "StockReceiptDetail_ReceiptId", (QueryOperator.EQUALS, receipt.StockReceipt_Id) },
            });

            details.Columns.Add("StockReceiptDetail_ItemName", typeof(string));

            foreach (DataRow dr in details.Rows)
            {
                var itemId = Convert.ToInt32(dr["StockReceiptDetail_ItemId"]);
                DataTable result = Database.Query("Stock", new List<string>() { "Stock_Name" },
                    new Dictionary<string, (QueryOperator, object)> { { "Stock_Id", (QueryOperator.EQUALS, itemId) } });
                if (result.Rows.Count > 0) {
                    var itemName = result.Rows[0]["Stock_Name"];
                    dr["StockReceiptDetail_ItemName"] = itemName;
                }
            }

            Util.LoadRowToForm(r,
                null,
                dtpkStockReceipt_Date,
                rb_Input,
                txtStockReceipt_Correspondent,
                txtStockReceipt_Division,
                txtStockReceipt_Reason,
                cboStockReceipt_StockId,
                txtStockReceipt_CertificateId,
                null
                );

            dgv_StockReceipt_Content.DataSource = details;
        }

        private void btnSaveStockReceipt_Click(object sender, EventArgs e)
        {
            var currentRow = dgv_StockReceipt.CurrentRow;
            var receiptId = Convert.ToInt32(currentRow.Cells["StockReceipt_Id"].Value);

            // update db
            Database.UpdateRecord("StockReceipt", receiptId,
                new Dictionary<string, object>
            {
                { "StockReceipt_IsInput"      , rb_Input.Checked ? 1 : 0                                    },
                { "StockReceipt_Date"         , dtpkStockReceipt_Date.Value.ToString("yyyy-MM-dd HH:mm:ss") },
                { "StockReceipt_Correspondent", txtStockReceipt_Correspondent.Text                          },
                { "StockReceipt_Division"     , txtStockReceipt_Division.Text                               },
                { "StockReceipt_Reason"       , txtStockReceipt_Reason.Text                                 },
                { "StockReceipt_StockId"      , cboStockReceipt_StockId.SelectedValue                       },
                { "StockReceipt_CertificateId", txtStockReceipt_CertificateId.Text                          }
            });
            foreach (DataGridViewRow row in dgv_StockReceipt_Content.Rows)
            {
                if (row != null && row.IsNewRow == false)
                {
                    var primaryValue = row.Cells["StockReceiptDetail_Id"].Value ?? DBNull.Value;

                    // query kho
                    Item newitem = new Item
                    {
                        Stock_Name = row.Cells["StockReceiptDetail_ItemName"].Value.ToString(),
                        Stock_Unit = row.Cells["StockReceiptDetail_Unit"].Value.ToString(),
                    };
                    newitem.Stock_Total = newitem.Stock_Quantity * Convert.ToInt32(row.Cells["StockReceiptDetail_Price"].Value);
                    DataTable checkResult = Database.Query("Stock",
                        new List<string> { "Stock_Id" },
                        new Dictionary<string, (QueryOperator, object)>
                            { { "Stock_Name", (QueryOperator.COLLATE, newitem.Stock_Name) } });

                    if (primaryValue == DBNull.Value)
                    {
                        // Nội dung vừa được thêm vào
                        StockReceiptDetail newContent = Util.MapRowTo<StockReceiptDetail>(row);
                        newContent.StockReceiptDetail_ReceiptId = receiptId;
                        // Tìm lại StockId cho nó
                        newContent.StockReceiptDetail_ItemId = checkResult.Rows.Count > 0 ?
                            Convert.ToInt32(checkResult.Rows[0]["Stock_Id"]) :
                            Database.AddRecord<Item>("Stock", newitem);

                        Database.AddRecord<StockReceiptDetail>("StockReceiptDetail", newContent);
                    }
                    else
                    {
                        // Sửa
                        var edits = new Dictionary<string, object>();
                        foreach (DataGridViewColumn col in dgv_StockReceipt_Content.Columns)
                        {
                            if (col.Name.Contains("_Id")) continue;

                            if (col.Name != "StockReceiptDetail_ItemName")
                                edits.Add(col.Name, row.Cells[col.Name].Value);
                            else
                            {
                                edits.Add("StockReceiptDetail_ItemId", checkResult.Rows.Count > 0 ?
                                    checkResult.Rows[0]["Stock_Id"] :
                                    Database.AddRecord<Item>("Stock", newitem)
                                    );
                            }
                        }

                        Database.UpdateRecord("StockReceiptDetail", Convert.ToInt32(primaryValue), edits);
                    }
                }
            }

            // update ui
            LoadStockReceipts(sender, e);
        }
    }
}
