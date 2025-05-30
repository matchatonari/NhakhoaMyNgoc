﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace NhakhoaMyNgoc_Db
{
    public class Customer
    {
        public int Customer_Id { get; set; }
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
        public int Stock_Id { get; set; } = -1;
        public string Stock_Name { get; set; }
        public int Stock_Quantity { get; set; } = 0;
        public int Stock_Total { get; set; } = 0;
        public string Stock_Unit { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Item i && (Stock_Id == i.Stock_Id || Stock_Name == i.Stock_Name);
        }

        public override int GetHashCode()
        {
            if (Stock_Id > 0)
                return Stock_Id.GetHashCode();
            else
                return Stock_Name.GetHashCode();
        }
    };

    public class Stock
    {
        public string StockList_Id { get; set; }
        public string StockList_Alias { get; set; }
        public string StockList_Address { get; set; }
    };

    public class Expense
    {
        public int Expense_Id { get; set; }
        public string Expense_Date { get; set; }
        public bool Expense_IsInput { get; set; }
        public string Expense_Participant { get; set; }
        public string Expense_Address { get; set; }
        public string Expense_Content { get; set; }
        public string Expense_Amount { get; set; }
        public int Expense_CertificateId { get; set; }
    };

    public static class Util
    {
        public const int HOOK_REFORMAT = 0b0000001;
        public const int HOOK_UPDATE = 0b0000010;
        public const int HOOK_DELETE = 0b0000100;
        public const int HOOK_RESTORE = 0b0001000;
        public const int HOOK_DISMISS = 0b0010000;
        public const int HOOK_DTPKDIALOG = 0b0100000;
        public const int HOOK_RTIP = 0b1000000; // restrict input

        private static void AttachReformatHook(DataGridView dgv)
        {
            dgv.CellFormatting += (sender, e) =>
            {
                string columnName = dgv.Columns[e.ColumnIndex].Name;
                if (columnName.Contains("Id")) return;

                if (e.Value != null && decimal.TryParse(e.Value.ToString(), out decimal value))
                {
                    e.Value = value.ToString("N0");
                    e.FormattingApplied = true;
                }
            };
        }

        public static bool IsNumeric(object value)
        {
            return value is byte || value is sbyte || value is short || value is ushort ||
                   value is int || value is uint || value is long || value is ulong ||
                   value is float || value is double || value is decimal;
        }

        private static bool IsRecordFullyEntered<T>(T record)
        {
            foreach (var prop in typeof(T).GetProperties())
            {
                if (prop.Name.EndsWith("_Id")) continue; // Bỏ qua cột có "_Id"
                var value = prop.GetValue(record);
                if (IsNumeric(value))
                {
                    if (Convert.ToDouble(value) == 0)
                        return false;
                } else
                {
                    if (value == null || value is DBNull || (value is string str && str.Length == 0))
                        return false; // Nếu có giá trị null, DBNull hoặc chuỗi rỗng => chưa nhập đủ
                }
            }
            return true;
        }

        private static void AttachUpdateHook<T>(DataGridView dgv, string tableName) where T : new()
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
                } else
                {
                    T record = MapRowTo<T>(dgv.CurrentRow);
                    if (IsRecordFullyEntered<T>(record))
                        Database.AddRecord(tableName, record);
                }
            };
        }

        private static void AttachDeleteHook(DataGridView dgv, string tableName, bool deletePermanently = false)
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
                                foreach (DataGridViewRow row in deleteIndices.OrderByDescending(i => i.Index))
                                    dgv.Rows.Remove(row);
                                Database.DeleteRecord(tableName, primaryValues);
                                break;
                        }
                    }
                }
            };
        }

        private static void AttachRestoreHook(ToolStripMenuItem ctl, DataGridView dgv, string tableName)
        {
            ctl.Click += (sender, e) =>
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
            };
        }

        public static void AttachRestrictInputHook(DataGridView dgv)
        {
            dgv.CellValidating += (s, ev) =>
            {
                if ((dgv.Columns[ev.ColumnIndex].ValueType == typeof(int) ||
                    dgv.Columns[ev.ColumnIndex].ValueType == typeof(long)) &&
                    !dgv.CurrentRow.IsNewRow)
                {
                    if (!int.TryParse(ev.FormattedValue.ToString(), NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _))
                    {
                        MessageBox.Show("Chỉ được nhập số!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        ev.Cancel = true; // Ngăn không cho rời khỏi ô
                    }
                }
            };
        }

        public static void AttachHook<T>(DataGridView dgv, int hook, string tableName = null, bool unrecoverable = false, ToolStripMenuItem ctl = null) where T : new()
        {
            if ((hook & HOOK_REFORMAT) > 0)
                AttachReformatHook(dgv);
            if ((hook & HOOK_UPDATE) > 0)
                AttachUpdateHook<T>(dgv, tableName);
            if ((hook & HOOK_DELETE) > 0)
                AttachDeleteHook(dgv, tableName, unrecoverable);
            if ((hook & HOOK_RESTORE) > 0)
                AttachRestoreHook(ctl, dgv, tableName);
            if ((hook & HOOK_DISMISS) > 0)
                DismissDirtyState(dgv);
            if ((hook & HOOK_DTPKDIALOG) > 0)
                IncludeDtpkDialog(dgv);
            if ((hook & HOOK_RTIP) > 0)
                AttachRestrictInputHook(dgv);
        }

        private static void DismissDirtyState(DataGridView dgv) {
            dgv.CurrentCellDirtyStateChanged += (sender, e) =>
            {
                if (dgv.CurrentCell is DataGridViewComboBoxCell || dgv.CurrentCell is DataGridViewCheckBoxCell)
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

        public static T MapRowTo<T>(DataRow row) where T : new()
        {
            T obj = new T();
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                if (row.Table.Columns.Contains(prop.Name) &&
                    row[prop.Name] != null &&
                    row[prop.Name] != DBNull.Value)
                {
                    object value = row[prop.Name];
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
                    cb.SelectedIndex = (cb.Items.Count > 0) ? 0 : -1;
                else if (control is DataGridView dgv)
                    dgv.DataSource = null;
            }
        }

        private static void IncludeDtpkDialog(DataGridView dgv)
        {
            dgv.CellBeginEdit += (sender, e) =>
            {
                bool result = dgv.Columns[e.ColumnIndex].Name.ToLower().Contains("date".ToLower());
                if (!result) return;

                e.Cancel = true;
                // Lấy giá trị ngày hiện tại trong ô
                DateTime currentDate = DateTime.Now;
                if (dgv.CurrentCell.Value != DBNull.Value)
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
        
        public static void LoadTableToDataGridView(DataTable dt, DataGridView dgv)
        {
            DataTable renderResult = dt.Clone();

            foreach (DataGridViewColumn c in dgv.Columns)
                if (c is DataGridViewCheckBoxColumn)
                    renderResult.Columns[c.Name].DataType = typeof(bool);

            foreach (DataRow r in dt.Rows)
            {
                var newRow = renderResult.NewRow();
                foreach (DataGridViewColumn c in dgv.Columns)
                {
                    if (c is DataGridViewCheckBoxColumn)
                        newRow[c.Name] = (Convert.ToInt32(r[c.Name]) == 1);
                    else
                        newRow[c.Name] = r[c.Name];
                }
                renderResult.Rows.Add(newRow);
            }

            dgv.DataSource = renderResult;
            renderResult.AcceptChanges();
        }

        public static void CopyDirectory(string sourceDir, string destinationDir)
        {
            if (!Directory.Exists(sourceDir))
                throw new DirectoryNotFoundException($"Thư mục nguồn không tồn tại: {sourceDir}");

            // Tạo thư mục đích nếu chưa có
            Directory.CreateDirectory(destinationDir);

            // Copy tất cả các file
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true); // `true` để ghi đè nếu file đã tồn tại
            }

            // Đệ quy copy các thư mục con
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }
    }

    public abstract class PrintablePaper
    {
        public static readonly string RESOURCE_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NhakhoaMyNgoc", "Templates");
        public bool Landscape { get; set; }
        public PrintablePaper()
        {
            Util.CopyDirectory(RESOURCE_PATH, Path.GetTempPath());
        }
        public abstract string GetTemplateName();
        public abstract object GetFileName();
        public abstract void Edit(ref string templateSrc);
        public void Render()
        {
            string templateSrc = File.ReadAllText(Path.Combine(RESOURCE_PATH, GetTemplateName() + ".html"));
            Edit(ref templateSrc);
            File.WriteAllText(Path.Combine(Path.GetTempPath(), GetFileName().ToString() + ".html"), templateSrc);
        }
    }
}
