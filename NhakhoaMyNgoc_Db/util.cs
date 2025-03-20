using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace NhakhoaMyNgoc_Db
{
    public class Customer
    {
        public string Customer_FullName { get; set; }
        public bool Customer_Sex { get; set; }
        public DateTime Customer_Birthdate { get; set; }
        public string Customer_CitizenId { get; set; }
        public string Customer_Address { get; set; }
        public string Customer_Phone { get; set; }
    };

    public class Receipt
    {
        public int Receipt_Id { get; set; }
        public int Receipt_CustomerId { get; set; }
        public DateTime Receipt_Date { get; set; }
        public int Receipt_Total { get; set; } = 0;
        public int Receipt_Remaining { get; set; } = 0;
        public DateTime Receipt_RevisitDate { get; set; }
        public string Receipt_Notes { get; set; }
    };

    public class ReceiptDetail
    {
        public int ReceiptDetail_Id { get; set; }
        public int ReceiptDetail_ReceiptId { get; set; }
        public string ReceiptDetail_Content { get; set; }
        public int ReceiptDetail_Quantity { get; set; }
        public int ReceiptDetail_Price { get; set; }
        public int ReceiptDetail_Discount { get; set; }
    };

    public class StockReceipt
    {
        public int StockReceipt_Id { get; set; }
        public DateTime StockReceipt_Date { get; set; }
        public bool StockReceipt_IsInput { get; set; }
        public string StockReceipt_Correspondent { get; set; }
        public string StockReceipt_Division { get; set; }
        public string StockReceipt_Reason { get; set; }
        public int StockReceipt_StockId { get; set; }
        public string StockReceipt_CertificateId { get; set; }
        public int StockReceipt_Total { get; set; } = 0;
    };

    public class StockReceiptDetail
    {
        public int StockReceiptDetail_Id { get; set; }
        public int StockReceiptDetail_ReceiptId { get; set; }
        public int StockReceiptDetail_ItemId { get; set; }
        public int StockReceiptDetail_Quantity { get; set; }
        public string StockReceiptDetail_Unit { get; set; }
        public int StockReceiptDetail_Demand { get; set; }
        public int StockReceiptDetail_Price { get; set; }
    };

    public class Item
    {
        public int Stock_Id { get; set; }
        public string Stock_Name { get; set; }
        public int Stock_Quantity { get; set; } = 0;
        public int Stock_Total { get; set; } = 0;
        public string Stock_Unit { get; set; }
    };

    public class Stock
    {
        public string StockList_Id { get; set; }
        public string StockList_Alias { get; set; }
        public string StockList_Address { get; set; }
    };

    public static class Util
    {
        public static void AttachReformatHook(DataGridView dgv)
        {
            dgv.CellFormatting += (sender, e) =>
            {
                string columnName = dgv.Columns[e.ColumnIndex].Name;
                if (e.Value != null && decimal.TryParse(e.Value.ToString(), out decimal value))
                {
                    e.Value = value.ToString("N0");
                    e.FormattingApplied = true;
                }
            };
        }

        public static void AttachUpdateHook(DataGridView dgv, string tableName)
        {
            dgv.CellValueChanged += (sender, e) => {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

                var currentColumn = dgv.Columns[e.ColumnIndex];
                if (currentColumn.Name.Contains("Total")) return;

                var id = dgv.CurrentRow.Cells[$"{tableName}_Id"].Value;
                var newValue = dgv.CurrentCell.Value;

                if (id != DBNull.Value)
                {
                    // Chỉ update hàng cũ
                    if (dgv.Columns[e.ColumnIndex] is DataGridViewCheckBoxColumn)
                        newValue = ((newValue != DBNull.Value && Convert.ToBoolean(newValue)) ? 1 : 0);
                    // ghi đè dữ liệu vào db
                    DataTable result = Database.UpdateRecord(tableName, Convert.ToInt32(id), new Dictionary<string, object>
                    { { currentColumn.Name, newValue } });
                }
            };
        }

        public static void AttachDeleteHook(DataGridView dgv, string tableName, bool deletePermanently = false)
        {
            dgv.KeyDown += (sender, e) => {
                if (e.KeyCode == Keys.Delete)
                {
                    if (MessageBox.Show("Bạn có chắc muốn xoá?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        switch (dgv.SelectionMode)
                        {
                            case DataGridViewSelectionMode.FullRowSelect:
                                foreach (DataGridViewRow row in dgv.SelectedRows)
                                {
                                    var id = row.Cells[$"{tableName}_Id"].Value;
                                    if (deletePermanently)
                                    {
                                        if (id != DBNull.Value)
                                            Database.DeleteRecord(tableName, new List<int> { Convert.ToInt32(id) });

                                        if (dgv.DataSource is BindingSource bs && row.DataBoundItem is DataRowView drv)
                                            drv.Delete();
                                        else
                                            dgv.Rows.Remove(row);
                                    }
                                    else
                                    {
                                        DataTable dt = new DataTable();
                                        if (dgv.DataSource is BindingSource bs && bs.DataSource is DataTable table)
                                            dt = table;
                                        else if (dgv.DataSource is DataTable directTable)
                                            dt = directTable;

                                        if (id != DBNull.Value)
                                        {
                                            Database.UpdateRecord(tableName,
                                                Convert.ToInt32(id),
                                                new Dictionary<string, object> { { $"{tableName}_IsActive", 0 } });

                                            DataRow[] selectedRow = dt.Select($"{tableName}_Id = " + id);
                                            selectedRow[0][$"{tableName}_IsActive"] = 0;
                                        }
                                    }
                                }
                                break;
                            case DataGridViewSelectionMode.RowHeaderSelect:
                                List<DataGridViewRow> deleteIndices = new List<DataGridViewRow>();
                                List<int> primaryValues = new List<int>();
                                foreach (DataGridViewCell cell in dgv.SelectedCells)
                                {
                                    deleteIndices.Add(cell.OwningRow);
                                    primaryValues.Add(Convert.ToInt32(dgv.Rows[cell.RowIndex].Cells[$"{tableName}_Id"].Value));
                                }
                                foreach (DataGridViewRow row in deleteIndices.OrderByDescending(i => i))
                                    dgv.Rows.Remove(row);
                                Database.DeleteRecord(tableName, primaryValues);
                                break;
                        }
                    }
                }
            };
        }

        public static void AttachRestoreHook(DataGridView dgv, string tableName)
        {
            DataTable dt = (DataTable)((BindingSource)dgv.DataSource).DataSource;
            foreach (DataGridViewRow row in dgv.SelectedRows)
            {
                Database.UpdateRecord(tableName,
                    Convert.ToInt32(row.Cells[$"{tableName}_Id"].Value),
                    new Dictionary<string, object>
                {
                    { $"{tableName}_IsActive", 1 }
                });

                DataRow[] selectedRow = dt.Select($"{tableName}_Id = " + row.Cells[$"{tableName}_Id"].Value);
                selectedRow[0][$"{tableName}_IsActive"] = 1;
            }
        }

        public static void DismissDirtyState(DataGridView dgv) {
            dgv.CurrentCellDirtyStateChanged += (sender, e) =>
            {
                if (dgv.CurrentCell is DataGridViewComboBoxCell)
                    dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
        }

        public static T MapRowTo<T>(DataGridViewRow row) where T : new()
        {
            T obj = new T();
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                if (row.DataGridView.Columns.Contains(prop.Name) &&
                    row.Cells[prop.Name].Value != null &&
                    row.Cells[prop.Name].Value != DBNull.Value)
                {
                    object value = row.Cells[prop.Name].Value;
                    prop.SetValue(obj, Convert.ChangeType(value, prop.PropertyType));
                }
            }
            return obj;
        }

        public static T GenerateObject<T>(params Control[] controls) where T : new()
        {
            T obj = new T();
            var properties = typeof(T).GetProperties();

            for (int i = 0; i < controls.Length; i++)
            {
                var control = controls[i];
                var prop = properties[i];
                object value = 0;

                if (control is TextBox txt)
                    value = txt.Text;
                if (control is DateTimePicker dtpk)
                    value = dtpk.Value;
                if (control is RadioButton rb)
                    value = rb.Checked;
                if (control is ComboBox cb)
                    value = cb.SelectedValue;

                prop.SetValue(obj, Convert.ChangeType(value, prop.PropertyType));
            }

            return obj;
        }

        public static void LoadRowToForm(DataGridViewRow row, params Control[] controls)
        {
            DataGridView dgv = row.DataGridView;
            for (int i = 0; i < dgv.Columns.Count; i++) {
                var value = row.Cells[i].Value;
                var control = controls[i];

                if (control is DateTimePicker dtpk)
                    dtpk.Value = Convert.ToDateTime(value);
                else if (control is TextBox txt)
                    txt.Text = value.ToString();
                else if (control is RadioButton rb)
                    rb.Checked = (Convert.ToInt32(value) != 0);
                else if (control is ComboBox cb)
                    cb.SelectedValue = value;
            }
        }

        public static void ClearForm(params Control[] controls)
        {
            foreach (Control control in controls)
            {
                if (control is DateTimePicker dtpk)
                    dtpk.Value = DateTime.Now;
                else if (control is TextBox txt)
                    txt.Text = string.Empty;
                else if (control is ComboBox cb)
                    cb.SelectedIndex = -1;
                else if (control is DataGridView dgv)
                    dgv.DataSource = null;
            }
        }

        public static void IncludeDtpkDialog(DataGridView dgv)
        {
            dgv.CellBeginEdit += (sender, e) =>
            {
                if (!dgv.Columns[e.ColumnIndex].Name.Contains("Date"))
                    return;

                e.Cancel = true;
                // Lấy giá trị ngày hiện tại trong ô
                DateTime currentDate = DateTime.Now;
                if (dgv.CurrentCell.Value != null)
                    DateTime.TryParse(dgv.CurrentCell.Value.ToString(), out currentDate);

                // Mở form chọn ngày
                using (DateTimePickerDialog dateDialog = new DateTimePickerDialog(currentDate))
                {
                    if (dateDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Cập nhật giá trị ô với ngày đã chọn
                        dgv.CurrentCell.Value = dateDialog.SelectedDate.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }
            };
        }
    }

    public abstract class PrintablePaper
    {
        public static readonly string RESOURCE_PATH = Path.Combine(Application.StartupPath, "res");
        public abstract string GetResultPath();
        public abstract void Render();
    }
}
