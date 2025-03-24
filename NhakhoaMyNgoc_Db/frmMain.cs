using NhakhoaMyNgoc_Db.Templates;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

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
            return Database.Query("Customer", conditions: new Dictionary<string, (QueryOperator, object)>
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

            Util.LoadTableToDataGridView(result, dgv_StockReceipt);

            bsStock.DataSource = Database.Query("Stock");
            dgv_StockReceipt_SelectionChanged(sender, e);
        }

        bool IsRowValid(DataGridView dgv)
        {
            return (dgv.Rows.Count > 0 && dgv.CurrentRow != null);
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

                // Lấy hoặc thêm Item vào DB
                var item = new Item {
                    Stock_Name = stockName,
                    Stock_Unit = detail.StockReceiptDetail_Unit
                };
                DataTable search = Database.Query("Stock", new List<string> { "Stock_Id", "Stock_Name" },
                                  conditions: new Dictionary<string, (QueryOperator, object)> { { "Stock_Name", (QueryOperator.COLLATE, stockName) } });
                item.Stock_Id = search.Rows.Count > 0 ? Convert.ToInt32(search.Rows[0]["Stock_Id"]) : Database.AddRecord("Stock", item);

                // Tính tổng
                detail.StockReceiptDetail_ReceiptId = receiptId;
                detail.StockReceiptDetail_ItemId = item.Stock_Id;

                receipt.StockReceipt_Total += detail.StockReceiptDetail_Quantity * detail.StockReceiptDetail_Price;

                Database.AddRecord("StockReceiptDetail", detail);
            }

            Database.UpdateRecord("StockReceipt", receiptId, new Dictionary<string, object> { { "StockReceipt_Total", receipt.StockReceipt_Total } });
            Util.ClearForm(
                    dtpkStockReceipt_Date,
                    txtStockReceipt_Correspondent,
                    txtStockReceipt_Division,
                    txtStockReceipt_Reason,
                    txtStockReceipt_CertificateId,
                    cboStockReceipt_StockId,
                    dgv_StockReceipt_Content
                );
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
                    txtStockReceipt_Reason,
                    txtStockReceipt_CertificateId,
                    cboStockReceipt_StockId,
                    dgv_StockReceipt_Content
                );
                return;
            }

            StockReceipt receipt = Util.MapRowTo<StockReceipt>(r);
            DataTable details = Database.Query("StockReceiptDetail", conditions: new Dictionary<string, (QueryOperator, object)>
            {
                { "StockReceiptDetail_ReceiptId", (QueryOperator.EQUALS, receipt.StockReceipt_Id) },
            });

            details.Columns.Add("StockReceiptDetail_ItemName", typeof(string));

            foreach (DataRow dr in details.Rows)
            {
                // Tìm itemName
                var itemId = Convert.ToInt32(dr["StockReceiptDetail_ItemId"]);
                DataTable result = Database.Query("Stock", new List<string>() { "Stock_Name" },
                    conditions: new Dictionary<string, (QueryOperator, object)> { { "Stock_Id", (QueryOperator.EQUALS, itemId) } });
                if (result.Rows.Count > 0)
                {
                    var itemName = result.Rows[0]["Stock_Name"];
                    dr["StockReceiptDetail_ItemName"] = itemName;
                }
            }

            Util.LoadRowToForm(r,
                null,
                dtpkStockReceipt_Date,
                null,
                txtStockReceipt_Correspondent,
                txtStockReceipt_Division,
                txtStockReceipt_Reason,
                cboStockReceipt_StockId,
                txtStockReceipt_CertificateId,
                null
                );

            rb_Input.Checked = (Convert.ToInt32(r.Cells["StockReceipt_IsInput"].Value) == 1);
            rb_Output.Checked = !rb_Input.Checked;

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
                        conditions: new Dictionary<string, (QueryOperator, object)>
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

                            if (col.Name == "StockReceiptDetail_ItemName")
                                edits.Add("StockReceiptDetail_ItemId", checkResult.Rows.Count > 0 ?
                                    checkResult.Rows[0]["Stock_Id"] :
                                    Database.AddRecord<Item>("Stock", newitem)
                                    );
                            else
                                edits.Add(col.Name, row.Cells[col.Name].Value);
                        }

                        Database.UpdateRecord("StockReceiptDetail", Convert.ToInt32(primaryValue), edits);
                    }
                }
            }

            // update ui
            LoadStockReceipts(sender, e);
        }

        private void btnPrintStockReceipt_Click(object sender, EventArgs e)
        {
            StockReceipt receipt = Util.MapRowTo<StockReceipt>(dgv_StockReceipt.CurrentRow);
            DataTable details = (DataTable)(dgv_StockReceipt_Content.DataSource);
            StockIO invoice = new StockIO(receipt, details);
            new PrintDialog(invoice).Show();
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
                null,
                txtCustomer_FullName,
                rdCustomer_Male,
                dtpkCustomer_Birthdate,
                txtCustomer_CitizenId,
                txtCustomer_Address,
                txtCustomer_Phone
            );
            DataTable customerTable = QueryCustomer();

            if (customerTable.Rows.Count == 0)
                customer.Customer_Id = Database.AddRecord("Customer", customer);
            else
                customer.Customer_Id = Convert.ToInt32(customerTable.Rows[0]["Customer_Id"]);

            // tạo hoá đơn mới
            Receipt receipt = new Receipt
            {
                Receipt_CustomerId = customer.Customer_Id,
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
            if (dgv_Receipt.CurrentRow == null || dgv_Receipt.Rows.Count == 0)
            {
                Util.ClearForm(
                    txtReceipt_Notes,
                    dgv_Receipt_Content,
                    dtpkReceipt_Date
                );
                return;
            }

            Receipt receipt = Util.MapRowTo<Receipt>(dgv_Receipt.CurrentRow);
            DataTable details = Database.Query("ReceiptDetail", conditions: new Dictionary<string, (QueryOperator, object)>
                { { "ReceiptDetail_ReceiptId", (QueryOperator.EQUALS, receipt.Receipt_Id) } });

            dtpkReceipt_Date.Value = receipt.Receipt_Date;
            dtpkReceipt_RevisitDate.Value = receipt.Receipt_RevisitDate;
            txtReceipt_Notes.Text = receipt.Receipt_Notes;
            dgv_Receipt_Content.DataSource = details;

            // cập nhật checkbox tái khám
            cbRevisitDate.Checked = (DateTime.Now.Year + 1 >= receipt.Receipt_RevisitDate.Year);
            cbRevisitDate_CheckedChanged(sender, e);
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
            Util.ClearForm(
                txtCustomer_FullName,
                txtCustomer_CitizenId,
                txtCustomer_Address,
                txtCustomer_Phone
                );
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
                    Util.LoadRowToForm(
                        dgv_Customer.CurrentRow,
                        null,
                        txtCustomer_FullName,
                        null,
                        dtpkCustomer_Birthdate,
                        txtCustomer_CitizenId,
                        txtCustomer_Address,
                        txtCustomer_Phone
                        );

                    (rdCustomer_Female.Checked, rdCustomer_Male.Checked) = 
                        customer.Customer_Sex ? (true, false) : (false, true);

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
            dtpk_Expense_FromDate.ValueChanged += (s, ev) =>
                dtpk_Expense_ToDate.MinDate = dtpk_Expense_FromDate.Value;

            Util.AttachHook<ReceiptDetail>(dgv_Receipt_Content,
                Util.HOOK_REFORMAT | Util.HOOK_DELETE | Util.HOOK_RTIP, "ReceiptDetail", true);
            Util.AttachHook<StockReceiptDetail>(dgv_StockReceipt_Content,
                Util.HOOK_REFORMAT | Util.HOOK_DELETE | Util.HOOK_RTIP, "StockReceiptDetail", true);
            Util.AttachHook<Receipt>(dgv_Receipt,
                Util.HOOK_REFORMAT | Util.HOOK_UPDATE | Util.HOOK_DELETE | Util.HOOK_DTPKDIALOG, "Receipt", true);
            Util.AttachHook<StockReceipt>(dgv_StockReceipt,
                Util.HOOK_REFORMAT | Util.HOOK_UPDATE | Util.HOOK_DELETE | Util.HOOK_DISMISS | Util.HOOK_DTPKDIALOG, "StockReceipt", true);
            Util.AttachHook<Expense>(dgv_Expense,
                Util.HOOK_REFORMAT | Util.HOOK_UPDATE | Util.HOOK_DELETE | Util.HOOK_DTPKDIALOG | Util.HOOK_DISMISS, "Expense", true);
            Util.AttachHook<Customer>(dgv_Customer,
                Util.HOOK_UPDATE | Util.HOOK_DELETE | Util.HOOK_RESTORE | Util.HOOK_DISMISS | Util.HOOK_DTPKDIALOG, "Customer", false, tsi_Restore);


            // sửa db
            dgv_StockReceipt.CellBeginEdit += (s, ev) =>
            {
                dgv_StockReceipt.CurrentCell.Tag =
                    (dgv_StockReceipt.Columns[ev.ColumnIndex].Name, dgv_StockReceipt.CurrentCell.Value);
            };

            // update
            msiKiemTraCapNhat.Click += (s, ev) => new frmUpdate().Show();
            btnEditStockList.Click += (s, ev) =>
            {
                if (new StockListEditor().ShowDialog() == DialogResult.OK)
                    UpdateStockList();
            };
            btn_SearchStockReceipt.Click += (s, ev) => LoadStockReceipts(s, ev);

            btnSearchExpenses_Click(sender, e);

            this.FormClosing += (s, ev) => Database.Close();
        }

        private void tbcMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tbcMain.SelectedIndex == 1)
                UpdateStockList();
        }

        private void tmProtection_Tick(object sender, EventArgs e)
        {
            btnSaveReceipt.Enabled = btnPrintReceipt.Enabled = IsRowValid(dgv_Receipt);
            btnPrintExpense.Enabled = IsRowValid(dgv_Expense);
            btnPrintCustomerHistory.Enabled = IsRowValid(dgv_Customer);
        }

        private void btnSearchExpenses_Click(object sender, EventArgs e)
        {
            DateTime from = dtpk_Expense_FromDate.Value.Date;
            DateTime to = dtpk_Expense_ToDate.Value.AddDays(1).AddSeconds(-1).Date;
            DataTable result = Database.Query("Expense", conditions: new Dictionary<string, (QueryOperator, object)> 
                { { "Expense_Date", (QueryOperator.BETWEEN, (from, to)) } });

            Util.LoadTableToDataGridView(result, dgv_Expense);

            int income = dgv_Expense.Rows.Cast<DataGridViewRow>()
                .Where(dr => Convert.ToBoolean(dr.Cells["Expense_IsInput"].Value))
                .Sum(dr => Convert.ToInt32(dr.Cells["Expense_Amount"].Value));
            int expense = dgv_Expense.Rows.Cast<DataGridViewRow>()
                .Where(dr => !Convert.ToBoolean(dr.Cells["Expense_IsInput"].Value))
                .Sum(dr => Convert.ToInt32(dr.Cells["Expense_Amount"].Value));
            int revenue = income - expense;

            lblIncome.Text = string.Format("{0:N0}₫", income);
            lblExpense.Text = string.Format("{0:N0}₫", expense);
            lblRevenue.Text = string.Format("{0:N0}₫", revenue);
        }

        private void btnPrintExpense_Click(object sender, EventArgs e)
        {
            Expense ex = Util.MapRowTo<Expense>(dgv_Expense.CurrentRow);
            if (ex != null)
            {
                ExpenseReceipt p = new ExpenseReceipt(ex);
                p.Landscape = true;
                new PrintDialog(p).Show();
            }
        }

        private void btnPrintCustomerHistory_Click(object sender, EventArgs e)
        {
            Customer c = Util.MapRowTo<Customer>(dgv_Customer.CurrentRow);
            DataTable dt = Database.GetCustomerHistory(c);
            CustomerHistory ch = new CustomerHistory(c, dt);
            new PrintDialog(ch).Show();
        }

        DataTable GetStockChanges(DateTime fromDate, DateTime toDate)
        {
            DataTable result = new DataTable();
            result.Columns.AddRange(new[]
            {
                new DataColumn("Stock_Id", typeof(int)),
                new DataColumn("Stock_Name", typeof(string)),
                new DataColumn("Stock_Unit", typeof(string)),
                new DataColumn("Quantity_Before", typeof(int)),
                new DataColumn("Input", typeof(int)),
                new DataColumn("Output", typeof(int)),
                new DataColumn("Quantity_After", typeof(int))
            });

            DataTable receipts = new DataTable();

            receipts = Database.Query("StockReceipt", new List<string>
                { "StockReceipt_Id", "StockReceipt_IsInput" }, conditions: new Dictionary<string, (QueryOperator, object)>
                { { "StockReceipt_Date", (QueryOperator.BETWEEN, (fromDate, toDate)) } });

            IDictionary<Item, (int, int)> transactionCount = new Dictionary<Item, (int, int)>();

            foreach (DataRow receipt in receipts.Rows)
            {
                var receiptId = receipt["StockReceipt_Id"];
                DataTable details = Database.Query("StockReceiptDetail", new List<string> { "StockReceiptDetail_ItemId", "StockReceiptDetail_Quantity" },
                    conditions: new Dictionary<string, (QueryOperator, object)>
                    { { "StockReceiptDetail_ReceiptId", (QueryOperator.EQUALS, receiptId) } });
                bool isInput = Convert.ToBoolean(receipt["StockReceipt_IsInput"]);
                foreach (DataRow detail in details.Rows)
                {
                    Item item = new Item();

                    int quantity = Convert.ToInt32(detail["StockReceiptDetail_Quantity"]);
                    int productId = Convert.ToInt32(detail["StockReceiptDetail_ItemId"]);
                    DataTable itemResult = Database.Query("Stock", new List<string> { "Stock_Name", "Stock_Unit" }, conditions: new Dictionary<string, (QueryOperator, object)>
                        { { "Stock_Id", (QueryOperator.EQUALS, productId) } });

                    item.Stock_Id = productId;
                    item.Stock_Name = itemResult.Rows[0]["Stock_Name"].ToString();
                    item.Stock_Unit = itemResult.Rows[0]["Stock_Unit"].ToString();

                    ValueTuple<int, int> oldValue;
                    if (!transactionCount.TryGetValue(item, out oldValue)) oldValue = (0, 0);

                    transactionCount[item] = isInput ? (oldValue.Item1 + quantity, oldValue.Item2) : (oldValue.Item1, oldValue.Item2 + quantity);
                }
            }

            foreach (KeyValuePair<Item, (int, int)> kvp in transactionCount)
            {
                Item k = kvp.Key;
                int input = kvp.Value.Item1;
                int output = kvp.Value.Item2;
                result.Rows.Add(k.Stock_Id, k.Stock_Name, k.Stock_Unit, 0, input, output, input - output);
            }

            return result;
        }

        private void btnPrintStock_Click(object sender, EventArgs e)
        {
            DateTime from = dtpk_Receipt_FromDate.Value.Date;
            DateTime to = dtpk_Receipt_ToDate.Value.AddDays(1).AddSeconds(-1).Date;

            DataTable between = GetStockChanges(from, to);
            DataTable before = GetStockChanges(DateTime.MinValue, from);

            foreach (DataRow item in between.Rows)
            {
                DataRow[] itemResult = before.Select($"Stock_Id = {item["Stock_Id"]}");
                if (itemResult.Length > 0)
                {
                    int got = Convert.ToInt32(item["Quantity_Before"]);
                    int current = Convert.ToInt32(itemResult[0]["Quantity_After"]);
                    int willGet = Convert.ToInt32(item["Quantity_After"]);
                    item["Quantity_Before"] = got + current;
                    item["Quantity_After"] = willGet + got + current;
                }
            }

            var sumConditions = new List<string> { "StockReceipt_Total" };
            DataTable sum = Database.Query("StockReceipt", sumConditions, sumConditions, new Dictionary<string, (QueryOperator, object)>
                { { "StockReceipt_Date", (QueryOperator.LESS_THAN_OR_EQUAL, to) } });

            StockReport sr = new StockReport(from, to, between, Convert.ToInt32(sum.Rows[0]["SUM(StockReceipt_Total)"]));
            new PrintDialog(sr).Show();
        }
    }
}
